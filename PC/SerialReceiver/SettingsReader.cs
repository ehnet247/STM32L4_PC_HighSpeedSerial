using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace SerialReceiver
{
    public class SettingsReader
    {
        public readonly string FileName;
        public Settings? Default { get; private set; } = null;
        public SettingsReader(string fileName = "settings.json")
        {
            FileName = fileName;
            Restore();
        }

        public void Restore()
        {
            if (!string.IsNullOrEmpty(FileName) && System.IO.File.Exists(FileName))
            {
                var jsonData = System.IO.File.ReadAllText(FileName);
                var settingsRead = System.Text.Json.JsonSerializer.Deserialize<Settings>(jsonData);
                if (settingsRead != null)
                {
                    Default = settingsRead;
                }
            }
            else
            {
                Default = new Settings();
            }
        }

        public void Save()
        {
            if (Default != null)
            {
                if (File.Exists(FileName))
                {
                    File.Delete(FileName);
                }
                var jsonData = System.Text.Json.JsonSerializer.Serialize(Default, new System.Text.Json.JsonSerializerOptions { WriteIndented = true });
        System.IO.File.WriteAllText(FileName, jsonData);
            }
        }
    }

    public class Settings
    {
        public string? LastComPort { get; set; } = null;
        public double? LastBaudRate { get; set; } = null;
    }
}
