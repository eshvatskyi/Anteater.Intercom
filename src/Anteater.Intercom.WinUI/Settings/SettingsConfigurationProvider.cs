using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using Anteater.Intercom.Services.Settings;
using Microsoft.Extensions.Configuration;
using Squirrel;

namespace Anteater.Intercom.Settings;

public class SettingsConfigurationProvider : ConfigurationProvider
{
    static Dictionary<string, string> Read()
    {
        using var mgr = new UpdateManager("");

        using var configFile = new FileStream($"{(mgr.IsInstalledApp ? mgr.AppDirectory + "/" : "")}App.ini", FileMode.OpenOrCreate, FileAccess.Read);

        var data = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        using (var reader = new StreamReader(configFile))
        {
            string sectionPrefix = "";

            while (reader.Peek() != -1)
            {
                var rawLine = reader.ReadLine()!; // Since Peak didn't return -1, stream hasn't ended.
                var line = rawLine.Trim();

                // Ignore blank lines
                if (string.IsNullOrWhiteSpace(line))
                {
                    continue;
                }

                // Ignore comments
                if (line[0] is ';' or '#' or '/')
                {
                    continue;
                }

                // [Section:header]
                if (line[0] == '[' && line[^1] == ']')
                {
                    sectionPrefix = string.Concat(line.AsSpan(1, line.Length - 2).Trim(), ConfigurationPath.KeyDelimiter);

                    continue;
                }

                // key = value OR "value"
                var separator = line.IndexOf('=');
                if (separator < 0)
                {
                    throw new FormatException($"Invalid format: {rawLine}");
                }

                var key = sectionPrefix + line[..separator].Trim();
                var value = line[(separator + 1)..].Trim();

                // Remove quotes
                if (value.Length > 1 && value[0] == '"' && value[^1] == '"')
                {
                    value = value.Substring(1, value.Length - 2);
                }

                if (data.ContainsKey(key))
                {
                    throw new FormatException($"Invalid format: {key}");
                }

                data[key] = value;
            }
        }

        return data;
    }

    public void Write(ConnectionSettings settings)
    {
        var defaultSettings = new ConnectionSettings();

        var oldSettings = new ConnectionSettings
        {
            Host = Data.TryGetValue(nameof(ConnectionSettings.Host), out var host) ? host : defaultSettings.Host,
            Username = Data.TryGetValue(nameof(ConnectionSettings.Username), out var username) ? username : defaultSettings.Username,
            Password = Data.TryGetValue(nameof(ConnectionSettings.Password), out var password) ? password : defaultSettings.Password,
            WebPort = Data.TryGetValue(nameof(ConnectionSettings.WebPort), out var webPort) ? Convert.ToInt32(webPort) : defaultSettings.WebPort,
            RtspPort = Data.TryGetValue(nameof(ConnectionSettings.RtspPort), out var rtspPort) ? Convert.ToInt32(rtspPort) : defaultSettings.RtspPort,
            DataPort = Data.TryGetValue(nameof(ConnectionSettings.DataPort), out var dataPort) ? Convert.ToInt32(dataPort) : defaultSettings.DataPort,
        };

        WriteToFile(settings);

        Data.Clear();

        Data.Add(nameof(ConnectionSettings.Host), settings.Host);
        Data.Add(nameof(ConnectionSettings.Username), settings.Username);
        Data.Add(nameof(ConnectionSettings.Password), settings.Password);
        Data.Add(nameof(ConnectionSettings.WebPort), settings.WebPort.ToString());
        Data.Add(nameof(ConnectionSettings.RtspPort), settings.RtspPort.ToString());
        Data.Add(nameof(ConnectionSettings.DataPort), settings.DataPort.ToString());

        if (oldSettings != settings)
        {
            OnReload();
        }
    }

    static void WriteToFile(ConnectionSettings settings)
    {
        var config = new StringBuilder();

        config.AppendLine($"{nameof(ConnectionSettings.Host)}={settings.Host.Trim()}");
        config.AppendLine($"{nameof(ConnectionSettings.Username)}={settings.Username.Trim()}");
        config.AppendLine($"{nameof(ConnectionSettings.Password)}={settings.Password.Trim()}");
        config.AppendLine($"{nameof(ConnectionSettings.WebPort)}={settings.WebPort}");
        config.AppendLine($"{nameof(ConnectionSettings.RtspPort)}={settings.RtspPort}");
        config.AppendLine($"{nameof(ConnectionSettings.DataPort)}={settings.DataPort}");

        using var mgr = new UpdateManager("");

        using var configFile = new FileStream($"{(mgr.IsInstalledApp ? mgr.AppDirectory + "/" : "")}App.ini", FileMode.Create, FileAccess.ReadWrite);

        configFile.Write(Encoding.UTF8.GetBytes(config.ToString()));
        configFile.Close();
    }

    public override void Load()
    {
        var defaultSettings = new ConnectionSettings();

        var oldSettings = new ConnectionSettings
        {
            Host = Data.TryGetValue(nameof(ConnectionSettings.Host), out var host) ? host : defaultSettings.Host,
            Username = Data.TryGetValue(nameof(ConnectionSettings.Username), out var username) ? username : defaultSettings.Username,
            Password = Data.TryGetValue(nameof(ConnectionSettings.Password), out var password) ? password : defaultSettings.Password,
            WebPort = Data.TryGetValue(nameof(ConnectionSettings.WebPort), out var webPort) ? Convert.ToInt32(webPort) : defaultSettings.WebPort,
            RtspPort = Data.TryGetValue(nameof(ConnectionSettings.RtspPort), out var rtspPort) ? Convert.ToInt32(rtspPort) : defaultSettings.RtspPort,
            DataPort = Data.TryGetValue(nameof(ConnectionSettings.DataPort), out var dataPort) ? Convert.ToInt32(dataPort) : defaultSettings.DataPort,
        };

        Data = Read();

        var newSettings = new ConnectionSettings
        {
            Host = Data.TryGetValue(nameof(ConnectionSettings.Host), out host) ? host : defaultSettings.Host,
            Username = Data.TryGetValue(nameof(ConnectionSettings.Username), out username) ? username : defaultSettings.Username,
            Password = Data.TryGetValue(nameof(ConnectionSettings.Password), out password) ? password : defaultSettings.Password,
            WebPort = Data.TryGetValue(nameof(ConnectionSettings.WebPort), out webPort) ? Convert.ToInt32(webPort) : defaultSettings.WebPort,
            RtspPort = Data.TryGetValue(nameof(ConnectionSettings.RtspPort), out rtspPort) ? Convert.ToInt32(rtspPort) : defaultSettings.RtspPort,
            DataPort = Data.TryGetValue(nameof(ConnectionSettings.DataPort), out dataPort) ? Convert.ToInt32(dataPort) : defaultSettings.DataPort,
        };

        if (oldSettings != newSettings)
        {
            OnReload();
        }
    }
}
