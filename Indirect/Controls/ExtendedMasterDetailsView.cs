using System;
using System.Numerics;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Media;
using Microsoft.Toolkit.Uwp.UI.Controls;

namespace Indirect.Controls
{
    [TemplatePart(Name = PartMainShadow, Type = typeof(ThemeShadow))]
    [TemplatePart(Name = PartMasterPanel, Type = typeof(FrameworkElement))]
    [TemplatePart(Name = PartDetailsPanel, Type = typeof(FrameworkElement))]
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

        private const string PartMainShadow = "MainShadow";
        private const string PartMasterPanel = "MasterPanel";
        private const string PartDetailsPanel = "DetailsPanel";

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

        protected override void OnApplyTemplate()
        {
            base.OnApplyTemplate();

            try
            {
                var shadow = GetTemplateChild(PartMainShadow) as ThemeShadow;
                var mainPanel = GetTemplateChild(PartMasterPanel) as FrameworkElement;
                var details = GetTemplateChild(PartDetailsPanel) as FrameworkElement;
                if (shadow == null || mainPanel == null || details == null) return;
                shadow.Receivers.Add(mainPanel);
                details.Translation += new Vector3(0, 0, 16);
                ViewStateChanged += (sender, state) =>
                {
                    if (state == MasterDetailsViewState.Master || state == MasterDetailsViewState.Details)
                    {
                        shadow.Receivers.Clear();
                    }
                    else if (shadow.Receivers.Count == 0)
                    {
                        shadow.Receivers.Add(mainPanel);
                    }
                };
            }
            catch (Exception)
            {
                
                // Failed to set config Shadow. Maybe old system?
            }
        }
    }
}
