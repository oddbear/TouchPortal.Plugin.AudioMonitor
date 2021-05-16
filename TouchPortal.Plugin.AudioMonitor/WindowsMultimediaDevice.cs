using System;
using System.Threading;
using NAudio.CoreAudioApi;
using NAudio.Wave;

namespace TouchPortal.Plugin.AudioMonitor
{
    public class WindowsMultimediaDevice : IDisposable
    {
        private readonly IPluginCallbacks _callbacks;

        private const bool FEATURE_DATA_AVAILABLE = true;

        private MMDevice _mmDevice;
        private WasapiCapture _recorder;
        private Thread _monitoringThread;

        public bool IsMonitoring { get; private set; }

        public WindowsMultimediaDevice(IPluginCallbacks callbacks)
        {
            _callbacks = callbacks ?? throw new ArgumentNullException(nameof(callbacks));

            ClearMonitoring();
        }

        public bool SetMultimediaDevice(string deviceName, int deviceOffset)
        {
            ClearMonitoring();
            _mmDevice = null;
            _recorder?.Dispose();

            var enumerator = new MMDeviceEnumerator();
            //DataFlow.Capture -> Microphone/Input
            //DataFlow.Render -> Speakers/Output
            var devices = enumerator
                .EnumerateAudioEndPoints(DataFlow.Capture, DeviceState.Active);
            
            if (string.IsNullOrWhiteSpace(deviceName))
                deviceName = enumerator
                    .GetDefaultAudioEndpoint(DataFlow.Capture, Role.Multimedia)
                    .FriendlyName;

            for (var i = 0; i < devices.Count; i++)
            {
                if (!devices[i].FriendlyName.Contains(deviceName))
                    continue;

                var index = GetIndex(i + deviceOffset, devices.Count);
                    
                _mmDevice = devices[index];

                _recorder = new WasapiCapture(_mmDevice);
                if (FEATURE_DATA_AVAILABLE)
                {
                    _recorder.DataAvailable += DataAvailableEvent;
                }
                _recorder.StartRecording();

                _callbacks.MultimediaDeviceUpdateCallback(_mmDevice.FriendlyName);

                return true;
            }
            
            return false;
        }

        private  int GetIndex(int offset, int len)
        {
            var index = offset % len;
            if (index < 0)
                index += len;

            return index;
        }

        public void StartMonitoring()
        {
            ClearMonitoring();

            if (!FEATURE_DATA_AVAILABLE)
            {
                _monitoringThread = new Thread(Monitoring)
                {
                    IsBackground = true
                };
                _monitoringThread.Start();
            }

            IsMonitoring = true;
        }
        
        public void ClearMonitoring()
        {
            _monitoringThread?.Interrupt();
            
            IsMonitoring = false;
        }

        public void ToggleMonitoring()
        {
            if (IsMonitoring)
                ClearMonitoring();
            else
                StartMonitoring();
        }

        //TODO: Make a Windows tester (I think it's the latency between the Android device and this that are the biggest issue):
        //From Docs: https://github.com/naudio/NAudio/blob/master/Docs/RecordingLevelMeter.md
        //With this, WasapiCapture have to be changed for WasapiLoopbackCapture if Render mode (outputs):
        private int _count = 0;
        private double _temp = double.MinValue;

        private void DataAvailableEvent(object obj, WaveInEventArgs args)
        {
            var max = 0f;
            var buffer = new WaveBuffer(args.Buffer);

            // interpret as 32 bit floating point audio (WasapiIn)
            for (var index = 0; index < args.BytesRecorded / 4; index++)
            {
                //Ignore, this situation is ok.
                var sample = buffer.FloatBuffer[index];

                // absolute value 
                if (sample < 0)
                    sample = -sample;

                // is this the max value?
                if (sample > max)
                    max = sample;
            }

            var decibel = Math.Log10(max) * 20;
            decibel = Math.Round(decibel);

            //It's to fast (easier to test this way than extending the buffers etc.):
            if (_count < 2)
            {
                _count++;
                if (decibel > _temp)
                    _temp = decibel;
            }
            else
            {
                _count = 0;
                _temp = double.MinValue;
                _callbacks.MonitoringCallback(decibel);
            }
        }

        private void Monitoring()
        {
            try
            {
                while (_mmDevice != null)
                {
                    var masterPeakValue = _mmDevice.AudioMeterInformation.MasterPeakValue;
                    
                    var decibel = Math.Log10(masterPeakValue) * 20;
                    decibel = Math.Round(decibel);
                    
                    _callbacks.MonitoringCallback(decibel);

                    Thread.Sleep(100); //Interrupted from waiting ... on change...
                }
            }
            catch (ThreadInterruptedException)
            {
                //Ignore, this situation is ok.
            }
        }

        public void Dispose()
        {
            _recorder.Dispose();
        }
    }
}
