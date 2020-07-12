using System.ComponentModel;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Media;
using Indirect.Utilities;

// The User Control item template is documented at https://go.microsoft.com/fwlink/?LinkId=234236

namespace Indirect.Controls
{
    public sealed partial class ReelProgressIndicator : UserControl, INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;

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

        public static readonly DependencyProperty BaseBrushProperty = DependencyProperty.Register(
            nameof(BaseBrush),
            typeof(Brush),
            typeof(ReelProgressIndicator),
            new PropertyMetadata(null));

        public static readonly DependencyProperty HighlightBrushProperty = DependencyProperty.Register(
            nameof(HighlightBrush),
            typeof(Brush),
            typeof(ReelProgressIndicator),
            new PropertyMetadata(null));

        public Brush BaseBrush
        {
            get => (Brush) GetValue(BaseBrushProperty);
            set => SetValue(BaseBrushProperty, value);
        }

        public Brush HighlightBrush
        {
            get => (Brush) GetValue(HighlightBrushProperty);
            set => SetValue(HighlightBrushProperty, value);
        }

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

        private double _singleWidth = 4;
        private double SingleWidth
        {
            get => _singleWidth;
            set
            {
                if (value == _singleWidth) return;
                _singleWidth = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(SingleWidth)));
            }
        }

        // Maximum number of stories at a time is 100. Source: https://mashable.com/2017/10/20/how-i-broke-instagram-stories/
        private readonly ProgressItem[] _indicatorCollection = new ProgressItem[100];

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
            for (var i = 0; i < Count; i++)
            {
                if (i < Selected)
                {
                    _indicatorCollection[i].Update(100, BaseBrush, SingleWidth);
                }
                else if (i == Selected)
                {
                    _indicatorCollection[i].Update(100, HighlightBrush, SingleWidth);
                }
                else
                {
                    _indicatorCollection[i].Update(0, BaseBrush, SingleWidth);
                }
            }
        }

        public ReelProgressIndicator()
        {
            this.InitializeComponent();
            for (var i = 0; i < _indicatorCollection.Length; i++)
            {
                _indicatorCollection[i] = new ProgressItem();
            }
        }

        private void CalculateIndicatorWidth()
        {
            var availableWidth = this.ActualWidth - (Count - 1) * 2d;  // 2 is spacing between indicators
            SingleWidth = availableWidth <= 0 ? 4 : availableWidth / Count;
        }

        private void UpdateIndicatorWidth()
        {
            for (var i = 0; i < Count; i++)
            {
                var item = _indicatorCollection[i];
                item.Update(item.Value, item.ForegroundBrush, SingleWidth);
            }
        }

        private void ReelProgressIndicator_OnSizeChanged(object sender, SizeChangedEventArgs e)
        {
            CalculateIndicatorWidth();
            UpdateIndicatorWidth();
        }
    }
}
