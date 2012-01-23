using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace Mubox.View.Client
{
    /// <summary>
    /// Interaction logic for ClientWindow.xaml
    /// </summary>
    public partial class ClientWindow
        : Window
    {
        public Mubox.Model.ClientState ClientState
        {
            get
            {
                return (this.DataContext as Mubox.Model.ClientState);
            }
            set
            {
                if (value != this.DataContext)
                {
                    object oldValue = this.DataContext;
                    this.DataContext = value;
                    OnPropertyChanged(new DependencyPropertyChangedEventArgs(DataContextProperty, oldValue, value));
                }
            }
        }

        System.Windows.Threading.DispatcherTimer timerStatusRefresh;

        public long AutoReconnectLastAttempt { get; set; }

        public int AutoReconnectDelay = 9;
        public string ApplicationLaunchPath { get; set; }

        public ClientWindow(Mubox.Model.ClientState clientState)
        {
            Mubox.View.Client.ClientWindowCollection.Instance.Add(this);
            Debug.Assert(clientState != null);
            try
            {
                this.Dispatcher.UnhandledException += (sender, e) =>
                {
                    try
                    {
                        // TODO remove this event handler, we throw during disconnect when exiting mubox because window closure and network disconnection race
                        e.Handled = e.Exception.InnerException is ObjectDisposedException;
                        if (!e.Handled)
                        {
                            StringBuilder sb = new StringBuilder();
                            Exception ex = e.Exception;
                            while (ex != null)
                            {
                                sb.AppendLine("-------- " + ex.Message).AppendLine(ex.StackTrace);
                                ex = ex.InnerException;
                            }
                            MessageBox.Show("An error has occurred. You may want to restart Mubox and re-try. If this error continues, please send a screenshot of this error pop-up" + Environment.NewLine + "to mubox@mrshaunwilson.com, including steps to reproduce. A fix will be provided shortly thereafter." + Environment.NewLine + Environment.NewLine + "--- BEGIN ERROR INFO" + Environment.NewLine + "---" + sb.ToString(), "Mubox Error", MessageBoxButton.OK, MessageBoxImage.Error);
                        }
                    }
                    catch (Exception ex)
                    {
                        Debug.WriteLine(ex.Message);
                        Debug.WriteLine(ex.StackTrace);
                    }
                };

                // create default state so we can perform initialization without errors related to state-saving
                InitializeComponent();

                clientState.GameProcessExited += (s, e) =>
                    {
                        this.Dispatcher.BeginInvoke((Action)delegate()
                        {
                            try
                            {
                                InitializeWindowPositionAndSizeInputs();
                                clientState_SetStatusText("Application Exit.");
                                buttonLaunchApplication.IsEnabled = true;
                                this.Show();
                                ToggleApplicationLaunchButton();
                            }
                            catch
                            {
                                // NOP - known issue during shutdown where client window is in a closing state before game process has exited, resulting in exception.
                            }
                        });
                    };

                clientState.GameProcessFound += (s, e) =>
                    {
                    };

                ClientState = clientState;

                textMachineName.Text = ClientState.Settings.Name;
                textServerName.Text = ClientState.Settings.ServerName;
                textPortNumber.Text = ClientState.Settings.ServerPortNumber.ToString();
                textApplicationPath.Text = ClientState.Settings.ApplicationPath;
                textApplicationArguments.Text = this.ClientState.Settings.ApplicationArguments;
                checkIsolateApplication.IsChecked = ClientState.Settings.EnableIsolation;
                checkRememberWindowPosition.IsChecked = ClientState.Settings.RememberWindowPosition;
                InitializeWindowPositionAndSizeInputs();
                ToggleWindowPositionInputs();
                checkRemoveWindowBorder.IsChecked = ClientState.Settings.RemoveWindowBorderEnabled;
                textIsolationPath.Text = ClientState.Settings.IsolationPath;
                textWorkingSetMB.Text = ClientState.Settings.MemoryMB.ToString();
                comboProcessorAffinity.Items.Add("Use All");
                for (int i = 0; i < Environment.ProcessorCount; i++)
                {
                    comboProcessorAffinity.Items.Add((i + 1).ToString());
                }
                if (clientState.Settings.ProcessorAffinity < comboProcessorAffinity.Items.Count)
                {
                    comboProcessorAffinity.SelectedIndex = (int)ClientState.Settings.ProcessorAffinity;
                }
                else
                {
                    comboProcessorAffinity.SelectedIndex = 0;
                }

                timerStatusRefresh = new System.Windows.Threading.DispatcherTimer(System.Windows.Threading.DispatcherPriority.Normal);
                timerStatusRefresh.Interval = TimeSpan.FromMilliseconds(500);
                timerStatusRefresh.Tick += (sender, e) =>
                    {
                        timerStatusRefresh.Stop();
                        timerStatusRefresh.Interval = TimeSpan.FromMilliseconds(500);
                        try
                        {
                            if (ClientState.NetworkClient != null)
                            {
                                buttonConnect.IsEnabled = false;
                                textServerName.IsEnabled = false;
                                textPortNumber.IsEnabled = false;
                                timerStatusRefresh.Interval = TimeSpan.FromMilliseconds(5000);
                            }
                            else if (clientState_AutoReconnect)
                            {
                                if (buttonConnect.IsEnabled)
                                {
                                    if (TimeSpan.FromTicks(DateTime.Now.Ticks - AutoReconnectLastAttempt).TotalSeconds >= AutoReconnectDelay)
                                    {
                                        buttonConnect_Click(null, null);
                                        AutoReconnectLastAttempt = DateTime.Now.Ticks;
                                    }
                                }
                            }
                            else if (clientState.Settings.PerformConnectOnLoad)
                            {
                                if (buttonConnect.IsEnabled)
                                {
                                    buttonConnect_Click(null, null);
                                }
                            }
                            clientState_SetStatusText(clientState_lastStatus);
                        }
                        catch (Exception ex)
                        {
                            Debug.WriteLine(ex.Message);
                            Debug.WriteLine(ex.StackTrace);
                        }
                        finally
                        {
                            timerStatusRefresh.Start();
                        }
                    };
                timerStatusRefresh.Start();
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message + Environment.NewLine + "---" + Environment.NewLine + ex.StackTrace, "Exception Info");
                try
                {
                    Close();
                }
                catch (Exception lex)
                {
                    Debug.WriteLine(lex.Message);
                    Debug.WriteLine(lex.StackTrace);
                }
            }
        }

        private void InitializeWindowPositionAndSizeInputs()
        {
            textWindowPositionLeft.Text = ClientState.Settings.WindowPosition.X.ToString();
            textWindowPositionTop.Text = ClientState.Settings.WindowPosition.Y.ToString();
            textWindowSizeWidth.Text = ClientState.Settings.WindowSize.Width.ToString();
            textWindowSizeHeight.Text = ClientState.Settings.WindowSize.Height.ToString();
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            if (this.ClientState.SettingsWindow == null)
            {
                this.ClientState.SettingsWindow = this;
                this.ClientState.Settings.WindowHandle = IntPtr.Zero;
            }
        }

        protected override void OnInitialized(EventArgs e)
        {
            base.OnInitialized(e);
        }

        protected override void OnClosed(EventArgs e)
        {
            base.OnClosed(e);
        }

        protected override void OnClosing(System.ComponentModel.CancelEventArgs e)
        {
            Mubox.View.Client.ClientWindowCollection.Instance.Remove(this);
            ClientState.Dispose();
            try
            {
                if (timerStatusRefresh != null)
                {
                    timerStatusRefresh.Stop();
                    timerStatusRefresh = null;
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex.Message);
                Debug.WriteLine(ex.StackTrace);
            }
            base.OnClosing(e);
        }

        // TODO set this to false when a |DC is received, to signal server-side forced closure
        public bool clientState_AutoReconnect { get; set; }

        private object buttonConnectLock = new object();

        private void buttonConnect_Click(object sender, RoutedEventArgs e)
        {
            lock (buttonConnectLock)
            {
                this.Dispatcher.Invoke((Action)delegate()
                {
                    IntPtr myWindowHandle = new System.Windows.Interop.WindowInteropHelper(this).Handle;
                    if (myWindowHandle != IntPtr.Zero)
                    {
                        IntPtr myInputQueue = IntPtr.Zero;
                        Mubox.Win32.Threads.GetWindowThreadProcessId(myWindowHandle, out myInputQueue);
                        if (myInputQueue != IntPtr.Zero)
                        {
                            Mubox.Control.Network.Client.MyInputQueue = myInputQueue;
                        }
                        else
                        {
                            Debug.WriteLine("GetWindowThreadProcessId Failed for " + this.ClientState.Settings.Name);
                        }
                    }
                    else
                    {
                        Debug.WriteLine("WindowInteropHelper Failed for " + this.ClientState.Settings.Name);
                    }
                });
                if (ClientState.NetworkClient == null)
                {
                    try
                    {
                        clientState_SetStatusText("Connecting...");
                        ClientState.NetworkClient = new Mubox.Control.Network.Client
                        {
                            DisplayName = ClientState.Settings.Name,
                            WindowStationHandle = ClientState.WindowStationHandle,
                            WindowDesktopHandle = ClientState.WindowDesktopHandle,
                            WindowHandle = ClientState.Settings.WindowHandle
                        };
                        ClientState.NetworkClient.Connected += new EventHandler<EventArgs>(Client_Connected);
                        ClientState.NetworkClient.Disconnected += new EventHandler<EventArgs>(Client_Disconnected);
                        ClientState.NetworkClient.Connect(ClientState.Settings.ServerName, ClientState.Settings.ServerPortNumber);
                        tabControl1.SelectedItem = tabGameSettings;
                        clientState_AutoReconnect = true;
                    }
                    catch (Exception ex)
                    {
                        ClientState.NetworkClient = null;
                        clientState_SetStatusText("Connect Failed: " + ex.Message);
                    }
                }
            }
        }

        private string clientState_lastStatus = "Not Connected";
        private void clientState_SetStatusText(string status)
        {
            bool changed = clientState_lastStatus != status;
            clientState_lastStatus = status;
            if (ClientState != null)
            {
                if (ClientState.NetworkClient == null)
                {
                    if (clientState_AutoReconnect)
                    {
                        if (TimeSpan.FromTicks(DateTime.Now.Ticks - AutoReconnectLastAttempt).TotalSeconds < AutoReconnectDelay)
                        {
                            status += Environment.NewLine + "Disconnected, next connection attempt in " + (int)(TimeSpan.FromTicks(AutoReconnectLastAttempt).Add(TimeSpan.FromSeconds(AutoReconnectDelay)).Subtract(TimeSpan.FromTicks(DateTime.Now.Ticks)).TotalSeconds + 1) + " seconds.";
                        }
                    }
                }
            }
            if (changed)
            {
                this.Dispatcher.BeginInvoke((Action)delegate()
                {
                    textStatus.Text = status;
                });
            }
        }

        private void Client_Connected(object sender, EventArgs e)
        {
            clientState_SetStatusText("Connected." + Environment.NewLine + "You can edit fields above while connected.");
            this.Dispatcher.Invoke((Action)delegate()
            {
                if (ClientState.NetworkClient != null)
                {
                    buttonConnect.IsEnabled = false;
                    ToggleApplicationLaunchButton();
                    textServerName.IsEnabled = false;
                    textPortNumber.IsEnabled = false;
                    if (ClientState.Settings.PerformConnectOnLoad)
                    {
                        ClientState.Settings.PerformConnectOnLoad = false;
                        Mubox.Configuration.MuboxConfigSection.Default.Save();
                        if (Mubox.Configuration.MuboxConfigSection.Default.AutoLaunchGame)
                        {
                            if (buttonLaunchApplication.IsEnabled)
                            {
                                buttonLaunchApplication_Click(null, null);
                            }
                        }
                    }
                }
            });
        }

        private void Client_Disconnected(object sender, EventArgs e)
        {
            this.Dispatcher.Invoke((Action)delegate()
            {
                if (ClientState.NetworkClient != null)
                {
                    ClientState.NetworkClient = null;
                    try
                    {
                        try
                        {
                            this.Show();
                        }
                        catch { }
                        clientState_SetStatusText("Disconnected " + DateTime.Now);
                        buttonConnect.IsEnabled = true;
                        clientState_AutoReconnect = true;
                        textServerName.IsEnabled = false;
                        textPortNumber.IsEnabled = false;
                    }
                    catch (Exception ex)
                    {
                        Debug.WriteLine(ex.Message);
                        Debug.WriteLine(ex.StackTrace);
                    }
                }
            });
        }

        public void ToggleApplicationLaunchButton()
        {
            try
            {
                buttonLaunchApplication.IsEnabled =
                    (
                    !buttonConnect.IsEnabled &&
                    ((ClientState.GameProcess == null) || (ClientState.GameProcess.HasExited == true))
                    )
                    &&
                    (
                    (ClientState.Settings.ApplicationPath.Trim('\\', ' ').Length > 0)
                    && (!ClientState.Settings.EnableIsolation || (!string.IsNullOrEmpty(ClientState.Settings.IsolationPath) && (ClientState.Settings.IsolationPath[0] == ClientState.Settings.ApplicationPath[0])))
                    );
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex.Message);
                Debug.WriteLine(ex.StackTrace);
            }

            try
            {
                if (buttonInstallMuboxer != null)
                {
                    buttonInstallMuboxer.IsEnabled =
                        buttonLaunchApplication.IsEnabled
                        && (textApplicationPath.Text ?? "").ToLower().EndsWith(@"\wow.exe");
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex.Message);
                Debug.WriteLine(ex.StackTrace);
            }

            try
            {
                if (System.IO.File.Exists(textApplicationPath.Text) && textApplicationPath.Text.EndsWith(".exe"))
                {
                    textApplicationPath.Background = SystemColors.WindowBrush;
                }
                else
                {
                    textApplicationPath.Background = Brushes.Red;
                    buttonLaunchApplication.IsEnabled = false;
                    if (buttonInstallMuboxer != null)
                    {
                        buttonInstallMuboxer.IsEnabled = false;
                    }
                }
                if ((ClientState != null) && ClientState.Settings.EnableIsolation)
                {
                    if (!string.IsNullOrEmpty(ClientState.Settings.IsolationPath) && (ClientState.Settings.IsolationPath[0] == ClientState.Settings.ApplicationPath[0]))
                    {
                        textIsolationPath.Background = SystemColors.WindowBrush;
                    }
                    else
                    {
                        textIsolationPath.Background = Brushes.Red;
                        buttonLaunchApplication.IsEnabled = false;
                        buttonInstallMuboxer.IsEnabled = false;
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex.Message);
                Debug.WriteLine(ex.StackTrace);
            }
        }

        private string GetSanitizedClientName()
        {
            return Mubox.Model.Client.ClientBase.Sanitize(textMachineName.Text);
        }

        private void textMachineName_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (textMachineName.Text.Length > 0)
            {
                Version version = typeof(Mubox.Control.Performance).Assembly.GetName().Version;
                string versionString = ((version.Major * 10) + version.Minor).ToString();
                string previousIdent = (string)Microsoft.Win32.Registry.CurrentUser.OpenSubKey("Software", true).CreateSubKey("Mubox").CreateSubKey(textMachineName.Text).GetValue("Ident", "");
                if (string.IsNullOrEmpty(previousIdent))
                {
                    // first-time init, store new ident, this is used for toon stats collection
                    previousIdent = Guid.NewGuid().ToString().Replace("-", versionString);
                    Microsoft.Win32.Registry.CurrentUser.OpenSubKey("Software", true).CreateSubKey("Mubox").CreateSubKey(textMachineName.Text).SetValue("Ident", previousIdent);
                }
                else
                {
                    // populate config from registry?
                }
                Microsoft.Win32.Registry.CurrentUser.OpenSubKey("Software", true).CreateSubKey("Mubox").CreateSubKey(textMachineName.Text).SetValue("Version", versionString);
            }
        }

        private void textPortNumber_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (ClientState != null)
            {
                int portNumber;
                if (int.TryParse(textPortNumber.Text, out portNumber))
                {
                    ClientState.Settings.ServerPortNumber = portNumber;
                }
            }
        }

        private void textServerName_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (ClientState != null)
            {
                ClientState.Settings.ServerName = textServerName.Text.Trim();
            }
        }

        public string AddOnInstallPath { get; set; }

        private void buttonInstallMuboxer_Click(object sender, RoutedEventArgs e)
        {
            ClientState.Settings.InstallWoWAddOn = true;
            Mubox.Configuration.MuboxConfigSection.Default.Save();
            try
            {
                ResolveAddOnInstallPath();

                if (!string.IsNullOrEmpty(AddOnInstallPath))
                {
                    // TODO infer these file names from disk to eliminate the need to maintain/hard-code this list
                    string[] addOnFileNames = new string[] {
                        "MbxCore.lua",
                        "MbxPlayer.lua",
                        "MbxMacros.lua",
                        "MbxHUD.lua",
                        "MbxGroup.lua",
                        "MbxEquipment.lua",
                        "MbxInventory.lua",
                        "MbxPvP.lua",
                        "Muboxer.lua",
                        "Muboxer.xml"
                    };
                    foreach (string addOnFileName in addOnFileNames)
                    {
                        InstallAddOnFile(addOnFileName);
                    }
                    InstallAddOnFile("Muboxer.toc");
                    InstallAddOnFile("readme.txt");
                    InstallAddOnFile("About.htm");
                    buttonInstallMuboxer.Content = "WoW AddOn Updated";
                    buttonInstallMuboxer.IsEnabled = false;
                    return;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message /* + Environment.NewLine + ex.StackTrace */, "Install Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            MessageBox.Show("Could not Update (or Locate) your WoW Interface AddOns Folder.", "Install Failed", MessageBoxButton.OK, MessageBoxImage.Stop);
        }

        private void ResolveAddOnInstallPath()
        {
            foreach (string defaultWowPath in new string[]
                    {
                        textApplicationPath.Text.Replace(System.IO.Path.GetFileName(textApplicationPath.Text), "")
                    })
            {
                string lDefaultWowPath = defaultWowPath.Replace("@SRV@", textMachineName.Text);
                if (System.IO.Directory.Exists(lDefaultWowPath))
                {
                    if (!System.IO.Directory.Exists(lDefaultWowPath + @"Interface\"))
                    {
                        System.IO.Directory.CreateDirectory(lDefaultWowPath + @"Interface\");
                    }
                    if (!System.IO.Directory.Exists(lDefaultWowPath + @"Interface\AddOns\"))
                    {
                        System.IO.Directory.CreateDirectory(lDefaultWowPath + @"Interface\AddOns\");
                    }
                    if (System.IO.Directory.Exists(lDefaultWowPath + @"Interface\AddOns\"))
                    {
                        if (!System.IO.Directory.Exists(lDefaultWowPath + @"Interface\AddOns\Muboxer"))
                        {
                            System.IO.Directory.CreateDirectory(lDefaultWowPath + @"Interface\AddOns\Muboxer");
                        }
                        if (System.IO.Directory.Exists(lDefaultWowPath + @"Interface\AddOns\Muboxer"))
                        {
                            AddOnInstallPath = lDefaultWowPath;
                            break;
                        }
                    }
                }
            }
        }

        private void InstallAddOnFile(string fileName)
        {
            fileName = fileName.Replace(System.IO.Path.DirectorySeparatorChar, '-');
            try
            {
                if (File.Exists(AddOnInstallPath + @"Interface\AddOns\Muboxer\" + fileName))
                {
                    FileInfo versionCheck = new FileInfo(AddOnInstallPath + @"Interface\AddOns\Muboxer\" + fileName);
                    FileInfo newVersionCheck = new FileInfo(Environment.CurrentDirectory + @"\AddOns\Muboxer\" + fileName);
                    if ((versionCheck.Length != newVersionCheck.Length) || (versionCheck.LastWriteTimeUtc < newVersionCheck.LastWriteTimeUtc))
                    {
                        FileCopy(Environment.CurrentDirectory + @"\AddOns\Muboxer\" + fileName, AddOnInstallPath + @"Interface\AddOns\Muboxer\" + fileName);
                    }
                }
                else
                {
                    FileCopy(Environment.CurrentDirectory + @"\AddOns\Muboxer\" + fileName, AddOnInstallPath + @"Interface\AddOns\Muboxer\" + fileName);
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex.Message);
                Debug.WriteLine(ex.StackTrace);
                FileCopy(Environment.CurrentDirectory + @"\AddOns\Muboxer\" + fileName, AddOnInstallPath + @"Interface\AddOns\Muboxer\" + fileName);
            }
        }

        private static readonly long MaxCopyBufferSize = 1024 * 1024;

        private void FileCopy(string s, string d)
        {
            using (System.IO.FileStream s_fs = System.IO.File.Open(s, System.IO.FileMode.Open, System.IO.FileAccess.Read, System.IO.FileShare.ReadWrite))
            {
                using (System.IO.FileStream d_fs = System.IO.File.Open(d, System.IO.FileMode.Create, System.IO.FileAccess.Write, System.IO.FileShare.ReadWrite))
                {
                    this.Dispatcher.Invoke((Action)delegate()
                    {
                        clientState_SetStatusText("Copying File... " + Environment.NewLine + "Src: " + System.IO.Path.GetFileName(d) + Environment.NewLine + "Size: " + s_fs.Length.ToString() + " Bytes.");
                    });
                    s_fs.CopyTo(d_fs, (int)(s_fs.Length > MaxCopyBufferSize ? MaxCopyBufferSize : s_fs.Length));
                }
            }
        }

        private void buttonLaunchApplication_Click(object sender, RoutedEventArgs e)
        {
            AutoUpdateMuboxerAddOn();
            try
            {
                ClientState.Settings.WindowPosition = new Point(int.Parse(textWindowPositionLeft.Text), int.Parse(textWindowPositionTop.Text));
                ClientState.Settings.WindowSize = new Size(int.Parse(textWindowSizeWidth.Text), int.Parse(textWindowSizeHeight.Text));

                buttonLaunchApplication.IsEnabled = false;
                ApplicationLaunchPath = textApplicationPath.Text;
                ClientState.Settings.IsolationPath = textIsolationPath.Text.TrimEnd(' ', '\\') + '\\';
                System.IO.Path.GetFileName(ApplicationLaunchPath);
                string applicationExeName = System.IO.Path.GetFileName(textApplicationPath.Text);
                bool checkIsolateApplication_IsChecked = checkIsolateApplication.IsChecked.GetValueOrDefault(false);
                buttonLaunchApplication.IsEnabled = false;
                PerformApplicationLaunch();
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex.Message);
                Debug.WriteLine(ex.StackTrace);
            }
        }

        private void AutoUpdateMuboxerAddOn()
        {
            try
            {
                if (buttonInstallMuboxer.IsEnabled)
                {
                    ResolveAddOnInstallPath();
                    if (System.IO.Directory.Exists(AddOnInstallPath + @"Interface\AddOns\Muboxer\"))
                    {
                        buttonInstallMuboxer_Click(null, null);
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex.Message);
                Debug.WriteLine(ex.StackTrace);
            }
        }

        private void PerformApplicationLaunch()
        {
            string applicationExeName = System.IO.Path.GetFileName(ClientState.Settings.ApplicationPath);

            try
            {
                if (System.IO.File.Exists(ApplicationLaunchPath))
                {
                    if (ClientState.Settings.EnableIsolation)
                    {
                        try
                        {
                            if (!System.IO.Directory.Exists(ClientState.Settings.IsolationPath))
                            {
                                System.IO.Directory.CreateDirectory(ClientState.Settings.IsolationPath);
                            }

                            string adjustedIsolationPath = System.IO.Path.Combine(ClientState.Settings.IsolationPath, ClientState.Settings.Name);

                            if (!System.IO.Directory.Exists(adjustedIsolationPath))
                            {
                                System.IO.Directory.CreateDirectory(adjustedIsolationPath);
                            }

                            IsolateFiles(ApplicationLaunchPath.Replace(applicationExeName, ""), adjustedIsolationPath);

                            ApplicationLaunchPath = System.IO.Path.Combine(adjustedIsolationPath, applicationExeName);
                        }
                        catch (Exception ex)
                        {
                            ApplicationLaunchPath = ClientState.Settings.ApplicationPath;
                            MessageBox.Show(ex.Message + "|" + ex.StackTrace, "Isolation Failed", MessageBoxButton.OK, MessageBoxImage.Error);
                            return;
                        }
                    }

                    if (ApplicationLaunchPath.StartsWith(@"\\"))
                    {
                        MessageBox.Show("Launching from Network Share is not supported, aborting launch.", "Error, Network Share", MessageBoxButton.OK, MessageBoxImage.Error);
                        return;
                    }

                    // TODO if this.ClientState.GameProcess != null, dispose it and synchronize on shutdown (req. for problem-free file replication)
                    this.ClientState.GameProcess = null;

                    Control.FileReplicationManager.PerformReplication(this.ClientState.Settings.Files);

                    ProcessStartInfo startInfo = new ProcessStartInfo(ApplicationLaunchPath);
                    startInfo.Arguments = ClientState.Settings.ApplicationArguments;
                    startInfo.WorkingDirectory = ApplicationLaunchPath.Replace(applicationExeName, "");
                    startInfo.UseShellExecute = false;

                    ToggleApplicationLaunchButton();
                    this.Hide();

                    Process process = new Process();
                    process.StartInfo = startInfo;
                    process.Start();
                    this.ClientState.GameProcess = process;
                    this.Dispatcher.BeginInvoke((Action)delegate() { this.tabControl1.SelectedItem = tabProcessManagement; });
                    clientState_SetStatusText("Application Started.");
                    return;
                }
            }
            finally
            {
                ToggleApplicationLaunchButton();
            }
        }

        private string IsolateFiles(string source, string destination)
        {
            string[] files = System.IO.Directory.GetFiles(source);
            string[] directories = System.IO.Directory.GetDirectories(source);

            foreach (string file in files)
            {
                string target = System.IO.Path.Combine(destination, file.Replace(source, ""));
                System.IO.File.Copy(file, target, true);
            }

            string[] doNotJunctionList = new[]
                {
                    @"\Cache",
                    @"\Logs",
                    @"\Screenshots",
                    @"\WTF",
                }; // TODO: should also NOT junction any folders which are configured for replication

            foreach (string directory in directories)
            {
                string target = System.IO.Path.Combine(destination, directory.Replace(source, ""));
                var doNotJunction = false;
                foreach (var item in doNotJunctionList)
                {
                    if (target.EndsWith(item))
                    {
                        doNotJunction = true;
                        break;
                    }
                }
                if (!doNotJunction)
                {
                    if (!System.IO.Directory.Exists(target))
                    {
                        Win32.IsolationApi.CreateFolder(target, directory);
                    }
                }
            }

            return destination;
        }

        private void checkIsolateApplication_Checked(object sender, RoutedEventArgs e)
        {
            if (ClientState != null)
            {
                if (!ClientState.Settings.EnableIsolation)
                {
                    if (MessageBoxResult.Yes == MessageBox.Show("This option will create filesystem links to the content of your game folder at the configured Isolation Path." + Environment.NewLine +
                        "This will only happen once per Unique Client Name, and only when you click the \"Launch\" Button." + Environment.NewLine +
                        "If you're not sure why you would want this, just click \"No\". This will disable Isolation.",
                        "Enable Isolation?",
                        MessageBoxButton.YesNo,
                        MessageBoxImage.Question))
                    {
                        ClientState.Settings.EnableIsolation = true;
                    }
                    else
                    {
                        checkIsolateApplication.IsChecked = false;
                    }
                }
            }
        }

        private void checkIsolateApplication_Unchecked(object sender, RoutedEventArgs e)
        {
            if (ClientState != null)
            {
                ClientState.Settings.EnableIsolation = false;
            }
        }

        private void textApplicationPath_TextChanged(object sender, TextChangedEventArgs e)
        {
            try
            {
                if (System.IO.File.Exists(textApplicationPath.Text))
                {
                    ClientState.Settings.ApplicationPath = textApplicationPath.Text;
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex.Message);
                Debug.WriteLine(ex.StackTrace);
            }
            ToggleApplicationLaunchButton();
        }

        private void textApplicationArguments_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (ClientState != null)
            {
                ClientState.Settings.ApplicationArguments = textApplicationArguments.Text;
            }
        }

        private void textIsolationPath_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (ClientState != null)
            {
                ClientState.Settings.IsolationPath = textIsolationPath.Text;
                ToggleApplicationLaunchButton();
            }
        }

        #region Migrated App Members

        public void DebugRestrictOptions()
        {
            checkRememberWindowPosition.IsChecked = false;
            checkRememberWindowPosition.Visibility = Visibility.Collapsed;
            checkRemoveWindowBorder.IsChecked = false;
            checkRemoveWindowBorder.Visibility = Visibility.Collapsed;
            labelWindowPositionLeft.Visibility = Visibility.Collapsed;
            labelWindowPositionTop.Visibility = Visibility.Collapsed;
            labelWindowSizeWidth.Visibility = Visibility.Collapsed;
            labelWindowSizeHeight.Visibility = Visibility.Collapsed;
            textWindowPositionLeft.Visibility = Visibility.Collapsed;
            textWindowPositionTop.Visibility = Visibility.Collapsed;
            textWindowSizeWidth.Visibility = Visibility.Collapsed;
            textWindowSizeHeight.Visibility = Visibility.Collapsed;
        }

        #endregion

        private void textWorkingSetMB_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (ClientState != null)
            {
                if (!string.IsNullOrEmpty(textWorkingSetMB.Text))
                {
                    int memoryMB;
                    if (int.TryParse(textWorkingSetMB.Text, out memoryMB))
                    {
                        if (memoryMB > 0)
                        {
                            ClientState.Settings.MemoryMB = memoryMB;
                            return;
                        }
                    }
                }
                ClientState.Settings.MemoryMB = 0;
            }
        }

        private void comboProcessorAffinity_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (ClientState != null)
            {
                if (comboProcessorAffinity.SelectedIndex < 1)
                {
                    ClientState.Settings.ProcessorAffinity = 0;
                }
                else
                {
                    ClientState.Settings.ProcessorAffinity = comboProcessorAffinity.SelectedIndex < 1 ? 0 : (uint)comboProcessorAffinity.SelectedIndex;
                }
            }
        }

        private void checkRememberWindowPosition_Checked(object sender, RoutedEventArgs e)
        {
            if (ClientState != null)
            {
                ClientState.Settings.RememberWindowPosition = true;
                ToggleWindowPositionInputs();
            }
        }

        private void checkRememberWindowPosition_Unchecked(object sender, RoutedEventArgs e)
        {
            if (ClientState != null)
            {
                ClientState.Settings.RememberWindowPosition = false;
                ToggleWindowPositionInputs();
            }
        }

        private void ToggleWindowPositionInputs()
        {
            textWindowPositionLeft.IsEnabled = checkRememberWindowPosition.IsChecked.GetValueOrDefault(false);
            textWindowPositionTop.IsEnabled = checkRememberWindowPosition.IsChecked.GetValueOrDefault(false);
            textWindowSizeWidth.IsEnabled = checkRememberWindowPosition.IsChecked.GetValueOrDefault(false);
            textWindowSizeHeight.IsEnabled = checkRememberWindowPosition.IsChecked.GetValueOrDefault(false);
        }

        private void checkRemoveWindowBorder_Checked(object sender, RoutedEventArgs e)
        {
            if (ClientState != null)
            {
                ClientState.Settings.RemoveWindowBorderEnabled = true;
            }
        }

        private void checkRemoveWindowBorder_Unchecked(object sender, RoutedEventArgs e)
        {
            if (ClientState != null)
            {
                ClientState.Settings.RemoveWindowBorderEnabled = false;
            }
        }

        private void buttonAddFileReplicationSetting_Click(object sender, RoutedEventArgs e)
        {
            // TODO file replication will occur on Mubox start-up, to ensure all files are synchronized
            // TODO file replication will create file watchers for any folder that contains a file required for file replication, if the file is updated/created/etc the file is copied immediately
            // TODO files are added to file replication settings via client settings window, but stored in global config

            // retrieve 'source' file from user
            System.Windows.Forms.OpenFileDialog openFileDialog = new System.Windows.Forms.OpenFileDialog
            {
                AutoUpgradeEnabled = true,
                CheckFileExists = true,
                CheckPathExists = true,
                DereferenceLinks = true,
                InitialDirectory = string.IsNullOrEmpty(Mubox.Configuration.MuboxConfigSection.Default.OpenFileDialogInitialDirectory)
                    ? System.IO.Path.GetDirectoryName(textApplicationPath.Text)
                    : Mubox.Configuration.MuboxConfigSection.Default.OpenFileDialogInitialDirectory,
                Multiselect = false,
                RestoreDirectory = true,
                ShowHelp = false,
                ShowReadOnly = false,
                SupportMultiDottedExtensions = true,
                Title = "Select 'Source' File to Replicate"
            };
            if (openFileDialog.ShowDialog() != System.Windows.Forms.DialogResult.OK)
            {
                return;
            }
            Mubox.Configuration.MuboxConfigSection.Default.OpenFileDialogInitialDirectory = System.IO.Path.GetDirectoryName(openFileDialog.FileName);

            // retrieve 'destination' from user
            System.Windows.Forms.SaveFileDialog saveFileDialog = new System.Windows.Forms.SaveFileDialog
            {
                AutoUpgradeEnabled = true,
                CheckFileExists = false,
                CheckPathExists = true,
                DereferenceLinks = true,
                InitialDirectory = (ClientState.Settings.EnableIsolation && (!string.IsNullOrEmpty(ClientState.Settings.IsolationPath))
                    ? System.IO.Path.Combine(ClientState.Settings.IsolationPath, this.ClientState.Settings.Name)
                    : Environment.CurrentDirectory),
                FileName = System.IO.Path.GetFileName(openFileDialog.FileName),
                RestoreDirectory = true,
                ShowHelp = false,
                SupportMultiDottedExtensions = true,
                Title = "Select a 'Destination' for the File, including the desired File Name",
                CreatePrompt = false,
                OverwritePrompt = false
            };
            if (saveFileDialog.ShowDialog() != System.Windows.Forms.DialogResult.OK)
            {
                return;
            }

            // add or update replication setting
            Configuration.FileReplicationSetting file;
            if (!ClientState.Settings.Files.TryGetKeySetting(openFileDialog.FileName, out file))
            {
                file = ClientState.Settings.Files.CreateNew(openFileDialog.FileName);
            }
            file.Destination = saveFileDialog.FileName;
            Mubox.Configuration.MuboxConfigSection.Default.Save();

            // TODO "add file" should WARN if user selects an input file that is greater than 50MB in size
        }

        private void listFileReplicationSettings_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            buttonRemoveFileReplicationSetting.IsEnabled = (listFileReplicationSettings.SelectedItems.Count > 0);
        }

        private void buttonRemoveFileReplicationSetting_Click(object sender, RoutedEventArgs e)
        {
            foreach (var item in new List<Configuration.FileReplicationSetting>(listFileReplicationSettings.SelectedItems.OfType<Configuration.FileReplicationSetting>()))
            {
                ClientState.Settings.Files.Remove(item.Source);
            }
        }

        private void vkBoardFtlSettings_Loaded(object sender, RoutedEventArgs e)
        {
            InitializeFtlButtonStates();
        }

        private void InitializeFtlButtonStates()
        {
            foreach (var keySetting in this.ClientState.Settings.Keys.OfType<Configuration.KeySetting>())
            {
                hasInitializedFtlButtonState = false;
                checkCASControl.IsChecked = (keySetting.OutputModifiers & Win32.CAS.CONTROL) == Win32.CAS.CONTROL;
                checkCASAlt.IsChecked = (keySetting.OutputModifiers & Win32.CAS.ALT) == Win32.CAS.ALT;
                checkCASShift.IsChecked = (keySetting.OutputModifiers & Win32.CAS.SHIFT) == Win32.CAS.SHIFT;
                checkNoModActiveClient.IsChecked = keySetting.EnableNoModActiveClient;
                break;
            }
            hasInitializedFtlButtonState = true;
            vkBoardFtlSettings.InitializeButtonState(
                this.ClientState.Settings.Keys,
                (keySetting) =>
                {
                    return
                        (checkCASControl.IsChecked.GetValueOrDefault(default(bool)) && ((keySetting.OutputModifiers & Win32.CAS.CONTROL) == Win32.CAS.CONTROL)) ||
                        (checkCASAlt.IsChecked.GetValueOrDefault(default(bool)) && ((keySetting.OutputModifiers & Win32.CAS.ALT) == Win32.CAS.ALT)) ||
                        (checkCASShift.IsChecked.GetValueOrDefault(default(bool)) && ((keySetting.OutputModifiers & Win32.CAS.SHIFT) == Win32.CAS.SHIFT)) ||
                        (checkNoModActiveClient.IsChecked.GetValueOrDefault(default(bool)) && keySetting.EnableNoModActiveClient);
                },
                (keySetting) =>
                {
                    keySetting.OutputModifiers = (Win32.CAS)0;
                    if (checkCASControl.IsChecked.GetValueOrDefault(default(bool)))
                    {
                        keySetting.OutputModifiers |= Win32.CAS.CONTROL;
                    }
                    if (checkCASAlt.IsChecked.GetValueOrDefault(default(bool)))
                    {
                        keySetting.OutputModifiers |= Win32.CAS.ALT;
                    }
                    if (checkCASShift.IsChecked.GetValueOrDefault(default(bool)))
                    {
                        keySetting.OutputModifiers |= Win32.CAS.SHIFT;
                    }
                    keySetting.EnableNoModActiveClient = checkNoModActiveClient.IsChecked.GetValueOrDefault(default(bool));
                },
                (keySetting) =>
                {
                    keySetting.OutputModifiers = (Win32.CAS)0;
                    keySetting.EnableNoModActiveClient = false;
                });
        }

        private bool hasInitializedFtlButtonState = default(bool);

        private void checkNoModActiveClient_Checked(object sender, RoutedEventArgs e)
        {
            if (!hasInitializedFtlButtonState)
            {
                return;
            }
            foreach (var keySetting in this.ClientState.Settings.Keys.OfType<Configuration.KeySetting>())
            {
                keySetting.OutputModifiers = (Win32.CAS)0;
                if (checkCASControl.IsChecked.GetValueOrDefault(default(bool)))
                {
                    keySetting.OutputModifiers |= Win32.CAS.CONTROL;
                }
                if (checkCASAlt.IsChecked.GetValueOrDefault(default(bool)))
                {
                    keySetting.OutputModifiers |= Win32.CAS.ALT;
                }
                if (checkCASShift.IsChecked.GetValueOrDefault(default(bool)))
                {
                    keySetting.OutputModifiers |= Win32.CAS.SHIFT;
                }
                keySetting.EnableNoModActiveClient = checkNoModActiveClient.IsChecked.GetValueOrDefault(default(bool));
            }
            Mubox.Configuration.MuboxConfigSection.Default.Save();
            InitializeFtlButtonStates();
        }

        private void checkNoModActiveClient_Unchecked(object sender, RoutedEventArgs e)
        {
            if (!hasInitializedFtlButtonState)
            {
                return;
            }
            foreach (var keySetting in this.ClientState.Settings.Keys.OfType<Configuration.KeySetting>())
            {
                keySetting.OutputModifiers = (Win32.CAS)0;
                keySetting.EnableNoModActiveClient = false;
            }
            Mubox.Configuration.MuboxConfigSection.Default.Save();
            InitializeFtlButtonStates();
        }
    }
}