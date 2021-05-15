using System;
using System.Configuration;
using System.Drawing;
using System.IO;

namespace TouchPortal.Plugin.AudioMonitor.Configuration
{
    public class AppConfiguration
    {
        public int Width
        {
            get => GetValue(nameof(Width), 100);
            set => SetValue(nameof(Width), value);
        }

        public int Height
        {
            get => GetValue(nameof(Height), 100);
            set => SetValue(nameof(Height), value);
        }
        
        public Color ColorBackground
        {
            get => GetValue(nameof(ColorBackground), Color.DarkGray);
            set => SetValue(nameof(ColorBackground), ColorTranslator.ToHtml(value));
        }

        public Color ColorMax
        {
            get => GetValue(nameof(ColorMax), Color.Red);
            set => SetValue(nameof(ColorMax), ColorTranslator.ToHtml(value));
        }

        public Color ColorPrev
        {
            get => GetValue(nameof(ColorPrev), Color.Blue);
            set => SetValue(nameof(ColorPrev), ColorTranslator.ToHtml(value));
        }

        public Color ColorLines
        {
            get => GetValue(nameof(ColorLines), Color.FromArgb(0x30, 0x30, 0x30));
            set => SetValue(nameof(ColorLines), ColorTranslator.ToHtml(value));
        }

        private System.Configuration.Configuration _prevConfiguration;
        
        public TType GetValue<TType>(string key, TType defaultValue)
        {
            var stringValue = GetValue(key);
            return TryConvert<TType>(stringValue, out var value)
                ? value
                : defaultValue;
        }

        public string GetValue(string key)
        {
            try
            {
                //TODO: Should we use a file watcher here instead?
                ConfigurationManager.RefreshSection("appSettings");
                var configuration = ConfigurationManager.OpenExeConfiguration(ConfigurationUserLevel.None);
                _prevConfiguration = configuration;
            }
            catch (ConfigurationErrorsException e)
                when (e.InnerException is IOException)
            {
                //There is a lock on the file and we cannot read from it.
                //Try using the old configuration instead.
            }

            try
            {
                var keyValue = _prevConfiguration.AppSettings.Settings[key];
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

        public void SetValue<TValue>(string key, TValue value)
        {
            try
            {
                var configuration = ConfigurationManager.OpenExeConfiguration(ConfigurationUserLevel.None);
                var appSettings = configuration.AppSettings;
                var setting = appSettings.Settings;
                var keyValue = setting[key];

                var stringValue = value?.ToString() ?? string.Empty;
                if (keyValue is null)
                    setting.Add(key, stringValue);
                else
                    keyValue.Value = stringValue;

                appSettings.SectionInformation.ForceSave = true;

                //TODO: What if the file is locked etc.?
                configuration.Save(ConfigurationSaveMode.Modified);
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
            }
        }

        private bool TryConvert<TType>(string stringValue, out TType value)
        {
            var type = typeof(TType);
            
            if (type == typeof(int))
            {
                if (int.TryParse(stringValue, out var intValue) && intValue is TType tTypeValue)
                {
                    value = tTypeValue;
                    return true;
                }
            }
            else if (type == typeof(Color))
            {
                try
                {
                    var color = ColorTranslator.FromHtml(stringValue);
                    if (!color.IsEmpty && color is TType tTypeValue)
                    {
                        value = tTypeValue;
                        return true;
                    }
                }
                catch (Exception e)
                {
                    Console.WriteLine(e);
                }
            }

            value = default;
            return false;
        }
    }
}
