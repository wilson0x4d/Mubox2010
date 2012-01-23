using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Windows;

namespace Mubox.Model
{
    public class ClientState : IDisposable
    {
        private ClientState()
        {
            Timer = new System.Timers.Timer();
            Timer.Elapsed += Timer_Elapsed;
            Timer.Interval = 1000;
            Timer.Start();
        }

        public ClientState(Mubox.Configuration.ClientSettings settings)
            : this()
        {
            Settings = settings;
        }

        public Configuration.ClientSettings Settings { get; private set; }

        #region State Monitor

        private DateTime clientState_lastTimerTick = DateTime.Now;
        private long NetworkClientNextServerUpdateTime;
        private bool clientState_windowPositionRestored;
        private bool clientState_windowBorderRemoved;

        private void Timer_Elapsed(object sender, System.Timers.ElapsedEventArgs e)
        {
            MonitorGameProcess();

            // update server with current client info, this doubles as "stale socket detection" code
            if (NetworkClient != null)
            {
                // trim memory
                MonitorMemoryMB();

                try
                {
                    MonitorWindowBorder(false);
                }
                catch (Exception ex)
                {
                    Debug.WriteLine(ex.Message);
                    Debug.WriteLine(ex.StackTrace);
                }

                try
                {
                    MonitorWindowPosition();
                }
                catch (Exception ex)
                {
                    Debug.WriteLine(ex.Message);
                    Debug.WriteLine(ex.StackTrace);
                }

                try
                {
                    MonitorNetworkClient();
                }
                catch (Exception ex)
                {
                    Debug.WriteLine(ex.Message);
                    Debug.WriteLine(ex.StackTrace);
                }
            }
            clientState_lastTimerTick = DateTime.Now;
        }

        public void MonitorWindowBorder(bool force)
        {
            bool windowBorderRemoved = force ? false : this.clientState_windowBorderRemoved;
            if (this.Settings.WindowHandle != IntPtr.Zero)
            {
                if (Settings.RemoveWindowBorderEnabled && !windowBorderRemoved)
                {
                    this.clientState_windowBorderRemoved = true;
                    Win32.Windows.WindowStyles ws = (Win32.Windows.WindowStyles)Win32.Windows.GetWindowLong(this.Settings.WindowHandle, Win32.Windows.GWL.GWL_STYLE);
                    Win32.Windows.WindowStyles wsNew = ws;
                    if (Win32.Windows.WindowStyles.WS_BORDER == (ws & Win32.Windows.WindowStyles.WS_BORDER))
                    {
                        wsNew ^= Win32.Windows.WindowStyles.WS_BORDER;
                    }
                    if (Win32.Windows.WindowStyles.WS_CAPTION == (ws & Win32.Windows.WindowStyles.WS_CAPTION))
                    {
                        wsNew ^= Win32.Windows.WindowStyles.WS_CAPTION;
                    }
                    if (Win32.Windows.WindowStyles.WS_EX_STATICEDGE == (ws & Win32.Windows.WindowStyles.WS_EX_STATICEDGE))
                    {
                        wsNew ^= Win32.Windows.WindowStyles.WS_EX_STATICEDGE;
                    }
                    if (Win32.Windows.WindowStyles.WS_SIZEBOX == (ws & Win32.Windows.WindowStyles.WS_SIZEBOX))
                    {
                        wsNew ^= Win32.Windows.WindowStyles.WS_SIZEBOX;
                    }
                    if (ws != wsNew)
                    {
                        Win32.Windows.SetWindowLong(this.Settings.WindowHandle, Win32.Windows.GWL.GWL_STYLE, (uint)wsNew);
                    }
                }
            }
            else
            {
                this.clientState_windowBorderRemoved = false;
            }
        }

