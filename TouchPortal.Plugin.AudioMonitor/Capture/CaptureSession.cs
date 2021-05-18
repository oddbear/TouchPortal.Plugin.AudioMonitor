using NAudio.CoreAudioApi;
using System;
using NAudio.Wave;

namespace TouchPortal.Plugin.AudioMonitor.Capture
{
    public class CaptureSession : IDisposable
    {
        private readonly object _lock = new object();

        private readonly MMDevice _mmDevice;
        private readonly WasapiCapture _recorder;

        private float _max;

        private CaptureSession(MMDevice mmDevice, WasapiCapture recorder)
        {
            _mmDevice = mmDevice;
            _recorder = recorder;
        }

        public float MeasurePeakValue()
        {
            lock (_lock)
            {
                var value = _max;
                _max = 0; //Reset value after read.
                return value;
            }
        }
        
        public static CaptureSession FromAudioOutput(MMDevice mmDevice)
        {
            var recorder = new WasapiLoopbackCapture(mmDevice);

            var session = new CaptureSession(mmDevice, recorder);
            recorder.DataAvailable += session.DataAvailableEvent;
            recorder.StartRecording();
            return session;
        }

        public static CaptureSession FromAudioInput(MMDevice mmDevice)
        {
            var recorder = new WasapiCapture(mmDevice);

            var session = new CaptureSession(mmDevice, recorder);
            recorder.DataAvailable += session.DataAvailableEvent;
            recorder.StartRecording();
            return session;
        }
        
        //From Docs: https://github.com/naudio/NAudio/blob/master/Docs/RecordingLevelMeter.md
        private void DataAvailableEvent(object obj, WaveInEventArgs args)
        {
            var buffer = new WaveBuffer(args.Buffer);

            var max = 0f;
            for (var index = 0; index < args.BytesRecorded / 4; index++)
            {
                var sample = buffer.FloatBuffer[index];
                sample = Math.Abs(sample);

                max = Math.Max(sample, max);
            }

            lock (_lock)
            {
                _max = Math.Max(max, _max);
            }
        }

        public void Dispose()
        {
            _recorder?.Dispose();
            _mmDevice?.Dispose();
        }
    }
}
