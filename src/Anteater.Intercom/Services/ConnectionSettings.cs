namespace Anteater.Intercom.Services;

public record ConnectionSettings
{
    public string Host { get; set; } = "127.0.0.1";

    public string Username { get; set; } = "";

    public string Password { get; set; } = "";

    public int WebPort { get; set; } = 80;

    public int RtspPort { get; set; } = 554;

    public int DataPort { get; set; } = 5000;
}
