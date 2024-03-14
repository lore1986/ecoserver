using System.Diagnostics;
using System.Net.Sockets;
using System.Net.WebSockets;
using System.Text;
using System.Text.Json;
using webapi;

public class EcodroneUsers
{
    public int Id { get; }
    public string Identification { get; set; } = "NNN";
    public string Password { get; set; } = "NNN";
    public DateTime LastLogin { get; set; } = DateTime.Now;

    public EcodroneUsers(int id)
    {
        Id = id;
    }

    public bool ReturnHashedPassword(string password)
    {
        //Add Hashing and Encrypt for password
        if (password == null || password.Length == 0) { return false; }
        if (password == Password) { return true; }

        return false;
    }

}

public class LoginModel
{
    public string Username { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
}


/*public class CustomerGroup
{
    public string GroupId { get; private set; }
    public List<TeensyGroup> teensyGroups;
    public CustomerGroup(string customerGroupName)
    {
        GroupId = customerGroupName;
        teensyGroups = new List<TeensyGroup>();
    }
}*/






public enum GroupRole
{
    None,
    Admin
}



public class BusEventMessage : EventArgs
{
    public string idTeensy { get; set; }
    public byte[] data { get; set; }

    //sender
    public BusEventMessage(string idT, byte[] data)
    {
        idTeensy = idT;
        this.data = data;
    }
}

public class WayPointEventArgs : EventArgs
{
    public List<WayPoint> WayPoints { get; private set; }
    public WayPointEventArgs(List<WayPoint> inways)
    {
        WayPoints = inways;
    }
}

public class NewClientEventArgs : EventArgs
{
    public string maskedTeensyId { get; set; } = "NNN";
    public string userid { get; set; }

}






public enum ClientCommunicationStates
{
    SENSORS_DATA,
    MISSIONS,
    VIDEO
}