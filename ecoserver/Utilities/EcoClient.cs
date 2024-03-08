using System.Diagnostics;
using System.Net.WebSockets;
using System.Text;
using System.Text.Json;
using Newtonsoft.Json;
using webapi;

public class EcoClient
{
    public string IdClient { get; set; } = "NNN";
    public GroupRole groupRole { get; set; } = GroupRole.None;
    public WebSocket? _socketClient {get; set;}

    public async void SerializeAndSendMessage(EcodroneBoatMessage ecodroneMessage)
    {
        if(_socketClient != null)
        {
            if (ecodroneMessage.direction == IdClient)
            {
                string message_serialized = JsonConvert.SerializeObject(ecodroneMessage, Formatting.Indented);
                           
                Debug.WriteLine(message_serialized);
                
                var messageToSend = new ArraySegment<byte>(Encoding.UTF8.GetBytes(message_serialized));

                await _socketClient.SendAsync(messageToSend, WebSocketMessageType.Binary, true, CancellationToken.None);
            }
            
        }
    }

}
