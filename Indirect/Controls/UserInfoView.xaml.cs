using System;
using System.Collections.Generic;
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
using InstagramAPI.Classes.User;

// The User Control item template is documented at https://go.microsoft.com/fwlink/?LinkId=234236

namespace Indirect.Controls
{
    public sealed partial class UserInfoView : UserControl
    {
        public static readonly DependencyProperty UserProperty = DependencyProperty.Register(
            nameof(User),
            typeof(UserInfo),
            typeof(UserInfoView),
            new PropertyMetadata(null, OnUserChanged));

        public UserInfo User
        {
            get => (UserInfo) GetValue(UserProperty);
            set => SetValue(UserProperty, value);
        }

        private static void OnUserChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var view = (UserInfoView)d;
            var item = (UserInfo) e.NewValue;
            if (item == null) return;
            view.GoToProfileHyperlinkButton.NavigateUri = item.ProfileUrl;
            if (!string.IsNullOrEmpty(item.ExternalUrl))
            {
                view.ExternalUrl.Content = item.ExternalUrl;
                view.ExternalUrl.NavigateUri = new Uri(item.ExternalUrl);
            }
        }

        private Visibility VisibleWhenNotNullOrEmpty(string s)
        {
            return string.IsNullOrEmpty(s) ? Visibility.Collapsed : Visibility.Visible;
        }

        public UserInfoView()
        {
            this.InitializeComponent();
        }
    }
}
