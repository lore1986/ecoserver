using System.Diagnostics;
using System.Net.Sockets;
using System.Threading.Channels;
using webapi;


public class EcodroneTeensyInstance
{
    public EcodroneBoat ecodroneBoat { get; private set; }
    public TcpClient? _teensySocket { get; set; }
    public NetworkStream? _networkStream { get; set; }

    public Task? taskReading { get; set; } = null;

    public CancellationTokenSource? src_cts_teensy {get; set;}
    public CancellationToken cts_teensy {get; set;}
    
    public List<TeensyMessageContainer> command_task_que;
    public ITeensyMessageConstructParser _teensyLibParser { get; private set; }
    //public Channel<ChannelTeensyMessage> channelTeensy { get; set; }
    public ISignalBusSocket signalBusSocket  = new SignalBusSocket();

    //public JetsonSocketHandler jetsonSocket {  get; set; }

    public EcodroneTeensyInstance(EcodroneBoat boat, string gid)
    { 
        ecodroneBoat = boat;
        ecodroneBoat.maskedId = gid;
        _teensyLibParser =  new TeensyMessageConstructParser();
        command_task_que = EcodroneMessagesContainers.GenerateRequestFunct();

    }
    private void GetCommandWriteChannel(ChannelTeensyMessage channelData)
    {
        if (channelData.data_message != null && channelData.data_message != "NNN")
        {
            signalBusSocket.Publish(channelData);
        }

        if (channelData.data_command != null && channelData.needAnswer)
        {
            TeensyMessageContainer newMessageContainer = new TeensyMessageContainer(channelData.id_client, channelData.message_id, channelData.data_command, needPreparation: false);
            
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

    private async Task TalkToTeensyAsync(TeensyMessageContainer m_cont)
    {
     
        byte[] data = m_cont.CommandId;

        if (m_cont.NeedPreparation)
        {
            data = _teensyLibParser.PrepareTeensyRequest(m_cont.CommandId);
        }

        if (_networkStream != null)
        {
            await _networkStream.WriteAsync(data, 0, data.Length);            
        }
        else
        {
            Debug.WriteLine("Error with network stream");
        }
    }


    public async Task StartTeensyTalk()
    {
        using (_teensySocket = new TcpClient(ecodroneBoat._IPTeensy, ecodroneBoat._PortTeensy))
        {
            using (_networkStream = _teensySocket.GetStream())
            {
                Debug.WriteLine("Teensy connected");

                while (_teensySocket.Connected)
                {

                    if(command_task_que.Count > 0)
                    {
                        await TalkToTeensyAsync(command_task_que[0]);
                    }

                    ChannelTeensyMessage channelMessage = await _teensyLibParser.ReadBufferAsync(_networkStream, command_task_que[0].IdClient);
                    
                    GetCommandWriteChannel(channelMessage);
                    command_task_que.RemoveAt(0);
                    await Task.Delay(50);

                    if(ecodroneBoat._boatclients.Count(x => x.Key.appState == ClientCommunicationStates.SENSORS_DATA) > 0 && command_task_que.Count() == 0)
                    {
                        command_task_que = EcodroneMessagesContainers.GenerateRequestFunct();
                    }
                    
                }
            }
        }

        _networkStream.Close();
        _networkStream.Dispose();
        _teensySocket.Close();
        _teensySocket.Dispose();
    }

}