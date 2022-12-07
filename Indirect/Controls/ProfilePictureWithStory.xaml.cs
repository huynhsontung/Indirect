using Indirect.Entities;
using InstagramAPI.Classes.User;
using System.Collections.ObjectModel;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Media;

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
            this.InitializeComponent();
        }

        private double GetIndicatorStrokeThickness(bool unseen) => unseen ? 2 : 1;

        private Brush GetIndicatorStrokeBrush(bool unseen) => unseen ? (Brush)Resources["StoryBrush"] : (Brush)App.Current.Resources["ControlElevationBorderBrush"];

        private Thickness GetPictureMargin(bool hasReel) => hasReel ? new Thickness(4) : default;
    }
}
