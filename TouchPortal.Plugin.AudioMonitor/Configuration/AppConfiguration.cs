using System;
using System.Configuration;

namespace TouchPortal.Plugin.AudioMonitor.Configuration
{
    public class AppConfiguration
    {
        public int Width
        {
            get => ParseInt(GetValue(nameof(Width)), 100);
            set => SetValue(nameof(Width), value.ToString());
        }

        public int Height
        {
            get => ParseInt(GetValue(nameof(Height)), 100);
            set => SetValue(nameof(Height), value.ToString());
        }

        private int ParseInt(string value, int defaultValue)
        {
            return int.TryParse(value, out var output)
                ? output
                : defaultValue;
        }

        public string GetValue(string key)
        {
            try
            {
                ConfigurationManager.RefreshSection("appSettings");
                var configuration = ConfigurationManager.OpenExeConfiguration(ConfigurationUserLevel.None);

                var config = configuration;
                var keyValue = config.AppSettings.Settings[key];
                if (keyValue is null)
                    return null;

                return keyValue.Value;
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                return null;
            }
        }

        public void SetValue(string key, string value)
        {
            try
            {
                var configuration = ConfigurationManager.OpenExeConfiguration(ConfigurationUserLevel.None);
                var appSettings = configuration.AppSettings;
                var setting = appSettings.Settings;
                var keyValue = setting[key];

                if (keyValue is null)
                    setting.Add(key, value);
                else
                    keyValue.Value = value;

                appSettings.SectionInformation.ForceSave = true;

                configuration.Save(ConfigurationSaveMode.Modified);
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
            }
        }
    }
}
