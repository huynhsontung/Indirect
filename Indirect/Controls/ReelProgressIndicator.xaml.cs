using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
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

        private readonly ObservableCollection<(double value, Brush foreground)> _indicatorCollection = new ObservableCollection<(double value, Brush foreground)>();

        private static void StaticOnSelectOrCountChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var owner = (ReelProgressIndicator) d;
            if ((int) e.NewValue < 0)
            {
                owner.SetValue(e.Property, 0);
                return;
            }
            if (e.OldValue == e.NewValue) return;
            owner.OnSelectOrCountChanged();
        }

        private void OnSelectOrCountChanged()
        {
            if (Count != _indicatorCollection.Count)
            {
                _indicatorCollection.Clear();
                for (var i = 0; i < Count; i++)
                {
                    if (i < Selected)
                    {
                        _indicatorCollection.Add((100, BaseBrush));
                    }
                    else if (i == Selected)
                    {
                        _indicatorCollection.Add((100, HighlightBrush));
                    }
                    else
                    {
                        _indicatorCollection.Add((0, BaseBrush));
                    }
                }
            }
            else
            {
                for (var i = 0; i < Selected && i < _indicatorCollection.Count; i++)
                {
                    if (i < Selected)
                    {
                        _indicatorCollection[i] = (100, BaseBrush);
                    }
                    else if (i == Selected)
                    {
                        _indicatorCollection[i] = (100, HighlightBrush);
                    }
                    else
                    {
                        _indicatorCollection[i] = (0, BaseBrush);
                    }
                }
            }
        }

        public ReelProgressIndicator()
        {
            this.InitializeComponent();
        }
    }
}