        private void MonitorWindowPosition()
        {
            if (DateTime.Now.Ticks < RememberWindowPositionNextCheckTime)
            {
                return;
            }
            RememberWindowPositionNextCheckTime = DateTime.Now.AddSeconds(3).Ticks;

            if (this.Settings.RememberWindowPosition)
            {
                if (this.Settings.WindowHandle != IntPtr.Zero)
                {
                    SetGameWindowPosition(false, this.Settings.WindowPosition, this.Settings.WindowSize);
                }
                else
                {
                    clientState_windowPositionRestored = false;
                }
            }
        }

        private void MonitorNetworkClient()
        {
            if (DateTime.Now.Ticks < NetworkClientNextServerUpdateTime)
            {
                return;
            }
            NetworkClientNextServerUpdateTime = DateTime.Now.AddSeconds(9).Ticks;

            Mubox.Control.Network.Client networkClient = this.NetworkClient;
            if (networkClient != null)
            {
                networkClient.DisplayName = this.Settings.Name;
                networkClient.WindowStationHandle = this.WindowStationHandle;
                networkClient.WindowDesktopHandle = this.WindowDesktopHandle;
                networkClient.WindowHandle = this.Settings.WindowHandle;
                networkClient.SendClientConfig();
                networkClient.SendPerformanceInfo(this.GameProcess);
            }
        }

        private void MonitorMemoryMB()
        {
            if (DateTime.Now.Ticks < MemoryMBNextCheckTime)
            {
                return;
            }
            MemoryMBNextCheckTime = DateTime.Now.AddSeconds(60).Ticks;

            if (this.Settings.MemoryMB > 0)
            {
                if (this.GameProcess != null)
                {
                    try
                    {
                        this.GameProcess.MaxWorkingSet = new IntPtr(this.Settings.MemoryMB * 1024 * 1024);
                    }
                    catch (Exception ex)
                    {
                        Debug.WriteLine(ex.Message);
                        Debug.WriteLine(ex.StackTrace);
                    }
                }
            }
        }

        private System.Timers.Timer Timer { get; set; }

        #endregion

        private Mubox.Control.Network.Client _networkClient;
        public Mubox.Control.Network.Client NetworkClient
        {
            get { return _networkClient; }
            set
            {
                if (_networkClient != value)
                {
                    if (_networkClient != null)
                    {
                        _networkClient.ClientActivated -= _networkClient_ClientActivated;
                    }
                    if (value != null)
                    {
                        value.ClientActivated += _networkClient_ClientActivated;
                    }
                    _networkClient = value;
                }
            }
        }

        private void _networkClient_ClientActivated(object sender, EventArgs e)
        {
            Mubox.Configuration.MuboxConfigSection.Default.Teams.ActiveTeam.ActiveClient = Settings;
        }

        public override string ToString()
        {
            return (Settings.Name ?? "NULL").ToString();
        }

        public IntPtr WindowStationHandle { get; set; }

        public IntPtr WindowDesktopHandle { get; set; }

        public Window SettingsWindow { get; set; }

        private long MemoryMBNextCheckTime;
        private long RememberWindowPositionNextCheckTime;

        public void SetGameWindowPosition(bool force, Point position, Size size)
        {
            if (this.Settings.WindowHandle == IntPtr.Zero)
            {
                return;
            }

            bool windowPositionRestored = (force ? false : this.clientState_windowPositionRestored);

            Win32.Windows.RECT windowRect;
            if (!windowPositionRestored && (this.Settings.WindowSize.Width > 0))
            {
                Win32.Windows.GetWindowRect(this.Settings.WindowHandle, out windowRect);
                if (!force && ((windowRect.Top == position.Y) && (windowRect.Left == position.X) && (size.Width == (windowRect.Right - windowRect.Left)) && (size.Height == (windowRect.Bottom - windowRect.Top))))
                {
                    this.clientState_windowPositionRestored = true;
                }
                else
                {
                    Win32.Windows.SetWindowPos(this.Settings.WindowHandle, (IntPtr)Mubox.Win32.Windows.Position.HWND_TOP,
                        (int)position.X, (int)position.Y, (int)size.Width, (int)size.Height, (uint)(Win32.Windows.Options.SWP_ASYNCWINDOWPOS | Win32.Windows.Options.SWP_NOZORDER));
                }
            }
            else
            {
                if (Win32.Windows.GetWindowRect(this.Settings.WindowHandle, out windowRect))
                {
                    this.Settings.WindowPosition = new Point(windowRect.Left, windowRect.Top);
                    this.Settings.WindowSize = new Size(windowRect.Right - windowRect.Left, windowRect.Bottom - windowRect.Top);
                    Mubox.Configuration.MuboxConfigSection.Default.Save();
                }
            }
        }

