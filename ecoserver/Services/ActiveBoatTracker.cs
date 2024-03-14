using System.Reflection.Metadata.Ecma335;

public  class EcodroneBoatSettings
{
    public byte[] _sync_teensy = [0x10, 0x11, 0x12];
    public string _ipAddress = "192.168.1.213";
    public int _port = 5050;
    public bool _active = true;
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
            ecodroneBoats.Remove(_ecodroneBoat);
            return true;
        }

        return false;
    }
}
