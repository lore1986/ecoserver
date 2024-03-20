public class EcodroneBoatMessage
{
    public char scope { get; set; } = 'N';
    public string type { get; set; } = string.Empty;
    public string uuid { get; set; } = string.Empty;
    public string direction { get; set; } = string.Empty;
    public string identity { get; set; } = string.Empty;
    public object? data { get; set; }

}