        public System.Diagnostics.Process GameProcess { get; set; }

        private long GameProcessNextCheckTime;

        public event EventHandler<EventArgs> GameProcessExited;

        public event EventHandler<EventArgs> GameProcessFound;

        [SuppressMessage("Microsoft.Security", "CA2122")]
        public void MonitorGameProcess()
        {
            if (DateTime.Now.Ticks < GameProcessNextCheckTime)
            {
                return;
            }

            GameProcessNextCheckTime = DateTime.Now.AddSeconds(5).Ticks;

            Process gameProcess = GameProcess;
            if (gameProcess == null)
            {
                Debug.WriteLine("NoGameProcess for " + this.Settings.Name);
                return;
            }

            try
            {
                gameProcess.Refresh();

                if (gameProcess.HasExited)
                {
                    Debug.WriteLine("GameProcessExited for " + this.Settings.Name);
                    GameProcess = null;
                    Settings.WindowHandle = IntPtr.Zero;
                    if (NetworkClient != null)
                    {
                        NetworkClient.WindowHandle = Settings.WindowHandle;
                    }
                    if (GameProcessExited != null)
                    {
                        GameProcessExited(this, new EventArgs());
                    }
                }
                else
                {
                    IntPtr newWindowHandle = IntPtr.Zero;
                    if (gameProcess.Responding)
                    {
                        newWindowHandle = gameProcess.MainWindowHandle;
                        if (NetworkClient != null)
                        {
                            NetworkClient.WindowHandle = newWindowHandle;
                        }
                        bool newProcessWindow = Settings.WindowHandle != newWindowHandle;
                        if (newProcessWindow)
                        {
                            Debug.WriteLine("NewGameProcess for " + this.Settings.Name);
                            Settings.WindowHandle = newWindowHandle;
                            if (GameProcessFound != null)
                            {
                                GameProcessFound(this, new EventArgs());
                            }
                        }
                        if (gameProcess.PriorityClass != ProcessPriorityClass.Idle)
                        {
                            gameProcess.PriorityClass = ProcessPriorityClass.Idle;
                        }
                    }
                    else
                    {
                        Debug.WriteLine("GameProcessNotResponding for " + this.Settings.Name);
                    }
                    ManageProcessorAffinity(gameProcess);
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex.Message);
                Debug.WriteLine(ex.StackTrace);
            }
        }

        [SuppressMessage("Microsoft.Security", "CA2122")]
        public void ManageProcessorAffinity(Process process)
        {
            if (Settings.ProcessorAffinity == 0)
            {
                uint processorAffinity = 1;
                for (int i = 1; i < Environment.ProcessorCount; i++)
                {
                    processorAffinity |= (uint)(1 << i);
                }
                process.ProcessorAffinity = new IntPtr(processorAffinity);
            }
            else if (Settings.ProcessorAffinity == 1)
            {
                process.ProcessorAffinity = new IntPtr(1);
            }
            else
            {
                process.ProcessorAffinity = new IntPtr(1 << ((int)Settings.ProcessorAffinity - 1));
            }
        }

