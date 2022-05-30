using InstagramAPI.Classes.User;
using Microsoft.Toolkit.Uwp.UI;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.System;
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
    public sealed partial class NewMessagePicker : UserControl
    {
        private MainViewModel ViewModel => ((App)Application.Current).ViewModel;

        private ObservableCollection<BaseUser> NewMessageCandidates { get; } = new ObservableCollection<BaseUser>();
        private Flyout _flyout;

        public NewMessagePicker()
        {
            this.InitializeComponent();
        }

        public static Flyout GetFlyout()
        {
            Flyout flyout = new()
            {
                Placement = FlyoutPlacementMode.RightEdgeAlignedTop
            };

            NewMessagePicker control = new();
            control._flyout = flyout;
            flyout.Content = control;
            return flyout;
        }

        private void NewMessageSuggestBox_OnTextChanged(AutoSuggestBox sender, AutoSuggestBoxTextChangedEventArgs args)
        {
            if (args.Reason != AutoSuggestionBoxTextChangeReason.UserInput) return;
            if (string.IsNullOrEmpty(sender.Text) || sender.Text.Length > 50)
            {
                return;
            }

            ViewModel.SearchWithoutThreads(sender.Text,
                updatedList => NewMessageSuggestBox.ItemsSource = updatedList);
        }

        private void NewMessageSuggestBox_OnSuggestionChosen(AutoSuggestBox sender, AutoSuggestBoxSuggestionChosenEventArgs args)
        {
            var selectedItem = (BaseUser)args.SelectedItem;
            sender.Text = selectedItem.Username;
        }

        private void NewMessageSuggestBox_OnQuerySubmitted(AutoSuggestBox sender, AutoSuggestBoxQuerySubmittedEventArgs args)
        {
            if (args.ChosenSuggestion != null)
            {
                var selectedRecipient = (BaseUser)args.ChosenSuggestion;
                if (NewMessageCandidates.All(x => selectedRecipient.Username != x.Username))
                    NewMessageCandidates.Add(selectedRecipient);
            }
            else if (!string.IsNullOrEmpty(sender.Text))
            {
                ViewModel.SearchWithoutThreads(sender.Text, updatedList =>
                {
                    if (updatedList.Count == 0) return;
                    NewMessageCandidates.Add(updatedList[0]);
                });
            }
            sender.Text = string.Empty;
            sender.ItemsSource = null;
        }

        private void NewMessageClearAll_OnClick(object sender, RoutedEventArgs e)
        {
            NewMessageCandidates.Clear();
        }

        private async void ChatButton_OnClick(object sender, RoutedEventArgs e)
        {
            _flyout.Hide();
            if (NewMessageCandidates.Count == 0 || NewMessageCandidates.Count > 32) return;
            var userIds = NewMessageCandidates.Select(x => x.Pk);
            await ViewModel.CreateAndOpenThread(userIds);
            NewMessageCandidates.Clear();
        }

        private void ClearSingleCandidateButton_OnClick(object sender, RoutedEventArgs e)
        {
            var target = (BaseUser)(sender as FrameworkElement)?.DataContext;
            if (target == null) return;
            NewMessageCandidates.Remove(target);
        }

        private void Candidate_PointerEntered(object sender, PointerRoutedEventArgs e)
        {
            if (((FrameworkElement)sender).FindDescendant("ClearSingleCandidateButton") is Button button)
            {
                button.Visibility = Visibility.Visible;
            }
        }

        private void Candidate_OnPointerExited(object sender, PointerRoutedEventArgs e)
        {
            if (((FrameworkElement)sender).FindDescendant("ClearSingleCandidateButton") is Button button)
            {
                button.Visibility = Visibility.Collapsed;
            }
        }

        private void ClearSingleCandidateSwipe_OnInvoked(SwipeItem sender, SwipeItemInvokedEventArgs args)
        {
            var target = (BaseUser)args.SwipeControl.DataContext;
            if (target == null) return;
            NewMessageCandidates.Remove(target);
        }

        private void NewMessageSuggestBox_OnProcessKeyboardAccelerators(UIElement sender, ProcessKeyboardAcceleratorEventArgs args)
        {
            if (args.Key == VirtualKey.Escape && args.Modifiers == VirtualKeyModifiers.None)
            {
                args.Handled = true;
                _flyout.Hide();
            }

        }
    }
}
