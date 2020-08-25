using System.ComponentModel;
using Indirect.Controls;

namespace Indirect.Entities
{
    /// <summary>
    /// Progress item for <see cref="ReelProgressIndicator"/>
    /// </summary>
    public class ProgressItem : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;

        public double Value { get; private set; }

        public double Width { get; private set; }

        public void Update(double value, double width)
        {
            Value = value;
            Width = width;
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(string.Empty));
        }
    }
}
