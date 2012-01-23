using System;
using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;

namespace Mubox.View.Server
{
    /// <summary>
    /// Interaction logic for MulticastConfigDialog.xaml
    /// </summary>
    public partial class MulticastConfigDialog : Window
    {
        public MulticastConfigDialog()
        {
            InitializeComponent();
            try
            {
                checkEnableMulticast.IsChecked = Mubox.Configuration.MuboxConfigSection.Default.EnableMulticast;
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex.Message);
                Debug.WriteLine(ex.StackTrace);
            }
        }

        static MulticastConfigDialog()
        {
        }

        private static MulticastConfigDialog staticDialogInstance = null;
        private static readonly object staticDialogInstanceLock = new object();
        public static void ShowStaticDialog()
        {
            lock (staticDialogInstanceLock)
            {
                if (staticDialogInstance == null)
                {
                    staticDialogInstance = new MulticastConfigDialog();
                    staticDialogInstance.Loaded += new RoutedEventHandler(staticDialogInstance_Loaded);
                    staticDialogInstance.Closed += new EventHandler(staticDialogInstance_Closed);
                }
                staticDialogInstance.Show();
            }
        }

        private static void staticDialogInstance_Closed(object sender, EventArgs e)
        {
            lock (staticDialogInstanceLock)
            {
                staticDialogInstance = null;
            }
        }

        private static void staticDialogInstance_Loaded(object sender, RoutedEventArgs e)
        {
            staticDialogInstance.vkBoardActiveClientOnly.InitializeButtonState(
                Mubox.Configuration.MuboxConfigSection.Default.Keys,
                (keySetting) => keySetting.ActiveClientOnly,
                (keySetting) =>
                {
                    keySetting.RoundRobinKey = false;
                    keySetting.ActiveClientOnly = true;
                    keySetting.SendToDesktop = false;
                },
                (keySetting) => keySetting.ActiveClientOnly = false);
            staticDialogInstance.vkBoardRoundRobin.InitializeButtonState(
                Mubox.Configuration.MuboxConfigSection.Default.Keys,
                (keySetting) => keySetting.RoundRobinKey,
                (keySetting) =>
                {
                    keySetting.RoundRobinKey = true;
                    keySetting.ActiveClientOnly = false;
                    keySetting.SendToDesktop = false;
                },
                (keySetting) => keySetting.RoundRobinKey = false);
            staticDialogInstance.vkBoardSendToDesktop.InitializeButtonState(
                Mubox.Configuration.MuboxConfigSection.Default.Keys,
                (keySetting) => keySetting.SendToDesktop,
                (keySetting) =>
                {
                    keySetting.RoundRobinKey = false;
                    keySetting.ActiveClientOnly = false;
                    keySetting.SendToDesktop = true;
                },
                (keySetting) => keySetting.SendToDesktop = false);
        }

        private void buttonSaveConfig_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }

        protected override void OnClosing(System.ComponentModel.CancelEventArgs e)
        {
            Mubox.Configuration.MuboxConfigSection.Default.EnableMulticast = checkEnableMulticast.IsChecked.GetValueOrDefault(false); // TODO databind instead
            Mubox.Configuration.MuboxConfigSection.Default.Save();
            base.OnClosing(e);
        }

        private void tabControl1_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
        }

        private void tabControl1_Loaded(object sender, RoutedEventArgs e)
        {
        }
    }
}