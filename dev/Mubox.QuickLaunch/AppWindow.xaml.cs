using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Windows;
using System.Windows.Navigation;
using System.Windows.Threading;

namespace Mubox.QuickLaunch
{
    /// <summary>
    /// Interaction logic for Window1.xaml
    /// </summary>
    public partial class AppWindow : Window
    {
        private DispatcherTimer TitleTimer { get; set; }

        private System.Windows.Forms.NotifyIcon notifyIcon = null;

        private DispatcherTimer timerDetectActivationChange = new DispatcherTimer();

        public AppWindow()
        {
            InitializeComponent();
            timerDetectActivationChange.Tick += new EventHandler(timerDetectActivationChange_Tick);
            timerDetectActivationChange.Interval = TimeSpan.FromMilliseconds(500);
            timerDetectActivationChange.Start();
        }

        private void timerDetectActivationChange_Tick(object sender, EventArgs e)
        {
            IntPtr lastActiveWindow = Mubox.Control.Network.Client.LastActivatedClientWindowHandle;
            IntPtr foregroundWindow = Win32.Windows.GetForegroundWindow();
            if ((foregroundWindow == IntPtr.Zero) || (foregroundWindow == lastActiveWindow))
            {
                return;
            }
            if (Mubox.View.Client.ClientWindowCollection.Instance != null)
            {
                foreach (Mubox.View.Client.ClientWindow clientWindow in Mubox.View.Client.ClientWindowCollection.Instance)
                {
                    if (clientWindow.ClientState.NetworkClient != null)
                    {
                        if (clientWindow.ClientState.NetworkClient.WindowHandle == foregroundWindow)
                        {
                            lastActiveWindow = foregroundWindow;
                            if (Mubox.View.Server.ServerWindow.Instance != null)
                            {
                                Mubox.View.Server.ServerWindow.Instance.CoerceActivation(clientWindow.ClientState.NetworkClient.WindowHandle);
                            }
                            else
                            {
                                clientWindow.ClientState.NetworkClient.CoerceActivation();
                            }
                        }
                    }
                }
            }
        }

        private Dictionary<string, System.Drawing.Icon> IconHandles = null;

        protected override void OnInitialized(EventArgs e)
        {
            NotifyPropertyChangedExtensions.UIDispatcher = this.Dispatcher;
            IconHandles = new Dictionary<string, System.Drawing.Icon>();
            IconHandles.Add("QuickLaunch", new System.Drawing.Icon(System.IO.Path.Combine(Environment.CurrentDirectory, @"Notification\Icons\network_center.ico")));
            notifyIcon = new System.Windows.Forms.NotifyIcon();
            notifyIcon.Click += notifyIcon_Click;
            notifyIcon.DoubleClick += notifyIcon_DoubleClick;
            notifyIcon.Icon = IconHandles["QuickLaunch"];
            Loaded += OnLoaded;
            StateChanged += OnStateChanged;
            Closing += OnClosing;
            Closed += OnClosed;
            Dispatcher.UnhandledException += new System.Windows.Threading.DispatcherUnhandledExceptionEventHandler(Dispatcher_UnhandledException);

            if (Mubox.Configuration.MuboxConfigSection.Default.AutoStartServer)
            {
                Mubox.View.SysTrayMenu.CreateServerUI();
            }

            base.OnInitialized(e);
        }

        private void Dispatcher_UnhandledException(object sender, System.Windows.Threading.DispatcherUnhandledExceptionEventArgs e)
        {
            if (!e.Handled)
            {
                System.Net.WebException ex = e.Exception as System.Net.WebException;
                if (ex != null)
                {
                    e.Handled = true;
                    Pages.ErrorPage errorPage = new Mubox.QuickLaunch.Pages.ErrorPage();
                    errorPage.DataContext = ex;
                    frameContentPage.Navigate(errorPage);
                }
            }
        }

        private void OnStateChanged(object sender, EventArgs e)
        {
            if (this.WindowState == WindowState.Minimized)
            {
                this.Topmost = false;
                this.ShowInTaskbar = false;
                notifyIcon.Visible = true;
            }
            else
            {
                notifyIcon.Visible = true;
                this.ShowInTaskbar = true;
                this.Topmost = true;
            }
        }

        private void OnClosing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            if (this.WindowState != WindowState.Minimized)
            {
                e.Cancel = true;
                // TODO Load /Pages/QuickLaunchMenu.xaml explaining how to get window back.
                this.WindowState = WindowState.Minimized;
            }
        }

        private void OnClosed(object sender, System.EventArgs e)
        {
            try
            {
                if (notifyIcon != null)
                {
                    notifyIcon.Visible = false;
                    notifyIcon.Dispose();
                    notifyIcon = null;
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex.Message);
                Debug.WriteLine(ex.StackTrace);
            }
        }

        private void OnLoaded(object sender, RoutedEventArgs e)
        {
            if (!this.Title.Contains("|"))
            {
                Version version = typeof(AppWindow).Assembly.GetName().Version;
                try
                {
                    version = System.Deployment.Application.ApplicationDeployment.CurrentDeployment.CurrentVersion;
                }
                catch
                {
                }
                this.Title = this.Title + " | " + version.ToString() + " (final)";

                Uri uri = new Uri("http://www.dualbox.net/");
                NavigateInternal(uri);
            }
            notifyIcon.Visible = true;
        }

        private void notifyIcon_Click(object sender, EventArgs e)
        {
            ShowSysTrayMenu();
        }

        private void ShowSysTrayMenu()
        {
            Mubox.View.SysTrayMenu sysTrayMenu = new Mubox.View.SysTrayMenu(
                () =>
                {
                    NavigateInternal(new Uri("http://www.dualbox.net/"));
                    ShowNavigationWindow();
                },
                () =>
                {
                    WindowState = WindowState.Minimized;
                    Close();
                });
            sysTrayMenu.IsOpen = true;
        }

        private void ShowNavigationWindow()
        {
            if (this.WindowState == WindowState.Minimized)
            {
                this.WindowState = WindowState.Normal;
            }
            this.Show();
            // TODO need hwnd
            // Win32.Windows.SetWindowPos(this.Handle, Win32.Windows.Position.HWND_TOP, -1, -1, -1, -1, Win32.Windows.Options.SWP_NOSIZE | Win32.Windows.Options.SWP_NOMOVE);
        }

        private void notifyIcon_DoubleClick(object sender, EventArgs e)
        {
            // NOP
        }

        public static Uri TryAgainSource { get; set; }

        private void NavigateInternal(Uri uri)
        {
            Dispatcher.BeginInvoke((Action<Uri>)delegate(Uri L_uri)
            {
                try
                {
                    //TryAgainSource = uri;
                    frameContentPage.Navigate(uri);
                }
                catch
                {
                    // TODO Log
                }
            }, uri);
        }

        public void ShowErrorPage(Exception ex)
        {
            Pages.ErrorPage errorPage = new Mubox.QuickLaunch.Pages.ErrorPage();
            errorPage.DataContext = ex ?? new Exception("Network failure, check your Firewall.");
            this.frameContentPage.Navigate(errorPage);
        }

        private void frameContentPage_Navigated(object sender, NavigationEventArgs e)
        {
            try
            {
                if (e.Uri != null)
                {
                    if (e.Uri.ToString().Contains("about:"))
                    {
                        ShowErrorPage(null);
                    }
                }
            }
            catch
            {
                ShowErrorPage(null);
            }
        }

        private void frameContentPage_NavigationFailed(object sender, NavigationFailedEventArgs e)
        {
        }
    }
}