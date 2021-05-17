using NAudio.CoreAudioApi;
using System;
using NAudio.Wave;

namespace TouchPortal.Plugin.AudioMonitor.Capture
{
    public class CaptureSession : IDisposable
    {
        //true or false is about the same... one less thread per output source on false.
        private const bool FEATURE_DATA_AVAILABLE = true;

        private readonly MMDevice _mmDevice;
        private readonly WasapiCapture _recorder;

        private float _max;

        private CaptureSession(MMDevice mmDevice, WasapiCapture recorder)
        {
            _mmDevice = mmDevice;
            _recorder = recorder;
            if (FEATURE_DATA_AVAILABLE)
            {
                if (_recorder != null)
                    _recorder.DataAvailable += DataAvailableEvent;
            }
        }

        public float MeasurePeakValue()
        {
            if (FEATURE_DATA_AVAILABLE)
            {
                var value = _max;
                _max = 0; //Reset
                return value;
            }
            return _mmDevice.AudioMeterInformation.MasterPeakValue;
        }
        
        public static CaptureSession FromAudioOutput(MMDevice mmDevice)
        {
            if (FEATURE_DATA_AVAILABLE)
            {
                var recorder = new WasapiLoopbackCapture(mmDevice);
                recorder.StartRecording();

                return new CaptureSession(mmDevice, recorder);
            }
            return new CaptureSession(mmDevice, null);
        }

        public static CaptureSession FromAudioInput(MMDevice mmDevice)
        {
            //If already recording:
            //InvalidOperationException::Message = "Previous recording still in progress"
            var recorder = new WasapiCapture(mmDevice);
            recorder.StartRecording();

            return new CaptureSession(mmDevice, recorder);
        }
        
        //From Docs: https://github.com/naudio/NAudio/blob/master/Docs/RecordingLevelMeter.md
        private void DataAvailableEvent(object obj, WaveInEventArgs args)
        {
            var buffer = new WaveBuffer(args.Buffer);
            
            for (var index = 0; index < args.BytesRecorded / 4; index++)
            {
                var sample = buffer.FloatBuffer[index];
                sample = Math.Abs(sample);
                _max = Math.Max(sample, _max);
            }
        }

        public void Dispose()
        {
            _recorder?.Dispose();
            _mmDevice?.Dispose();
        }
    }
}
