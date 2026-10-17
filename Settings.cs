using System;
using System.IO;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Json;

namespace L14KeyboardLight
{
    [DataContract]
    public sealed class Settings
    {
        [DataMember] public int PreferredLevel = 2;
        [DataMember] public bool AutoEnabled = true;
        public static readonly string Folder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "L14KeyboardLight");
        public static readonly string FilePath = Path.Combine(Folder, "settings.json");
        public static readonly string LogPath = Path.Combine(Folder, "diagnostics.log");

        public static Settings Load(int initialLevel)
        {
            if (File.Exists(FilePath))
            {
                try
                {
                    using (FileStream stream = File.OpenRead(FilePath))
                    {
                        Settings settings = (Settings)new DataContractJsonSerializer(typeof(Settings)).ReadObject(stream);
                        if (settings != null && settings.PreferredLevel >= 0 && settings.PreferredLevel <= 2) return settings;
                    }
                }
                catch (Exception error) { Log("Beállítások olvasása: " + error.Message); }
            }
            return new Settings { PreferredLevel = initialLevel };
        }

        public void Save()
        {
            Directory.CreateDirectory(Folder);
            string temp = FilePath + ".tmp";
            using (FileStream stream = File.Create(temp))
                new DataContractJsonSerializer(typeof(Settings)).WriteObject(stream, this);
            if (File.Exists(FilePath)) File.Replace(temp, FilePath, null);
            else File.Move(temp, FilePath);
        }

        public static void Log(string message)
        {
            try
            {
                Directory.CreateDirectory(Folder);
                if (File.Exists(LogPath) && new FileInfo(LogPath).Length > 256 * 1024)
                {
                    string previous = LogPath + ".old";
                    if (File.Exists(previous)) File.Delete(previous);
                    File.Move(LogPath, previous);
                }
                File.AppendAllText(LogPath, DateTime.Now.ToString("s") + " " + message + Environment.NewLine);
            }
            catch { }
        }
    }
}
