using System;
using System.Linq;
using System.Threading.Tasks;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Indirect.Entities;
using Indirect.Services;
using InstagramAPI.Utils;

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

        public static Task<AudioWithWaveform> ShowAsync(FrameworkElement placementTarget, FlyoutShowOptions showOptions)
        {
            var flyout = _openFlyout = new Flyout();
            var instance = new SendAudioControl();
            flyout.Content = instance;
            var tcs = new TaskCompletionSource<AudioWithWaveform>();

            flyout.Closed += (sender, o) =>
            {
                instance._recorder.Dispose();
                tcs.SetResult(instance._audio);
            };

            flyout.ShowAt(placementTarget, showOptions);

            return tcs.Task;
        }

        private SendAudioControl()
        {
            this.InitializeComponent();
            _recorder = new AudioRecorder();
        }

        private void ClearButton_OnClick(object sender, RoutedEventArgs e)
        {
            if (_operation != Operation.Stopped) return;

            _operation = Operation.RecordReady;
            _audio = null;
            Timer.Text = "0s";
            AudioPlayer.Pause();
            AudioPlayer.Source = null;
            VisualStateManager.GoToState(this, "RecordReady", false);
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
                        VisualStateManager.GoToState(this, "Recording", false);
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
                    this.Log(_audio.Waveform.Count);
                    this.Log(string.Join(", ", _audio.Waveform.Select(f => f.ToString())));
                    VisualStateManager.GoToState(this, "Stopped", false);
                    break;
                case Operation.Stopped:
                    _openFlyout.Hide();
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }
    }
}
