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
    public Channel<ChannelTeensyMessage> channelTeensy { get; set; }
    

    //public JetsonSocketHandler jetsonSocket {  get; set; }

    public EcodroneTeensyInstance(EcodroneBoat boat, string gid)
    { 
        ecodroneBoat = boat;
        ecodroneBoat.maskedId = gid;

        cts_teensy = src_cts_teensy.Token;

        _teensyLibParser =  new TeensyMessageConstructParser();
        command_task_que = EcodroneMessagesContainers.GenerateRequestFunct();

        channelTeensy = Channel.CreateUnbounded<ChannelTeensyMessage>(new UnboundedChannelOptions
        {
            SingleWriter = false,
            SingleReader = false,
            AllowSynchronousContinuations = true
        });

    }

    public async Task StartTeensyTalk()
    {
        using (_teensySocket = new TcpClient(ecodroneBoat._IPTeensy, ecodroneBoat._PortTeensy))
        {
            using (_networkStream = _teensySocket.GetStream())
            {
                while (_teensySocket.Connected && !cts_teensy.IsCancellationRequested)
                {
                    if(command_task_que.Count > 0)
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
                        }
                        
                    }else
                    {
                        command_task_que = EcodroneMessagesContainers.GenerateRequestFunct();
                    }
                }

                _networkStream.Close();
                _networkStream.Dispose();
                _teensySocket.Close();
                _teensySocket.Dispose();

            }
        }
    }

    private async Task<ChannelTeensyMessage> TalkToTeensyAsync(TeensyMessageContainer m_cont)
    {
        ChannelTeensyMessage channelMessage = new ChannelTeensyMessage("empty");

        byte[] data = m_cont.CommandId;

        if (m_cont.NeedPreparation)
        {
            data = _teensyLibParser.PrepareTeensyRequest(m_cont.CommandId);
        }

        if (_networkStream != null)
        {
            await _networkStream.WriteAsync(data, 0, data.Length);
            
            channelMessage = await _teensyLibParser.ReadBufferAsync(_networkStream);

            return channelMessage;
            
        }
        else
        {
            Debug.WriteLine("Error with network stream");
        }

        return channelMessage;
    }

    private void GetCommandWriteChannel(ChannelTeensyMessage channelData)
    {
        if (channelData.data_message != null)
        {
            channelTeensy.Writer.WriteAsync(channelData, cts_teensy);
        }

        //if there is a new issued command then add it to the list of tasks at position 0
        if (channelData.data_command != null && channelData.needAnswer)
        {
            command_task_que.Add(
                new TeensyMessageContainer("new_internal_data", channelData.data_command, needPreparation: false)
            );

        }

    }

    public ChannelTeensyMessage? ReadOnChannel()
    {
        if(channelTeensy.Reader.Count != 0)
        {
            ChannelTeensyMessage? channelRead;
            channelTeensy.Reader.TryRead(out channelRead);
            
            return channelRead;
        }

        return null;
    }
}