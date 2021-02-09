using Anteater.Intercom.Properties;

namespace Anteater.Intercom.Device
{
    public class ConnectionSettings
    {
        public static ConnectionSettings Default { get; }

        static ConnectionSettings()
        {
            Default = new ConnectionSettings(Settings.Default.Host, Settings.Default.Username, Settings.Default.Password);            
        }

        private ConnectionSettings(string host, string username = null, string password = null, int webPort = 80, int rtspPort = 554, int dataPort = 5000)
        {
            Host = host;
            Username = username;
            Password = password;
            WebPort = webPort;
            RtspPort = rtspPort;
            DataPort = dataPort;
        }

        public string Host { get; }

        public string Username { get; }

        public string Password { get; }

        public int WebPort { get; }

        public int RtspPort { get; }

        public int DataPort { get; }
    }
}
