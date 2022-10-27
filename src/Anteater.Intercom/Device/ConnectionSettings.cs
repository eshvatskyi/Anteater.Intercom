namespace Anteater.Intercom.Device
{
    public record ConnectionSettings
    {
        public string Host { get; set; }

        public string Username { get; set; }

        public string Password { get; set; }

        public int WebPort { get; set; } = 80;

        public int RtspPort { get; set; } = 554;

        public int DataPort { get; set; } = 5000;
    }
}
