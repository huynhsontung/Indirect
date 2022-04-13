using System;
using System.Collections.Immutable;
using System.Linq;
using System.Threading.Tasks;
using Windows.Foundation.Metadata;
using Windows.System;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Microsoft.Toolkit.Uwp.UI;
using NeoSmart.Unicode;

namespace Indirect.Controls
{
    public partial class EmojiPicker : Control
    {
        private static Flyout openFlyout;

        private readonly CollectionViewSource groupedEmoji;
        private readonly DispatcherQueue queue;
        private readonly DispatcherQueueTimer timer;
        private ListViewBase emojiPresenter;
        private Button[] categoryButtons;
        private Button closeButton;
        private TextBox searchBox;
        private string selectedEmoji;

        public EmojiPicker()
        {
            this.DefaultStyleKey = typeof(EmojiPicker);
            this.categoryButtons = new Button[6];
            this.queue = DispatcherQueue.GetForCurrentThread();
            this.timer = queue.CreateTimer();
            this.groupedEmoji = new CollectionViewSource
            {
                IsSourceGrouped = true
            };

            Loaded += OnLoaded;
        }

        public static async Task<string> ShowAsync(FrameworkElement placementTarget, FlyoutShowOptions showOptions)
        {
            var picker = new EmojiPicker();

            openFlyout = new Flyout
            {
                Content = picker
            };

            if (ApiInformation.IsApiContractPresent("Windows.Foundation.UniversalApiContract", 8))
            {
                openFlyout.ShouldConstrainToRootBounds = false;
            }

            openFlyout.ShowAt(placementTarget, showOptions);
            
            var tcs = new TaskCompletionSource<string>();

            openFlyout.Closed += (_, __) => tcs.SetResult(picker.selectedEmoji);

            return await tcs.Task;
        }

        protected override void OnApplyTemplate()
        {
            base.OnApplyTemplate();

            this.emojiPresenter = (ListViewBase) this.GetTemplateChild("EmojiPresenter");

            this.categoryButtons[0] = (Button)this.GetTemplateChild("SmilesButton");
            this.categoryButtons[1] = (Button)this.GetTemplateChild("PeopleButton");
            this.categoryButtons[2] = (Button)this.GetTemplateChild("PizzaButton");
            this.categoryButtons[3] = (Button)this.GetTemplateChild("CarButton");
            this.categoryButtons[4] = (Button)this.GetTemplateChild("BalloonButton");
            this.categoryButtons[5] = (Button)this.GetTemplateChild("HeartButton");
            this.closeButton = (Button) this.GetTemplateChild("CloseButton");
            this.searchBox = (TextBox) this.GetTemplateChild("SearchBox");

            this.closeButton.Click += this.CloseButtonClick;
            this.emojiPresenter.ItemClick += this.EmojiSelected;
            this.searchBox.TextChanged += SearchBoxTextChanged;

            this.ResetDisplayEmoji();

            categoryButtons[0].Tag = EmojiGroups[0].First();
            categoryButtons[1].Tag = EmojiGroups[1].First();
            categoryButtons[2].Tag = EmojiGroups[3].First();
            categoryButtons[3].Tag = EmojiGroups[4].First();
            categoryButtons[4].Tag = EmojiGroups[5].First();
            categoryButtons[5].Tag = EmojiGroups[7].First();

            foreach (var button in this.categoryButtons)
            {
                button.Click += this.ChangeCategoryClick;
            }
        }

        private void OnLoaded(object sender, RoutedEventArgs e)
        {
            this.searchBox.Focus(FocusState.Programmatic);
        }

        private async void RefreshSearch()
        {
            var phrase = searchBox.Text.Trim();
            if (phrase.Length == 0)
            {
                this.ResetDisplayEmoji();
            }
            else
            {
                await this.UpdateSearchResults(phrase);
            }
        }

        private async Task UpdateSearchResults(string phrase)
        {
            void Action()
            {
                var result = Emoji.All
                    .Where(e => e.SearchTerms.Any(s => s.StartsWith(phrase)))
                    .Select(x => new EmojiViewModel(x)).ToImmutableList();
                queue.TryEnqueue(() => this.emojiPresenter.ItemsSource = result);
            }

            await Task.Run(Action);
        }

        private void SearchBoxTextChanged(object sender, TextChangedEventArgs e)
        {
            this.timer.Debounce(RefreshSearch, TimeSpan.FromMilliseconds(100));
        }

        private void EmojiSelected(object sender, ItemClickEventArgs e)
        {
            this.selectedEmoji = ((EmojiViewModel)e.ClickedItem).Glyph;
            openFlyout.Hide();
        }

        private void ChangeCategoryClick(object sender, RoutedEventArgs e)
        {
            if (!string.IsNullOrEmpty(searchBox.Text)) return;
            var button = (Button)sender;
            var firstItemInGroup = button.Tag;
            this.emojiPresenter.ScrollIntoView(firstItemInGroup, ScrollIntoViewAlignment.Leading);
        }

        private void CloseButtonClick(object sender, RoutedEventArgs e)
        {
            openFlyout.Hide();
        }

        private void ResetDisplayEmoji()
        {
            if (groupedEmoji.Source == null)
                groupedEmoji.Source = EmojiGroups;
            this.emojiPresenter.ItemsSource = this.groupedEmoji.View;
        }
    }
}
