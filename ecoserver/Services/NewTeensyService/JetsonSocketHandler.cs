using System;
using System.Collections;
using System.Net;
using System.Net.Sockets;
using System.Net.Security;
using System.Security.Authentication;
using System.Text;
using System.Security.Cryptography.X509Certificates;
using System.IO;
using Microsoft.AspNetCore.DataProtection.KeyManagement;
using System.Text.Json;
using Microsoft.AspNetCore.DataProtection;
using System.Xml.Linq;
using static System.Runtime.InteropServices.JavaScript.JSType;
using System.Runtime.ConstrainedExecution;
using System.Diagnostics;
using System.Security.Cryptography;
using System.Net.NetworkInformation;
using System.Net.WebSockets;
using System.Reflection.Emit;
using System.IO.Compression;
using Microsoft.AspNetCore.Http.HttpResults;
using static System.Net.Mime.MediaTypeNames;
using System.Transactions;
using Microsoft.AspNetCore.Http;
using Newtonsoft.Json.Linq;
using System.Runtime.InteropServices;
using System.Reflection.Metadata;
using System.Numerics;
using Microsoft.AspNetCore.Mvc;
using System.Reflection;
using System.Threading.Tasks;
using webapi.Services.SocketService;
using System.Net.Http;

namespace webapi
{

/*    public class VideoConnectedClientServer
    {
        public string uuid { get; set; } = string.Empty;
        public bool isServer { get; set; } = false;
        public string message { get; set; } = string.Empty;
        public VideoSocketManager? socketManager { get; set; } = null;
        public Socket? _socket { get; set; } = null;
    }*/

    /*public class JetsonSocketHandler
    {
        *//*static TcpListener tcpListener;
        static Thread listenerThread;

        public List<VideoConnectedClientServer> ClientServers = new List<VideoConnectedClientServer>(); 

        public void RunServer()
        {
            // Start the server on a specific IP address and port
            tcpListener = new TcpListener(IPAddress.Any, 5055);
            listenerThread = new Thread(new ThreadStart(ListenForClients));
            listenerThread.Start();
        }

        public VideoConnectedClientServer? CheckReturnClient(string uuid)
        {
            return ClientServers.SingleOrDefault(x => x.uuid == uuid);
        }


        public void PublishToClient(byte[] message, string uuid)
        {
            VideoConnectedClientServer? c_serv_client = ClientServers.SingleOrDefault(x => x.uuid == uuid);

            if(c_serv_client != null)
            {
                if (c_serv_client._socket != null && c_serv_client._socket.Connected)
                {
                    c_serv_client._socket.SendAsync(message);
                }
            }
        }

        public void ListenForClients()
        {
            tcpListener.Start();

            while (true)
            {
                // Accept a new client connection
                Socket clientSocket = tcpListener.AcceptSocket();

                Console.WriteLine($"Client connected: {clientSocket.RemoteEndPoint}");

                VideoSocketManager videoSocketManager = new VideoSocketManager();
                videoSocketManager.HandleClient(clientSocket);
            }
        }*//*


    }*/

}
