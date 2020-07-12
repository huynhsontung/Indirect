using System.ComponentModel;
using Windows.UI.Xaml.Media;
using Indirect.Controls;

namespace Indirect.Utilities
{
    /// <summary>
    /// Progress item for <see cref="ReelProgressIndicator"/>
    /// </summary>
    public class ProgressItem : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;

        public double Value { get; private set; }

        public Brush ForegroundBrush { get; private set; }

        public double Width { get; private set; }

        public void Update(double value, Brush foreground, double width)
        {
            Value = value;
            ForegroundBrush = foreground;
            Width = width;
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(string.Empty));
        }
    }
}
