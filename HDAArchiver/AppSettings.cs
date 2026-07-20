using System;
using System.IO;
using System.Xml.Serialization;

namespace HDAArchiver
{
    [Serializable]
    public class AppSettings
    {
        public bool CompressedByDefault { get; set; }
            = true;

        public bool ConfirmOnDelete { get; set; }
            = true;

        public bool ShowFileSize { get; set; }
            = true;

        public bool ShowCompressedSize { get; set; }
            = true;

        public string LastOpenFolder { get; set; }
            = "";

        public string LastExtractFolder { get; set; }
            = "";

        // ── Singleton ──────────────────────────────
        private static AppSettings _instance;

        private static string SettingsPath =>
            Path.Combine(
                AppDomain.CurrentDomain
                         .BaseDirectory,
                "HDAArchiver.settings.xml");

        public static AppSettings Instance
        {
            get
            {
                if (_instance == null)
                    _instance = Load();
                return _instance;
            }
        }

        public static AppSettings Load()
        {
            try
            {
                if (File.Exists(SettingsPath))
                {
                    var xs =
                        new XmlSerializer(
                            typeof(AppSettings));
                    using (var fs =
                        File.OpenRead(SettingsPath))
                    {
                        return (AppSettings)
                            xs.Deserialize(fs);
                    }
                }
            }
            catch { }

            return new AppSettings();
        }

        public void Save()
        {
            try
            {
                var xs =
                    new XmlSerializer(
                        typeof(AppSettings));
                using (var fs =
                    File.Create(SettingsPath))
                {
                    xs.Serialize(fs, this);
                }
            }
            catch { }
        }
    }
}