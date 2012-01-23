using System;
using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace Mubox.View.Controls
{
    /// <summary>
    /// Interaction logic for ClientHotBar.xaml
    /// </summary>
    public partial class VKBoard : UserControl
    {
        public VKBoard()
        {
            InitializeComponent();
            this.AddHandler(System.Windows.Controls.Primitives.ToggleButton.CheckedEvent, (RoutedEventHandler)ToggleButton_Checked);
            this.AddHandler(System.Windows.Controls.Primitives.ToggleButton.UncheckedEvent, (RoutedEventHandler)ToggleButton_Unchecked);
        }

        private void ToggleButton_Checked(object sender, RoutedEventArgs e)
        {
            try
            {
                Win32.VK vk = (Win32.VK)Enum.Parse(typeof(Win32.VK), (e.OriginalSource as System.Windows.Controls.Primitives.ToggleButton).Tag as string, true);
                Mubox.Configuration.KeySetting keySetting;
                if (!KeySettings.TryGetKeySetting(vk, out keySetting))
                {
                    keySetting = KeySettings.CreateNew(vk);
                }
                this.EnableCallback(keySetting);
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex.Message);
                Debug.WriteLine(ex.StackTrace);
            }
        }

        private void ToggleButton_Unchecked(object sender, RoutedEventArgs e)
        {
            try
            {
                Win32.VK vk = (Win32.VK)Enum.Parse(typeof(Win32.VK), (e.OriginalSource as System.Windows.Controls.Primitives.ToggleButton).Tag as string, true);
                Mubox.Configuration.KeySetting keySetting;
                if (KeySettings.TryGetKeySetting(vk, out keySetting))
                {
                    this.DisableCallback(keySetting);
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex.Message);
                Debug.WriteLine(ex.StackTrace);
            }
        }

        public Mubox.Configuration.KeySettingCollection KeySettings { get; private set; }

        public Predicate<Mubox.Configuration.KeySetting> FilterCallback { get; private set; }

        public Action<Mubox.Configuration.KeySetting> EnableCallback { get; private set; }

        public Action<Mubox.Configuration.KeySetting> DisableCallback { get; private set; }

        public void InitializeButtonState(Mubox.Configuration.KeySettingCollection keySettings, Predicate<Mubox.Configuration.KeySetting> filterCallback, Action<Mubox.Configuration.KeySetting> enableCallback, Action<Mubox.Configuration.KeySetting> disableCallback)
        {
            this.KeySettings = keySettings;
            this.FilterCallback = filterCallback;
            this.EnableCallback = enableCallback;
            this.DisableCallback = disableCallback;
            ProcessFrameworkElementTree(this, (Action<FrameworkElement>)delegate(FrameworkElement frameworkElement)
            {
                try
                {
                    System.Windows.Controls.Primitives.ToggleButton toggleButton = frameworkElement as System.Windows.Controls.Primitives.ToggleButton;
                    if (toggleButton != null)
                    {
                        Mubox.Configuration.KeySetting keySetting;
                        toggleButton.IsChecked =
                            KeySettings.TryGetKeySetting((Win32.VK)Enum.Parse(typeof(Win32.VK), toggleButton.Tag as string, true), out keySetting)
                            && FilterCallback(keySetting);
                    }
                }
                catch (Exception ex)
                {
                    Debug.WriteLine(ex.Message);
                    Debug.WriteLine(ex.StackTrace);
                }
            });
        }

        public void ProcessVisual(Visual visual, Action<Visual> callback)
        {
            for (int i = 0; i < VisualTreeHelper.GetChildrenCount(visual); i++)
            {
                Visual childVisual = (Visual)VisualTreeHelper.GetChild(visual, i);
                if (childVisual != null)
                {
                    ProcessVisual(childVisual, callback);
                    callback(childVisual);
                }
            }
        }

        public void ProcessFrameworkElementTree(FrameworkElement frameworkElement, Action<FrameworkElement> callback)
        {
            foreach (object o in LogicalTreeHelper.GetChildren(frameworkElement))
            {
                FrameworkElement child = o as FrameworkElement;
                if (child != null)
                {
                    ProcessFrameworkElementTree(child, callback);
                    callback(child);
                }
            }
        }
    }
}