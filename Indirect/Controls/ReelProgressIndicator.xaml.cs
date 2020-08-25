using System.Collections.ObjectModel;
using System.ComponentModel;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Media;
using Indirect.Entities;
using Indirect.Utilities;

// The User Control item template is documented at https://go.microsoft.com/fwlink/?LinkId=234236

namespace Indirect.Controls
{
    public sealed partial class ReelProgressIndicator : UserControl
    {
        public static readonly DependencyProperty CountProperty = DependencyProperty.Register(
            nameof(Count),
            typeof(int),
            typeof(ReelProgressIndicator),
            new PropertyMetadata(0, StaticOnSelectOrCountChanged));

        public static readonly DependencyProperty SelectedProperty = DependencyProperty.Register(
            nameof(Selected),
            typeof(int),
            typeof(ReelProgressIndicator),
            new PropertyMetadata(0, StaticOnSelectOrCountChanged));

        public static readonly DependencyProperty SingleWidthProperty = DependencyProperty.Register(
            nameof(SingleWidth),
            typeof(double),
            typeof(ReelProgressIndicator),
            new PropertyMetadata(60d));

        public int Selected
        {
            get => (int) GetValue(SelectedProperty);
            set => SetValue(SelectedProperty, value);
        }

        public int Count
        {
            get => (int)GetValue(CountProperty);
            set => SetValue(CountProperty, value);
        }

        public double SingleWidth
        {
            get => (double) GetValue(SingleWidthProperty);
            set => SetValue(SingleWidthProperty, value);
        }

        public ObservableCollection<ProgressItem> IndicatorCollection { get; } = new ObservableCollection<ProgressItem>();

        private static void StaticOnSelectOrCountChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var owner = (ReelProgressIndicator) d;
            if ((int) e.NewValue < 0)
            {
                owner.SetValue(e.Property, 0);
                return;
            }
            if (e.OldValue == e.NewValue) return;
            owner.CalculateIndicatorWidth();
            owner.OnSelectOrCountChanged();
        }

        private void OnSelectOrCountChanged()
        {
            if (IndicatorCollection.Count > Count)
            {
                var diff = IndicatorCollection.Count - Count;
                for (int i = 0; i < diff; i++)
                {
                    IndicatorCollection.RemoveAt(IndicatorCollection.Count - 1);
                }
            }

            if (IndicatorCollection.Count < Count)
            {
                var diff = Count - IndicatorCollection.Count;
                for (int i = 0; i < diff; i++)
                {
                    IndicatorCollection.Add(new ProgressItem());
                }
            }

            for (var i = 0; i < IndicatorCollection.Count; i++)
            {
                if (i < Selected)
                {
                    IndicatorCollection[i].Update(100, SingleWidth);
                }
                else if (i == Selected)
                {
                    IndicatorCollection[i].Update(100, SingleWidth);
                }
                else
                {
                    IndicatorCollection[i].Update(0, SingleWidth);
                }
            }
        }

        public ReelProgressIndicator()
        {
            this.InitializeComponent();
        }

        private void CalculateIndicatorWidth()
        {
            var availableWidth = this.ActualWidth - (Count - 1) * 2d;  // 2 is spacing between indicators
            SingleWidth = availableWidth <= 0 ? 4 : availableWidth / Count;
        }

        private void ReelProgressIndicator_OnSizeChanged(object sender, SizeChangedEventArgs e)
        {
            CalculateIndicatorWidth();
        }
    }
}
