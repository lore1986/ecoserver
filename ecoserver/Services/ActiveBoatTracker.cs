using System.Diagnostics;

public  class EcodroneBoatSettings
{
    public readonly byte[] _sync_teensy = [0x10, 0x11, 0x12];
    public string _ipAddress = "2.194.19.139"; //"192.168.1.213"; // 
    public int _port = 5050;
    public bool _active = true;
    // public EcodroneBoatSettings(byte[] sync_teensy, )
    // {
        
    // }
}

public class ActiveBoatTracker : IActiveBoatTracker
{
    private List<EcodroneBoat> ecodroneBoats = new List<EcodroneBoat>();

    private Dictionary<string, EcodroneBoatSettings> ecodroneBoatSettings = new Dictionary<string, EcodroneBoatSettings>()
    {
        { "ecodrone_boatone", new EcodroneBoatSettings() }


    };

    public EcodroneBoat? CreateAddBoat(string boatId) 
    {
        EcodroneBoatSettings? _settings = ecodroneBoatSettings.SingleOrDefault(x => x.Key == boatId).Value;

        if(_settings != null)
        {
            if(!ecodroneBoats.Any(x => x._Sync == _settings._sync_teensy))
            {
                EcodroneBoat ecodroneBoat = new EcodroneBoat(boatId, _settings._sync_teensy, _settings._ipAddress, _settings._port, _settings._active);
                ecodroneBoats.Add(ecodroneBoat);

                return ecodroneBoat;
            }

            return null;
           
        }else
        {
            return null;
        }
        
    }

    public EcodroneBoat? ReturnEcodroneBoatInstance(string boat_id)
    {
        return ecodroneBoats.SingleOrDefault(x => x.maskedId == boat_id);
    }

    public async void RemoveEcodroneBoatInstance(string boat_id)
    {
        EcodroneBoat? _ecodroneBoat = ecodroneBoats.SingleOrDefault(x => x.maskedId == boat_id);
        
        if(_ecodroneBoat != null)
        {
            if(_ecodroneBoat.teensySocketInstance.src_cts_teensy != null)
            {
                _ecodroneBoat.teensySocketInstance.src_cts_teensy.Cancel();
            }
            

            for (int i = 0; i < _ecodroneBoat._boatclients.Count(); i++)
            {
                EcoClient ecoClient = _ecodroneBoat._boatclients.ElementAt(i);

                
                if(ecoClient.taskina != null)
                {
                    

                    await ecoClient._socketClient.CloseAsync(System.Net.WebSockets.WebSocketCloseStatus.NormalClosure, "closed by big server", CancellationToken.None);
                    ecoClient.src_cts_client.Cancel();
                    Debug.WriteLine($"STATUS {ecoClient._socketClient.State} ");

                }
                
                
                // await ecoClient._socketClient.CloseAsync(System.Net.WebSockets.WebSocketCloseStatus.NormalClosure, "closing", CancellationToken.None);
                // ecoClient._socketClient.Dispose();
                // Task? task_client= _ecodroneBoat._boatclients.ElementAt(i).Value;
                if(_ecodroneBoat._videoBusService.IsASubscriber(ecoClient.IdClient))
                {
                    ecoClient.UnsubscribeVideo(_ecodroneBoat);
                }
                    
            }

            //_ecodroneBoat.src_cts_block_listening.Cancel();
        

            //_ecodroneBoat.src_cts_boat.Cancel();

            _ecodroneBoat._ecodroneBoatClienSocketListener.Stop();
            //_ecodroneBoat._ecodroneBoatClienSocketListener.Prefixes.Remove($"http://localhost:{_ecodroneBoat.port}/interface/");

            

            EcodroneBoatMessage ecodroneBoatMessage = new EcodroneBoatMessage()
            {
                scope = 'X',
                type = "0",
                uuid = _ecodroneBoat.maskedId,
                direction = "jetson_id",
                identity = string.Empty,
                data = "NNN"
            };

            _ecodroneBoat._videoBusService?.Publish(ecodroneBoatMessage);

            
            if(_ecodroneBoat.ecodroneVideo.taskjetson != null)
            {
                
                if(_ecodroneBoat.ecodroneVideo.socketJetson != null)
                {
                    _ecodroneBoat.ecodroneVideo.socketJetson.Close();
                    _ecodroneBoat.ecodroneVideo.socketJetson.Dispose();
                }
                
                _ecodroneBoat._videoBusService?.Unsubscribe(_ecodroneBoat.ecodroneVideo.ReadAndSendJetson, "jetson_id");

                _ecodroneBoat.ecodroneVideo.src_cts_read.Cancel();
            }
           

            ecodroneBoats.Remove(_ecodroneBoat);

        }

        
    }
}
