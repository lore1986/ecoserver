using System.Diagnostics;

public  class EcodroneBoatSettings
{
    public byte[] _sync_teensy = [0x10, 0x11, 0x12];
    public string _ipAddress = "2.194.22.210"; //"192.168.1.213"; // 
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
            EcodroneBoat ecodroneBoat = new EcodroneBoat(boatId, _settings._sync_teensy, _settings._ipAddress, _settings._port, _settings._active);
            ecodroneBoats.Add(ecodroneBoat);


            return ecodroneBoat;
        }else
        {
            return null;
        }
        
    }

    public EcodroneBoat? ReturnEcodroneBoatInstance(string boat_id)
    {
        return ecodroneBoats.SingleOrDefault(x => x.maskedId == boat_id);
    }

    public bool RemoveEcodroneBoatInstance(string boat_id)
    {
        EcodroneBoat? _ecodroneBoat = ecodroneBoats.SingleOrDefault(x => x.maskedId == boat_id);
        
        if(_ecodroneBoat != null)
        {
            if(_ecodroneBoat.teensySocketInstance.src_cts_teensy != null)
            {
                _ecodroneBoat.teensySocketInstance.src_cts_teensy.Cancel();
            }else
            {
                Debug.WriteLine("no client was in state != video");
            }
            

            for (int i = 0; i < _ecodroneBoat._boatclients.Count(); i++)
            {
                EcoClient ecoClient = _ecodroneBoat._boatclients.ElementAt(i).Key;
                Task? task_client= _ecodroneBoat._boatclients.ElementAt(i).Value;

                if(ecoClient.appState == ClientCommunicationStates.VIDEO)
                {
                    ecoClient.UnsubscribeVideo(_ecodroneBoat);
                }
                    
            }

            _ecodroneBoat.src_cts_block_listening.Cancel();
        

            _ecodroneBoat.src_cts_boat.Cancel();

            _ecodroneBoat._ecodroneBoatClienSocketListener.Stop();
            _ecodroneBoat._ecodroneBoatClienSocketListener.Prefixes.Remove($"http://*:{_ecodroneBoat.port}/");
            _ecodroneBoat._ecodroneBoatClienSocketListener.Close();

            _ecodroneBoat.ecodroneVideo.src_cts_jetson.Cancel();

            _ecodroneBoat.ecodroneVideo._jetsonClientListener.Stop();
            _ecodroneBoat.ecodroneVideo._jetsonClientListener.Dispose();

            ecodroneBoats.Remove(_ecodroneBoat);

            return true;
        }

        return false;
    }
}
