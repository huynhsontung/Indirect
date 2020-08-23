using System;
using System.Collections.Generic;
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

// The Blank Page item template is documented at https://go.microsoft.com/fwlink/?LinkId=234238

namespace Indirect.Pages
{
    public sealed partial class NotAvailablePage : Page
    {
        public static readonly DependencyProperty ReasonProperty = DependencyProperty.Register(
            nameof(Reason),
            typeof(string),
            typeof(ReelPage),
            new PropertyMetadata(null));

        public string Reason
        {
            get => (string)GetValue(ReasonProperty);
            set => SetValue(ReasonProperty, value);
        }

        public NotAvailablePage()
        {
            this.InitializeComponent();
        }

        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);
            Reason = e?.Parameter as string;
            Bindings.Update();
        }
    }
}
