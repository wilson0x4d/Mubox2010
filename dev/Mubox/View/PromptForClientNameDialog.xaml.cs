using System;
using System.Windows;
using System.Windows.Controls;

namespace Mubox.View
{
    /// <summary>
    /// Interaction logic for ClientNameDialog.xaml
    /// </summary>
    public partial class PromptForClientNameDialog : Window
    {
        public PromptForClientNameDialog()
        {
            InitializeComponent();
        }

        #region ClientName

        /// <summary>
        /// ClientName Dependency Property
        /// </summary>
        public static readonly DependencyProperty ClientNameProperty =
            DependencyProperty.Register("ClientName", typeof(string), typeof(PromptForClientNameDialog),
                new FrameworkPropertyMetadata((string)Mubox.Model.Client.ClientBase.Sanitize(Environment.MachineName)));

        /// <summary>
        /// Gets or sets the ClientName property.  This dependency property
        /// indicates the chose client name.
        /// </summary>
        public string ClientName
        {
            get { return Mubox.Model.Client.ClientBase.Sanitize((string)GetValue(ClientNameProperty)); }
            set { SetValue(ClientNameProperty, Mubox.Model.Client.ClientBase.Sanitize(value).ToUpper()); }
        }

        #endregion

        private void buttonOK_Click(object sender, RoutedEventArgs e)
        {
            this.DialogResult = true;
            this.Close();
        }

        private void buttonCancel_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }

        private void textClientName_TextChanged(object sender, TextChangedEventArgs e)
        {
            this.ClientName = textClientName.Text;
            buttonOK.IsEnabled = !string.IsNullOrEmpty(this.ClientName);
        }

        public static string PromptForClientName()
        {
            Mubox.View.PromptForClientNameDialog dlg = new Mubox.View.PromptForClientNameDialog();
            bool cancel = !dlg.ShowDialog().GetValueOrDefault(false);
            if (string.IsNullOrEmpty(dlg.ClientName) || cancel)
            {
                throw new ArgumentException("Cancelled", "clientName");
            }
            return dlg.ClientName;
        }
    }
}