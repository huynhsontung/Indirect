using Indirect.Entities;
using InstagramAPI.Classes.User;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
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

// The User Control item template is documented at https://go.microsoft.com/fwlink/?LinkId=234236

namespace Indirect.Controls
{
    public sealed partial class ProfilePictureWithStory : UserControl
    {
        public static readonly DependencyProperty SourceProperty = DependencyProperty.Register(
            nameof(Source),
            typeof(ObservableCollection<BaseUser>),
            typeof(ProfilePicture),
            new PropertyMetadata(null));

        public ObservableCollection<BaseUser> Source
        {
            get => (ObservableCollection<BaseUser>)GetValue(SourceProperty);
            set
            {
                SetValue(SourceProperty, value);
                ViewModel.Users = value;
            }

        }

        private ProfilePictureWithStoryViewModel ViewModel { get; }

        public ProfilePictureWithStory()
        {
            DataContext = ViewModel = new ProfilePictureWithStoryViewModel();
            ViewModel.PropertyChanged += ViewModel_PropertyChanged;
            this.InitializeComponent();
        }

        private double GetIndicatorStrokeThickness(bool unseen) => unseen ? 2 : 1;

        private Brush GetIndicatorStrokeBrush(bool unseen) => unseen ? (Brush)Resources["StoryBrush"] : (Brush)App.Current.Resources["ControlElevationBorderBrush"];

        private void ViewModel_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(ViewModel.HasReel))
            {
                UpdatePictureSize();
            }
        }

        private void ProfilePictureWithStory_OnSizeChanged(object sender, SizeChangedEventArgs e)
        {
            UpdatePictureSize();
        }

        private void UpdatePictureSize()
        {
            if (ViewModel.HasReel)
            {
                Picture.Width = ActualWidth - 8;
                Picture.Height = ActualHeight - 8;
            }
            else
            {
                Picture.Width = double.NaN;
                Picture.Height = double.NaN;
            }
        }
    }
}
