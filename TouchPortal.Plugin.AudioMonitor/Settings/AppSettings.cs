﻿using System;
using System.Configuration;
using System.Drawing;
using System.IO;

namespace TouchPortal.Plugin.AudioMonitor.Settings
{
    public class AppSettings
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

        public Color ColorLineMax
        {
            get => GetValue(nameof(ColorLineMax), Color.Red);
            set => SetValue(nameof(ColorLineMax), ColorTranslator.ToHtml(value));
        }

        public Color ColorLinePrev
        {
            get => GetValue(nameof(ColorLinePrev), Color.Blue);
            set => SetValue(nameof(ColorLinePrev), ColorTranslator.ToHtml(value));
        }

        public Color ColorOverlay
        {
            get => GetValue(nameof(ColorOverlay), Color.FromArgb(0x30, 0x30, 0x30));
            set => SetValue(nameof(ColorOverlay), ColorTranslator.ToHtml(value));
        }

        public Color ColorBarMeterLow
        {
            get => GetValue(nameof(ColorBarMeterLow), Color.LightGreen);
            set => SetValue(nameof(ColorBarMeterLow), ColorTranslator.ToHtml(value));
        }

        public Color ColorBarMeterMid
        {
            get => GetValue(nameof(ColorBarMeterMid), Color.Yellow);
            set => SetValue(nameof(ColorBarMeterMid), ColorTranslator.ToHtml(value));
        }

        public Color ColorBarMeterHigh
        {
            get => GetValue(nameof(ColorBarMeterHigh), Color.DarkRed);
            set => SetValue(nameof(ColorBarMeterHigh), ColorTranslator.ToHtml(value));
        }

        public AppSettings()
        {
            _configuration = ConfigurationManager.OpenExeConfiguration(ConfigurationUserLevel.None);

            _configWatcher = new FileSystemWatcher
            {
                Path = Path.GetDirectoryName(_configuration.FilePath),
                Filter = Path.GetFileName(_configuration.FilePath),
                EnableRaisingEvents = true
            };

            //This will load twice, so just mark as "dirty":
            _configWatcher.Changed += (sender, args) => _configHasChanged = true;
        }
        
        private readonly FileSystemWatcher _configWatcher;
        private bool _configHasChanged;
        private Configuration _configuration;

        private Configuration GetConfiguration()
        {
            if (_configHasChanged)
            {
                //Will reload after save, but that is OK.
                try
                {
                    //Reload configuration:
                    _configuration = ConfigurationManager.OpenExeConfiguration(ConfigurationUserLevel.None);
                    //Config is read OK, so no need to read it again before it is marked as "dirty" again:
                    _configHasChanged = false;
                }
                catch (ConfigurationErrorsException e)
                    when (e.InnerException is IOException)
                {
                    //There is a lock on the file and we cannot read from it.
                    //Try using the old configuration instead.
                }
            }

            return _configuration;
        }

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
                var configuration = GetConfiguration();
                var keyValue = configuration.AppSettings.Settings[key];
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
                var configuration = GetConfiguration();
                var appSettings = configuration.AppSettings;
                var setting = appSettings.Settings;
                var keyValue = setting[key];

                var stringValue = value?.ToString() ?? string.Empty;
                if (keyValue is null)
                    setting.Add(key, stringValue);
                else
                    keyValue.Value = stringValue;

                appSettings.SectionInformation.ForceSave = true;
                
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
