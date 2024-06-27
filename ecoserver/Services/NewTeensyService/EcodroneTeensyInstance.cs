using System.Diagnostics;
using System.Net.Sockets;
using System.Text;
using System.Threading.Channels;
using webapi;


public class EcodroneTeensyInstance
{
    public EcodroneBoat ecodroneBoat { get; private set; }
    public TcpClient? _teensySocket { get; set; }
    public NetworkStream? _networkStream { get; set; }

    public CancellationTokenSource? src_cts_teensy {get; set;}
    public CancellationToken cts_teensy {get; set;}
    
    public List<TeensyMessageContainer> command_task_que;
    public List<TeensyMessageContainer> internal_task_que;

    private ITeensyMessageConstructParser _teensyLibParser;
    
    private ISignalBusSocket signalBusSocket;

    private Task socketTask;

    public EcodroneTeensyInstance(EcodroneBoat boat, string gid)
    { 
        ecodroneBoat = boat;
        ecodroneBoat.maskedId = gid;
        signalBusSocket = boat.signalBusSocket;
        _teensyLibParser = boat._teensyLibParser;
        command_task_que = new List<TeensyMessageContainer>();
        internal_task_que = new List<TeensyMessageContainer>();
        socketTask = Task.Run(StartTeensyTalk);

        src_cts_teensy = new CancellationTokenSource();
        cts_teensy = src_cts_teensy.Token;
    }

    private Tuple<byte[], int> IsSubArray(byte[] mainArray, byte[] subArray)
    {
        for (int i = 0; i <= mainArray.Length - subArray.Length; i++)
        {
            if (mainArray.Skip(i).Take(subArray.Length).SequenceEqual(subArray))
            {
                int length = mainArray[i + 3] + 4;
                byte[] unconditionArray = new byte[length];
                Array.Copy(mainArray, i, unconditionArray, 0, length);
                return Tuple.Create(unconditionArray, 1);
            }
        }
        
        return Tuple.Create(new byte[1], -1);
    }


    public async void StartTeensyTalk()
    {
        _teensySocket = new TcpClient(ecodroneBoat._IPTeensy, ecodroneBoat._PortTeensy){ ReceiveTimeout = 5000 };

        _networkStream = _teensySocket.GetStream();
            
        Debug.WriteLine("Teensy is connected");

        while(!cts_teensy.IsCancellationRequested && _teensySocket.Connected)
        {
            if(command_task_que.Count > 0)
            {
                StringBuilder sb = new StringBuilder();
                foreach (byte b in command_task_que[0].CommandId)
                {
                    sb.Append(b + " ");
                }

                Debug.WriteLine("Message DATA");
                // Print the formatted string
                Debug.WriteLine(sb.ToString().Trim());

                await _networkStream.WriteAsync(command_task_que[0].CommandId, 0, command_task_que[0].CommandId.Length); 

        
                byte[] dataread = new byte[1024*4];
                int bytesRead = await _networkStream.ReadAsync(dataread);

                Tuple<byte[], int> check_array  = IsSubArray(dataread, [ecodroneBoat._Sync[0],ecodroneBoat._Sync[1],ecodroneBoat._Sync[2]]);

                if(check_array.Item2 > 0)
                {
                    _teensyLibParser.ParseTeensyMessage(check_array.Item1);
                    command_task_que.RemoveAt(0);
                }else
                {
                    command_task_que.RemoveAt(0);
                    // Debug.WriteLine($"Dirty sub string are you {Encoding.UTF8.GetString(dataread)}");
                    // Debug.WriteLine($"Dirty sub buffer are you {dataread}");
                }

            }
            else
            {
                command_task_que = ecodroneBoat.ecodroneMessagesContainers.GenerateRequestFunct();
            }


            await Task.Delay(100);
            
        }

        _networkStream?.Close();
        _networkStream?.Dispose();
        _teensySocket.Close();
        _teensySocket.Dispose();
        
        socketTask.Wait(cts_teensy);
        socketTask.Dispose();

    }

    public void RestartTeensyTalkSignal()
    {
        src_cts_teensy = new CancellationTokenSource();
        cts_teensy = src_cts_teensy.Token;
        socketTask = Task.Run(StartTeensyTalk);
    }

}