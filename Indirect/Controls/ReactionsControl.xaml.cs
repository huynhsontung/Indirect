using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
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
using Indirect.Entities.Wrappers;

// The User Control item template is documented at https://go.microsoft.com/fwlink/?LinkId=234236

namespace Indirect.Controls
{
    public sealed partial class ReactionsControl : UserControl, INotifyPropertyChanged
    {
        public static DependencyProperty ReactionsProperty = DependencyProperty.Register(
            nameof(Reactions), 
            typeof(ObservableCollection<ReactionWithUser>), 
            typeof(ReactionsControl), 
            new PropertyMetadata(null, OnReactionsPropertyChanged));

        public event PropertyChangedEventHandler PropertyChanged;

        public ObservableCollection<ReactionWithUser> Reactions
        {
            get => (ObservableCollection<ReactionWithUser>) GetValue(ReactionsProperty);
            set => SetValue(ReactionsProperty, value);
        }
        
        private string FirstEmoji { get; set; }
        
        private string SecondEmoji { get; set; }
        
        private string ThirdEmoji { get; set; }
        
        private Visibility CounterVisibility { get; set; }

        public ReactionsControl()
        {
            this.InitializeComponent();
        }

        private static void OnReactionsPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var view = (ReactionsControl) d;
            var oldValue = (ObservableCollection<ReactionWithUser>) e.OldValue;
            var newValue = (ObservableCollection<ReactionWithUser>) e.NewValue;

            if (oldValue != null)
            {
                oldValue.CollectionChanged -= view.OnReactionsCollectionChanged;
            }

            if (newValue != null)
            {
                newValue.CollectionChanged += view.OnReactionsCollectionChanged;
            }
            
            view.UpdateEmojiPreviews();
            view.Bindings.Update();
        }

        private void UpdateEmojiPreviews()
        {
            if (Reactions == null || Reactions.Count == 0)
            {
                Visibility = Visibility.Collapsed;
                return;
            }

            Visibility = Visibility.Visible;
            FirstEmoji = Reactions[0].Reaction.Emoji;
            SecondEmoji = Reactions.FirstOrDefault(x => x.Reaction.Emoji != FirstEmoji)?.Reaction.Emoji;
            ThirdEmoji = Reactions
                .FirstOrDefault(x => x.Reaction.Emoji != FirstEmoji && x.Reaction.Emoji != SecondEmoji)?.Reaction.Emoji;
            CounterVisibility = Reactions.Count > 1 ? Visibility.Visible : Visibility.Collapsed;
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(string.Empty));
        }

        private void OnReactionsCollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            UpdateEmojiPreviews();
        }

        private void ReactionsControl_OnTapped(object sender, TappedRoutedEventArgs e)
        {
            FlyoutBase.ShowAttachedFlyout(this);
        }
    }
}
