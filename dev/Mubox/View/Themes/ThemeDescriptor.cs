using System.Windows;

namespace Mubox.View.Themes
{
    public class ThemeDescriptor : DependencyObject
    {
        #region Name

        /// <summary>
        /// Name Dependency Property
        /// </summary>
        public static readonly DependencyProperty NameProperty =
            DependencyProperty.Register("Name", typeof(string), typeof(ThemeDescriptor),
                new FrameworkPropertyMetadata((string)""));

        /// <summary>
        /// Gets or sets the Name property.  This dependency property
        /// indicates the Name of the Theme.
        /// </summary>
        public string Name
        {
            get { return (string)GetValue(NameProperty); }
            set { SetValue(NameProperty, value); }
        }

        #endregion

        #region Resources

        /// <summary>
        /// Resources Dependency Property
        /// </summary>
        public static readonly DependencyProperty ResourcesProperty =
            DependencyProperty.Register("Resources", typeof(ResourceDictionary), typeof(ThemeDescriptor),
                new FrameworkPropertyMetadata((ResourceDictionary)null));

        /// <summary>
        /// Gets or sets the Resources property.  This dependency property
        /// indicates the Resources of the Theme.
        /// </summary>
        public ResourceDictionary Resources
        {
            get { return (ResourceDictionary)GetValue(ResourcesProperty); }
            set { SetValue(ResourcesProperty, value); }
        }

        #endregion
    }
}