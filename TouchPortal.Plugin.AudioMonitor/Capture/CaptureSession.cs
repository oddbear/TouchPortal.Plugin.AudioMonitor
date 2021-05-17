using NAudio.CoreAudioApi;
using System;
using NAudio.Wave;
using TouchPortal.Plugin.AudioMonitor.Models;

namespace TouchPortal.Plugin.AudioMonitor.Capture
{
    public class CaptureSession : IDisposable
    {
        private readonly MMDevice _mmDevice;
        private readonly WasapiCapture _recorder;
        
        private CaptureSession(MMDevice mmDevice, WasapiCapture recorder)
        {
            _mmDevice = mmDevice;
            _recorder = recorder;
        }

        public float MeasurePeakValue()
            => _mmDevice.AudioMeterInformation.MasterPeakValue;

        public void StartMonitor()
        {
            if (_recorder.CaptureState == CaptureState.Stopped || _recorder.CaptureState == CaptureState.Stopping)
            {
                try
                {
                    _recorder.StartRecording();
                }
                catch (Exception e)
                {
                    Console.WriteLine(e);
                }
            }
        }

        public void StopMonitoring()
        {
            if (_recorder.CaptureState == CaptureState.Capturing || _recorder.CaptureState == CaptureState.Starting)
            {
                try
                {
                    _recorder.StopRecording();
                }
                catch (Exception e)
                {
                    Console.WriteLine(e);
                }
            }
        }

        public static CaptureSession FromAudioOutput(MMDevice mmDevice)
        {
            var recorder = new WasapiLoopbackCapture();
            //TODO: Maybe make my own, if I then can adjust the buffers etc. (only sets flags AudioClientStreamFlags.Loopback etc.)
            return new CaptureSession(mmDevice, recorder);
        }

        public static CaptureSession FromAudioInput(MMDevice mmDevice)
        {
            //If already recording:
            //InvalidOperationException::Message = "Previous recording still in progress"
            var recorder = new WasapiCapture(mmDevice);
            recorder.StartRecording();

            return new CaptureSession(mmDevice, recorder);
        }

        public void Dispose()
        {
            _recorder?.Dispose();
            _mmDevice?.Dispose();
        }
    }
}
