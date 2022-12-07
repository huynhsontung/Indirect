using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.InteropServices;
using Windows.UI.Core;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Indirect.Utilities;
using InstagramAPI.Classes.User;
using InstagramAPI.Utils;
using CommunityToolkit.Mvvm.Messaging;
using Indirect.Entities.Messages;
using InstagramAPI.Classes.Responses;
using Windows.System;
using System.Collections.Generic;

// The User Control item template is documented at https://go.microsoft.com/fwlink/?LinkId=234236

namespace Indirect.Controls
{
    public sealed partial class ProfilePicture : UserControl
    {
        public static readonly DependencyProperty SourceProperty = DependencyProperty.Register(
            nameof(Source),
            typeof(IList<BaseUser>),
            typeof(ProfilePicture),
            new PropertyMetadata(null, OnSourceChanged));
        public static readonly DependencyProperty IsUserActiveProperty = DependencyProperty.Register(
            nameof(IsUserActive),
            typeof(bool),
            typeof(ProfilePicture),
            new PropertyMetadata(null));


        public IList<BaseUser> Source
        {
            get => (IList<BaseUser>)GetValue(SourceProperty);
            set => SetValue(SourceProperty, value);
        }

        public bool IsUserActive
        {
            get => (bool) GetValue(IsUserActiveProperty);
            set => SetValue(IsUserActiveProperty, value);
        }

        private readonly DispatcherQueue _dispatcherQueue;

        public ProfilePicture()
        {
            _dispatcherQueue = DispatcherQueue.GetForCurrentThread();
            this.InitializeComponent();

            WeakReferenceMessenger.Default.Register<UserPresenceUpdatedMessage>(this, (r, m) =>
            {
                if (Source == null) return;
                ProfilePicture view = (ProfilePicture)r;
                if (Source.All(user => user.Pk != m.UserId)) return;
                view._dispatcherQueue.TryEnqueue(() =>
                {
                    IsUserActive = m.Presence.IsActive;
                });
            });
        }

        private static void OnSourceChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var view = (ProfilePicture)d;
            var item = (IList<BaseUser>)e.NewValue;
            if (item.Count > 1)
            {
                view.Single.Visibility = Visibility.Collapsed;
                view.Group.Visibility = Visibility.Visible;
                view.Person1.Source = item[0].ProfilePictureUrl;
                view.Person2.Source = item[1].ProfilePictureUrl;
            }
            else
            {
                view.Single.Visibility = Visibility.Visible;
                view.Group.Visibility = Visibility.Collapsed;
                view.Single.Source = item[0]?.ProfilePictureUrl;
            }

            view.UpdatePresence();
        }

        public void UpdatePresence()
        {
            if (Source == null) return;
            UserPresenceValue presence = null;
            foreach (BaseUser user in Source)
            {
                presence = WeakReferenceMessenger.Default.Send(new UserPresenceRequestMessage(user.Pk));
                if (presence != null) break;
            }

            if (presence == null) return;
            IsUserActive = presence.IsActive;
        }

        //private async void OnUserPresenceChanged(object sender, PropertyChangedEventArgs e)
        //{
        //    if (e.PropertyName != nameof(MainViewModel.UserPresenceDictionary) &&
        //        !string.IsNullOrEmpty(e.PropertyName))
        //    {
        //        return;
        //    }

        //    try
        //    {
        //        await Dispatcher.QuickRunAsync(() =>
        //        {
        //            if (Source == null)
        //            {
        //                return;
        //            }

        //            IsUserActive = Source.Any(user =>
        //                ((App) Application.Current).ViewModel.UserPresenceDictionary
        //                .TryGetValue(user.Pk, out var value) &&
        //                value.IsActive);
        //        }, CoreDispatcherPriority.Low);
        //    }
        //    catch (InvalidComObjectException exception)
        //    {
        //        // This happens when ContactPanel is closed but this view still listens to event from viewmodel
        //        DebugLogger.LogException(exception, false);
        //    }
        //}

        private void ProfilePicture_OnSizeChanged(object sender, SizeChangedEventArgs e)
        {
            const double groupImageRatio = 0.8;
            Single.Width = e.NewSize.Width;
            Single.Height = e.NewSize.Height;
            Person1.Width = Person2.Width = e.NewSize.Width * groupImageRatio;
            Person1.Height = Person2.Height = e.NewSize.Height * groupImageRatio;
            Person1.Margin = new Thickness((1 - groupImageRatio) * e.NewSize.Width, 0, 0, (1 - groupImageRatio) * e.NewSize.Height);
            Person2.Margin = new Thickness(0, (1 - groupImageRatio) * e.NewSize.Height, (1 - groupImageRatio) * e.NewSize.Width, 0);
            ActiveCircle.Width = ActiveCircle.Height = e.NewSize.Height * 0.25;
        }
    }
}
