using System.Diagnostics;
using System.Net.Sockets;
using System.Threading.Channels;
using webapi;


public class EcodroneTeensyInstance
{
    public EcodroneBoat ecodroneBoat { get; private set; }
    public TcpClient? _teensySocket { get; set; }
    public NetworkStream? _networkStream { get; set; }

    public Task? TaskReading { get; set; } = null;

    public CancellationTokenSource src_cts_teensy = new CancellationTokenSource();
    public CancellationToken cts_teensy {get; private set;}
    
    public List<TeensyMessageContainer> command_task_que;
    public ITeensyMessageConstructParser _teensyLibParser { get; private set; }
    //public Channel<ChannelTeensyMessage> channelTeensy { get; set; }
    public SignalBusSocket signalBusSocket  = new SignalBusSocket();

    //public JetsonSocketHandler jetsonSocket {  get; set; }

    public EcodroneTeensyInstance(EcodroneBoat boat, string gid)
    { 
        ecodroneBoat = boat;
        ecodroneBoat.maskedId = gid;

        cts_teensy = src_cts_teensy.Token;

        _teensyLibParser =  new TeensyMessageConstructParser();
        command_task_que = EcodroneMessagesContainers.GenerateRequestFunct();

        // channelTeensy = Channel.CreateUnbounded<ChannelTeensyMessage>(new UnboundedChannelOptions
        // {
        //     SingleWriter = false,
        //     SingleReader = false,
        //     AllowSynchronousContinuations = true
        // });

    }
    private void GetCommandWriteChannel(ChannelTeensyMessage channelData)
    {
        if (channelData.data_message != null && channelData.data_message != "NNN")
        {
            signalBusSocket.Publish(channelData);
            //await channelTeensy.Writer.WriteAsync(channelData, cts_teensy);
        }

        if (channelData.data_command != null && channelData.needAnswer)
        {
            TeensyMessageContainer newMessageContainer = new TeensyMessageContainer(channelData.message_id, channelData.data_command, needPreparation: false);
            
            if(channelData.id_client != null && channelData.id_client != "NNN")
            {
                newMessageContainer.IdClient = channelData.id_client;
                
            }else
            {
                newMessageContainer.IdClient = "NNN";
            }

            command_task_que.Add(newMessageContainer);

        }

    }

    private async Task<ChannelTeensyMessage> TalkToTeensyAsync(TeensyMessageContainer m_cont)
    {
        ChannelTeensyMessage channelMessage = new ChannelTeensyMessage("empty");

        if(m_cont.IdContainer == "UpMission")
        {
            Debug.WriteLine("container ");
        }

        byte[] data = m_cont.CommandId;

        if (m_cont.NeedPreparation)
        {
            data = _teensyLibParser.PrepareTeensyRequest(m_cont.CommandId);
        }

        if (_networkStream != null)
        {
            await _networkStream.WriteAsync(data, 0, data.Length);
            
            channelMessage = await _teensyLibParser.ReadBufferAsync(_networkStream);

            if(m_cont.IdClient != null && m_cont.IdClient != "NNN")
            {
                channelMessage.id_client = m_cont.IdClient;
            }
            
            return channelMessage;
            
        }
        else
        {
            Debug.WriteLine("Error with network stream");
        }

        return channelMessage;
    }


    public async Task StartTeensyTalk()
    {
        using (_teensySocket = new TcpClient(ecodroneBoat._IPTeensy, ecodroneBoat._PortTeensy))
        {
            using (_networkStream = _teensySocket.GetStream())
            {
                while (_teensySocket.Connected && !cts_teensy.IsCancellationRequested)
                {
                    
                    while(command_task_que.Count > 0)
                    {
                        //commands to teensy 
                        //step one send command to teensy and read answer
                        ChannelTeensyMessage channelMessage = await TalkToTeensyAsync(command_task_que[0]);
                        //remove command from original list of commands
                        command_task_que.RemoveAt(0);
                        //write the message on the channel to be read
                        GetCommandWriteChannel(channelMessage);
                        await Task.Delay(20);
                    }

                    command_task_que = EcodroneMessagesContainers.GenerateRequestFunct();
                    //USE SIGNAL TO DO THIS ONCHANGE STATE WITH A DELEGATE FROM BoatClientAppStateManager or maybe change position of this function
                    // int connected_clients = ecodroneBoat._boatclients.Where(x => x.appState == ClientCommunicationStates.SENSORS_DATA).Count();
                    // if(connected_clients != 0)
                    // {
                        
                    // }
                }

                _networkStream.Close();
                _networkStream.Dispose();
                _teensySocket.Close();
                _teensySocket.Dispose();

            }
        }
    }

    
    

    // public ChannelTeensyMessage? ReadOnChannel()
    // {
    //     if(channelTeensy.Reader.Count != 0)
    //     {
    //         ChannelTeensyMessage? channelRead;
    //         channelTeensy.Reader.TryRead(out channelRead);
            
    //         return channelRead;
    //     }

    //     return null;
    // }
}