using System;
using System.Threading;
using System.Threading.Tasks;
using Windows.Storage;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Indirect.Entities;
using Indirect.Services;

// The User Control item template is documented at https://go.microsoft.com/fwlink/?LinkId=234236

namespace Indirect.Controls
{
    public sealed partial class SendAudioControl : UserControl
    {
        private enum Operation
        {
            RecordReady,
            Recording,
            Stopped
        }

        private static Flyout _openFlyout;
        private readonly AudioRecorder _recorder;
        private Operation _operation;
        private AudioWithWaveform _audio;
        private AudioWithWaveform _returnAudio;
        private CancellationTokenSource _tokenSource;

        public static Task<AudioWithWaveform> ShowAsync(FrameworkElement placementTarget, FlyoutShowOptions showOptions)
        {
            var flyout = _openFlyout = new Flyout();
            var instance = new SendAudioControl();
            flyout.Content = instance;
            var tcs = new TaskCompletionSource<AudioWithWaveform>();

            async void OnFlyoutOnClosed(object sender, object o)
            {
                if (instance._operation == Operation.Recording)
                {
                    instance._audio = await instance._recorder.StopAsync();
                    if (!(instance._tokenSource?.IsCancellationRequested ?? true))
                    {
                        instance._tokenSource.Cancel();
                    }
                }

                instance._recorder.Dispose();
                tcs.SetResult(instance._returnAudio);

                if (instance._audio != null && instance._returnAudio == null)
                {
                    try
                    {
                        await instance._audio.AudioFile.DeleteAsync(StorageDeleteOption.PermanentDelete);
                    }
                    catch (Exception)
                    {
                        // pass
                    }
                }
            }

            flyout.Closed += OnFlyoutOnClosed;

            flyout.ShowAt(placementTarget, showOptions);
            instance.MainButton_OnClick(instance.MainButton, null);

            return tcs.Task;
        }

        private SendAudioControl()
        {
            this.InitializeComponent();
            _recorder = new AudioRecorder();
        }

        private async void MainButton_OnClick(object sender, RoutedEventArgs e)
        {
            switch (_operation)
            {
                case Operation.RecordReady:
                    if (await _recorder.InitializeAsync())
                    {
                        _operation = Operation.Recording;
                        _recorder.Start();
                        VisualStateManager.GoToState(this, "Recording", true);
                        MainButton.IsEnabled = false;
                        await StartTimer();
                    }
                    else
                    {
                        ErrorInfo.Message = _recorder.ExtendedError?.ToString();
                        ErrorInfo.IsOpen = true;
                    }
                    break;
                case Operation.Recording:
                    _audio = await _recorder.StopAsync();
                    _operation = Operation.Stopped;
                    AudioPlayer.Source = _audio.AudioFile;
                    VisualStateManager.GoToState(this, "Stopped", true);
                    if (!(_tokenSource?.IsCancellationRequested ?? true))
                    {
                        _tokenSource.Cancel();
                    }
                    break;
                case Operation.Stopped:
                    _returnAudio = _audio;
                    _openFlyout.Hide();
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        private async Task StartTimer()
        {
            var startTime = DateTimeOffset.Now;
            using (var tokenSource = _tokenSource = new CancellationTokenSource())
            {
                for (int i = 0; i < 60; i++)
                {
                    if (tokenSource.IsCancellationRequested)
                    {
                        return;
                    }

                    try
                    {
                        await Task.Delay(1000, tokenSource.Token);
                    }
                    catch (TaskCanceledException)
                    {
                        return;
                    }

                    MainButton.IsEnabled = true;

                    var duration = DateTimeOffset.Now - startTime;
                    DurationText.Text = $"{duration.Minutes}:{duration.Seconds:00}";
                    DurationRing.Value = duration.TotalSeconds / 60 * 100;
                }

                tokenSource.Cancel();
                MainButton_OnClick(MainButton, null);
            }
        }
    }
}
