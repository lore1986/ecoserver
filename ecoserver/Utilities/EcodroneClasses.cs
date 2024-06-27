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





public enum ClientCommunicationStates
{
    SENSORS_DATA,
    MISSIONS,
    VIDEO,
    WAYPOINT,
    NAVIGATION
}