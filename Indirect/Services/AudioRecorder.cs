using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using Windows.Media;
using Windows.Media.Audio;
using Windows.Media.Capture;
using Windows.Media.MediaProperties;
using Windows.Media.Render;
using Windows.Media.Transcoding;
using Windows.Storage;
using Indirect.Entities;
using InstagramAPI.Utils;

namespace Indirect.Services
{
    internal class AudioRecorder : IDisposable
    {
        [ComImport]
        [Guid("5B0D3235-4DBA-4D44-865E-8F1D0E4FD04D")]
        [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
        private unsafe interface IMemoryBufferByteAccess
        {
            void GetBuffer(out byte* buffer, out uint capacity);
        }

        public Exception ExtendedError { get; private set; }

        private static readonly StorageFolder SaveFolder = ApplicationData.Current.TemporaryFolder;
        private readonly List<float> _waveform;
        private AudioGraph _audioGraph;
        private AudioFrameOutputNode _frameOutputNode;
        private AudioFileOutputNode _fileOutputNode;
        private StorageFile _audioFile;
        private uint _sampleCount;

        public AudioRecorder()
        {
            _waveform = new List<float>();
        }

        public async Task<bool> InitializeAsync()
        {
            _sampleCount = 0;
            _audioGraph?.Dispose();
            _waveform.Clear();
            var settings = new AudioGraphSettings(AudioRenderCategory.Speech);
            CreateAudioGraphResult result = await AudioGraph.CreateAsync(settings);
            if (result.Status != AudioGraphCreationStatus.Success)
            {
                ExtendedError = result.ExtendedError;
                return false;
            }

            var audioGraph = _audioGraph = result.Graph;

            try
            {
                CreateAudioDeviceInputNodeResult inputNodeResult = await audioGraph.CreateDeviceInputNodeAsync(MediaCategory.Speech);
                if (inputNodeResult.Status != AudioDeviceNodeCreationStatus.Success)
                {
                    ExtendedError = inputNodeResult.ExtendedError;
                    return false;
                }

                var inputNode = inputNodeResult.DeviceInputNode;

                var file = _audioFile = await SaveFolder.CreateFileAsync($"{DateTimeOffset.Now.Ticks}.m4a", CreationCollisionOption.GenerateUniqueName);
                CreateAudioFileOutputNodeResult fileOutputNodeResult = await audioGraph.CreateFileOutputNodeAsync(file, MediaEncodingProfile.CreateM4a(AudioEncodingQuality.Medium));
                if (fileOutputNodeResult.Status != AudioFileNodeCreationStatus.Success)
                {
                    ExtendedError = fileOutputNodeResult.ExtendedError;
                    return false;
                }

                var fileOutputNode = _fileOutputNode = fileOutputNodeResult.FileOutputNode;
                var frameOutputNode = _frameOutputNode = audioGraph.CreateFrameOutputNode();
                inputNode.AddOutgoingConnection(fileOutputNode);
                inputNode.AddOutgoingConnection(frameOutputNode);
                audioGraph.QuantumStarted += AudioGraphOnQuantumStarted;
                return true;
            }
            catch (ObjectDisposedException e)
            {
                ExtendedError = e;
                return false;
            }
        }

        public void Start()
        {
            _audioGraph?.Start();
        }

        public async Task<AudioWithWaveform> StopAsync()
        {
            try
            {
                _audioGraph.Stop();
                _fileOutputNode.Stop();
                var errorReason = await _fileOutputNode.FinalizeAsync();
                return errorReason != TranscodeFailureReason.None
                    ? null
                    : new AudioWithWaveform { AudioFile = _audioFile, Waveform = _waveform };
            }
            catch (Exception e)
            {
                ExtendedError = e;
                DebugLogger.LogException(e);
                return null;
            }
        }

        private void AudioGraphOnQuantumStarted(AudioGraph sender, object args)
        {
            _sampleCount++;
            if (_audioGraph.CompletedQuantumCount == 1 || _sampleCount % 10 != 0) return;
            var frame = _frameOutputNode.GetFrame();
            ProcessAudioFrame(frame);
        }

        private unsafe void ProcessAudioFrame(AudioFrame frame)
        {
            using (var buffer = frame.LockBuffer(AudioBufferAccessMode.Read))
            {
                using (var reference = buffer.CreateReference())
                {
                    ((IMemoryBufferByteAccess)reference).GetBuffer(out var dataInBytes, out var capacity);
                    float* dataInFloat = (float*)dataInBytes;
                    uint capacityInFloat = capacity / sizeof(float);
                    if (capacityInFloat <= 0) return;
                    double sample = 0.0;
                    for (int i = 0; i < capacityInFloat; i++)
                    {
                        sample += Math.Pow(dataInFloat[i], 2);
                    }

                    sample /= capacityInFloat;
                    sample = Math.Sqrt(sample);
                    _waveform.Add((float)sample);
                }
            }
        }

        public void Dispose()
        {
            _audioGraph?.Dispose();
        }
    }
}
