using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.UI.Xaml;

namespace Indirect.Controls
{
    public class ExtendedMasterDetailsView : Microsoft.Toolkit.Uwp.UI.Controls.MasterDetailsView
    {
        public static readonly DependencyProperty MasterListHeaderProperty = DependencyProperty.Register(
            nameof(MasterListHeader),
            typeof(object),
            typeof(ExtendedMasterDetailsView),
            new PropertyMetadata(null));

        public static readonly DependencyProperty MasterListHeaderTemplateProperty = DependencyProperty.Register(
            nameof(MasterListHeaderTemplate),
            typeof(DataTemplate),
            typeof(ExtendedMasterDetailsView),
            new PropertyMetadata(null));

        public object MasterListHeader
        {
            get => GetValue(MasterListHeaderProperty);
            set => SetValue(MasterListHeaderProperty, value);
        }

        public DataTemplate MasterListHeaderTemplate
        {
            get => (DataTemplate)GetValue(MasterListHeaderTemplateProperty);
            set => SetValue(MasterListHeaderTemplateProperty, value);
        }
    }
}
