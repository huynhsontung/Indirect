using System;
using System.Collections.ObjectModel;
using System.Threading;
using System.Threading.Tasks;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Indirect.Entities.Wrappers;
using Indirect.Services;
using Indirect.Utilities;
using InstagramAPI;
using InstagramAPI.Classes.Media;

// The User Control item template is documented at https://go.microsoft.com/fwlink/?LinkId=234236

namespace Indirect.Controls
{
    internal sealed partial class AnimatedImagePicker : UserControl
    {
        public event EventHandler<GiphyMedia> ImageSelected; 

        public DirectThreadWrapper Thread { get; set; }

        public ObservableCollection<GiphyMedia> ImageList { get; } = new ObservableCollection<GiphyMedia>();
        
        private static MainViewModel ViewModel => ((App)Application.Current).ViewModel;

        private GiphyMedia[] _stickers;
        private GiphyMedia[] _gifs;
        private string _selectedType;
        

        public AnimatedImagePicker()
        {
            this.InitializeComponent();
        }

        private async void TypeSelectBox_OnSelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            var typeName = e.AddedItems[0].ToString().ToLower();
            if (typeName == _selectedType) return;
            _selectedType = typeName;
            switch (typeName)
            {
                case "sticker" when _stickers != null:
                    PrepareImagesForDisplay(_stickers);
                    return;
                case "gif" when _gifs != null:
                    PrepareImagesForDisplay(_gifs);
                    return;
                default:
                    await SearchAnimatedImage().ConfigureAwait(false);
                    break;
            }
        }

        private async void SearchBox_OnTextChanged(AutoSuggestBox sender, AutoSuggestBoxTextChangedEventArgs args)
        {
            if (args.Reason != AutoSuggestionBoxTextChangeReason.UserInput) return;
            if (await Debouncer.Delay("AnimatedImageSearch", 500).ConfigureAwait(true))
            {
                await SearchAnimatedImage().ConfigureAwait(false);
            }
        }

        private async Task SearchAnimatedImage()
        {
            if (string.IsNullOrEmpty(_selectedType)) return;
            var query = SearchBox.Text;
            switch (_selectedType)
            {
                case "sticker":
                    {
                        var result = await Instagram.Instance.SearchAnimatedImageAsync(query, AnimatedImageType.Sticker);
                        if (result.Value == null) return;
                        _stickers = result.Value;
                        PrepareImagesForDisplay(_stickers);
                    }
                    break;
                case "gif":
                    {
                        var result = await Instagram.Instance.SearchAnimatedImageAsync(query, AnimatedImageType.Gif);
                        if (result.Value == null) return;
                        _gifs = result.Value;
                        PrepareImagesForDisplay(_gifs);
                    }
                    break;
                default:
                    throw new NotImplementedException($"{_selectedType} is not a supported animated image type");
            }
        }

        private void PrepareImagesForDisplay(GiphyMedia[] source)
        {
            if (source == null) return;
            ImageList.Clear();
            foreach (var media in source)
            {
                ImageList.Add(media);
            }
            if (ImageList.Count > 0)
            {
                PickerView.UpdateLayout();
                PickerView.ScrollIntoView(ImageList[0]);
            }
        }

        private async void SearchBox_OnQuerySubmitted(AutoSuggestBox sender, AutoSuggestBoxQuerySubmittedEventArgs args)
        {
            await SearchAnimatedImage().ConfigureAwait(false);
        }

        private void TypeSelectBox_OnLoaded(object sender, RoutedEventArgs e)
        {
            TypeSelectBox.SelectedIndex = 0;
        }

        private async void PickerOnSelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (e.AddedItems.Count == 0 || e.AddedItems[0] == null || Thread == null) return;
            var image = (GiphyMedia)e.AddedItems[0];
            await ViewModel.ChatService.SendAnimatedImage(Thread, image.Id, image.IsSticker);
            var gridView = (GridView) sender;
            gridView.SelectedItem = null;
            ImageSelected?.Invoke(this, image);
        }

        private void PickerView_OnLoaded(object sender, RoutedEventArgs e)
        {
            var winHeight = Window.Current.Bounds.Height;
            ((FrameworkElement) sender).Height = winHeight * 3 / 5;
        }
    }
}
