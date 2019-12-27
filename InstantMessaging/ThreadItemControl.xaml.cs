using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;
using InstantMessaging.Wrapper;
using InstaSharper.Classes.Models.Direct;
using InstaSharper.Enums;

// The User Control item template is documented at https://go.microsoft.com/fwlink/?LinkId=234236

namespace InstantMessaging
{
    internal sealed partial class ThreadItemControl : UserControl, INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;

        private InstaDirectInboxItemWrapper _source;

        public InstaDirectInboxItemWrapper Source
        {
            get => _source;
            set
            {
                _source = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Source)));
            }
        }

        public ThreadItemControl()
        {
            this.InitializeComponent();
            // DataContextChanged += OnDataContextChanged;
            PropertyChanged += OnPropertyChanged;
        }

        private void OnPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName != nameof(Source)) return;
            this.Bindings.Update();
            MessageContentWithBorder.Visibility = Visibility.Collapsed;
            MessageContentNoBorder.Visibility = Visibility.Collapsed;
            ImageFrame.Visibility = Visibility.Collapsed;
            NotSupportedMessage.Visibility = Visibility.Collapsed;
            MediaFrame.Visibility = Visibility.Collapsed;
            switch (Source.ItemType)
            {
                case InstaDirectThreadItemType.Text:
                case InstaDirectThreadItemType.Link:
                case InstaDirectThreadItemType.Hashtag:
                    MessageContentWithBorder.Visibility = Visibility.Visible;
                    break;

                case InstaDirectThreadItemType.Like:
                    MessageContentNoBorder.Visibility = Visibility.Visible;
                    break;

                // case InstaDirectThreadItemType.MediaShare:
                //     break;
                case InstaDirectThreadItemType.Media when Source.Media.MediaType == InstaMediaType.Image:
                case InstaDirectThreadItemType.RavenMedia when
                    Source.RavenMedia?.MediaType == InstaMediaType.Image || Source.VisualMedia?.Media.MediaType == InstaMediaType.Image:
                    ImageFrame.Visibility = Visibility.Visible;
                    break;
                // case InstaDirectThreadItemType.ReelShare:
                //     break;
                // case InstaDirectThreadItemType.Placeholder:
                //     break;
                // case InstaDirectThreadItemType.StoryShare:
                //     break;

                case InstaDirectThreadItemType.ActionLog:
                    ItemContainer.Visibility = Visibility.Collapsed;
                    break;

                // case InstaDirectThreadItemType.Profile:
                //     break;
                // case InstaDirectThreadItemType.Location:
                //     break;
                // case InstaDirectThreadItemType.FelixShare:
                //     break;
                // case InstaDirectThreadItemType.VoiceMedia:
                //     break;
                case InstaDirectThreadItemType.AnimatedMedia:
                    MediaFrame.AutoPlay = true;
                    MediaFrame.AreTransportControlsEnabled = false;
                    MediaFrame.MediaPlayer.IsLoopingEnabled = true;
                    MediaFrame.Width = Source.AnimatedMedia.Media.Width;
                    MediaFrame.Height = Source.AnimatedMedia.Media.Height;
                    MediaFrame.Visibility = Visibility.Visible;
                    break;
                // case InstaDirectThreadItemType.LiveViewerInvite:
                //     break;
                default:
                    NotSupportedMessage.Visibility = Visibility.Visible;
                    break;
            }
        }

        private void ItemContainer_DoubleTapped(object sender, DoubleTappedRoutedEventArgs e)
        {
            var panel = (Panel)sender;
            var timestampTextBlock = panel.Children.Last();
            timestampTextBlock.Visibility = timestampTextBlock.Visibility == Visibility.Collapsed ? Visibility.Visible : Visibility.Collapsed;
        }

    }
}