        private static string TryResolveApplicationPath(string defaultIfNotFound)
        {
            List<string> applicationPathPermutations = new List<string>();

            applicationPathPermutations.Add(@"Users\Public\Games\World of Warcraft\wow.exe".Replace('\\', System.IO.Path.DirectorySeparatorChar));
            applicationPathPermutations.Add(@"Program Files\World of Warcraft\wow.exe".Replace('\\', System.IO.Path.DirectorySeparatorChar));
            applicationPathPermutations.Add(@"Program Files (x86)\World of Warcraft\wow.exe".Replace('\\', System.IO.Path.DirectorySeparatorChar));
            applicationPathPermutations.Add(@"World of Warcraft\wow.exe".Replace('\\', System.IO.Path.DirectorySeparatorChar));
            applicationPathPermutations.Add(@"WoW\wow.exe".Replace('\\', System.IO.Path.DirectorySeparatorChar));
            applicationPathPermutations.Add(@"wow.exe".Replace('\\', System.IO.Path.DirectorySeparatorChar));

            for (int i = (int)'C'; i <= (int)'Z'; i++)
            {
                string rootFolder = ((char)i) + @":\".Replace('\\', System.IO.Path.DirectorySeparatorChar);
                if (System.IO.Directory.Exists(rootFolder))
                {
                    foreach (string applicationPath in applicationPathPermutations)
                    {
                        if (System.IO.File.Exists(rootFolder + applicationPath))
                        {
                            return rootFolder + applicationPath;
                        }
                    }
                }
            }

            return defaultIfNotFound;
        }

        private static string TryResolveIsolationPath(string defaultIfNotFound)
        {
            System.IO.DriveInfo mostLikelyDrive = null;
            System.IO.DriveInfo[] drives = System.IO.DriveInfo.GetDrives();

            foreach (System.IO.DriveInfo drive in drives)
            {
                try
                {
                    if (!drive.IsReady)
                    {
                        continue;
                    }
                    if (mostLikelyDrive == null || mostLikelyDrive.AvailableFreeSpace < drive.AvailableFreeSpace || defaultIfNotFound.StartsWith(drive.RootDirectory.FullName.Substring(0, 3)))
                    {
                        mostLikelyDrive = drive;
                    }
                }
                catch (Exception ex)
                {
                    Debug.WriteLine(ex.Message);
                    Debug.WriteLine(ex.StackTrace);
                }
            }

            return mostLikelyDrive == null
                ? defaultIfNotFound
                : mostLikelyDrive.RootDirectory.FullName;
        }

        #region IDisposable Members

        ~ClientState()
        {
            Dispose(false);
        }

        public void Dispose()
        {
            Dispose(true);
        }

        [SuppressMessage("Microsoft.Security", "CA2122")]
        private void Dispose(bool disposing)
        {
            if (disposing)
            {
                GC.SuppressFinalize(this);
            }
            if (Timer != null)
            {
                try
                {
                    Timer.Stop();
                }
                catch (Exception ex)
                {
                    Debug.WriteLine(ex.Message);
                    Debug.WriteLine(ex.StackTrace);
                }
                Timer = null;
            }
            if (this.GameProcess != null)
            {
                // TODO: killing game process should only be performed when Mubox launched the process
                try
                {
                    this.GameProcess.CloseMainWindow();
                    if (!this.GameProcess.WaitForExit(5000)) // TODO: arbitrary, and it's unclear why some games refuse to process WM_CLOSE properly
                    {
                        if (!this.GameProcess.HasExited)
                        {
                            this.GameProcess.Kill();
                        }
                    }
                }
                catch (Exception ex)
                {
                    Debug.WriteLine(ex.Message);
                    Debug.WriteLine(ex.StackTrace);
                }
                this.GameProcess = null;
            }
            if (this.NetworkClient != null)
            {
                try
                {
                    this.NetworkClient.Disconnect();
                }
                catch (Exception ex)
                {
                    Debug.WriteLine(ex.Message);
                    Debug.WriteLine(ex.StackTrace);
                }
                this.NetworkClient = null;
            }
            if (this.SettingsWindow != null)
            {
                this.SettingsWindow = null;
            }
        }

        #endregion
    }
}