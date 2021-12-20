using System.IO;

using DatabaseInterpreter.Model;

using Newtonsoft.Json;

namespace DatabaseInterpreter.Core
{
    public class SettingManager : ConfigManager
    {
        public static Setting Setting { get; private set; } = new Setting();

        static SettingManager()
        {
            LoadConfig();
        }

        public static string ConfigFilePath
        {
            get
            {
                return Path.Combine(ConfigRootFolder, "Setting.json");
            }
        }

        public static void LoadConfig()
        {
            if (File.Exists(ConfigFilePath))
            {
                Setting = (Setting)JsonConvert.DeserializeObject(File.ReadAllText(ConfigFilePath), typeof(Setting));
            }
        }

        public static void SaveConfig(Setting setting)
        {
            Setting = setting;
            string content = JsonConvert.SerializeObject(setting, Formatting.Indented);

            File.WriteAllText(ConfigFilePath, content);
        }
    }
}
