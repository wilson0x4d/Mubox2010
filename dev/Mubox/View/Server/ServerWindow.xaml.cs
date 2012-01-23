using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using Mubox.Configuration;
using Mubox.Model;
using Mubox.Model.Client;
using Mubox.Model.Input;

// TODO the horrible naming conventions, such as "Mouse_AbsoluteMovement_Screen_Resolution_UpdateTimestampTicks" represent code that should be factored into domain objects.

namespace Mubox.View.Server
{
    /// <summary>
    /// Interaction logic for ServerWindow.xaml
    /// </summary>
    public partial class ServerWindow : Window
    {
        public static ServerWindow Instance { get; set; }

        internal double Mouse_RelativeMovement_LastX = double.MinValue, RelativeMovement_LastY = double.MinValue;

        internal long Mouse_AbsoluteMovement_Screen_Resolution_UpdateTimestampTicks;
        internal int Mouse_AbsoluteMovement_Screen_ResolutionX = Win32.SystemMetrics.GetSystemMetrics(Win32.SystemMetrics.SM.SM_CXSCREEN);
        internal int Mouse_AbsoluteMovement_Screen_ResolutionY = Win32.SystemMetrics.GetSystemMetrics(Win32.SystemMetrics.SM.SM_CYSCREEN);

        public ClientBase ActiveClient { get; private set; }

        public ServerWindow()
        {
            InitializeComponent();
            Mubox.Control.Network.Server.ClientAccepted += Server_ClientAccepted;
            Mubox.Control.Network.Server.ClientRemoved += Server_ClientRemoved;
            Keyboard_ActionMap = Keyboard_ActionMap_CreateNew((e) => e.Handled);
            Keyboard_ActionMap_SetDefaultActions(Keyboard_ActionMap);
        }

        private Point L_e_Point = new Point(0, 0);
        private ClientBase[] clientsCached = null;
        private int[/*vk*/] roundRobinTable = new int[ushort.MaxValue];
        private Dictionary<Win32.WM, ButtonState> Mouse_Buttons = InitializeMouseButtons();

        private static Dictionary<Win32.WM, ButtonState> InitializeMouseButtons()
        {
            Dictionary<Win32.WM, ButtonState> buttons = new Dictionary<Win32.WM, ButtonState>();
            foreach (var button in new Win32.WM[]
                {
                    Win32.WM.LBUTTONDOWN,
                    Win32.WM.RBUTTONDOWN,
                    Win32.WM.MBUTTONDOWN,
                    Win32.WM.XBUTTONDOWN,
                    //Win32.WM.LBUTTONDBLCLK,
                    //Win32.WM.RBUTTONDBLCLK,
                    //Win32.WM.MBUTTONDBLCLK,
                    //Win32.WM.XBUTTONDBLCLK,
                    Win32.WM.LBUTTONUP,
                    Win32.WM.RBUTTONUP,
                    Win32.WM.MBUTTONUP,
                    Win32.WM.XBUTTONUP
                })
            {
                buttons[button] = new ButtonState
                {
                    LastDownTimestamp = DateTime.MinValue,
                    LastUpTimestamp = DateTime.MinValue
                };
            }
            return buttons;
        }

        private void MouseInputHook_MouseInputReceived(MouseInput e)
        {
            if (!Mubox.Configuration.MuboxConfigSection.Default.EnableMouseCapture)
            {
                e.Handled = false;
                return;
            }

            ClientBase activeClient = ActiveClient;

            #region Mouse_RelativeMovement

            // one-time init
            if (this.Mouse_RelativeMovement_LastX == double.MinValue)
            {
                this.Mouse_RelativeMovement_LastX = e.Point.X;
                this.RelativeMovement_LastY = e.Point.Y;
                return;
            }

            // update for resolution changes every XX seconds
            if ((DateTime.Now.Ticks - Mouse_AbsoluteMovement_Screen_Resolution_UpdateTimestampTicks) > TimeSpan.FromSeconds(15).Ticks)
            {
                Mouse_Screen_OnResolutionChanged();
            }

            // track relative movement
            // TODO: except when the mouse moves TO the center of the screen or center of client area, as wow does this to pan the screen
            // TODO: except when the mouse moves FROM the center of the screen or center of client area TO the last location we moved BEFORE 'detecting a center screen mouse move'
            // TODO: mouseglitch
            int relX = (int)e.Point.X;
            int relY = (int)e.Point.Y;

            var shouldCloneMouse = ShouldCloneMouse(e);

            relX -= (int)this.Mouse_RelativeMovement_LastX;
            relY -= (int)this.RelativeMovement_LastY;

            // if a mousemove, track relative change
            if (e.WM == Win32.WM.MOUSEMOVE)
            {
                this.Mouse_RelativeMovement_LastX += relX;
                if (this.Mouse_RelativeMovement_LastX <= 0)
                    this.Mouse_RelativeMovement_LastX = 0;
                else if (this.Mouse_RelativeMovement_LastX >= Mouse_AbsoluteMovement_Screen_ResolutionX)
                    this.Mouse_RelativeMovement_LastX = Mouse_AbsoluteMovement_Screen_ResolutionX;
                this.RelativeMovement_LastY += relY;
                if (this.RelativeMovement_LastY <= 0)
                    this.RelativeMovement_LastY = 0;
                else if (this.RelativeMovement_LastY >= Mouse_AbsoluteMovement_Screen_ResolutionY)
                    this.RelativeMovement_LastY = Mouse_AbsoluteMovement_Screen_ResolutionY;
            }

            #endregion
            #region 'Mouse Buffering' Option

            ButtonState mouseButtonInfo = null;
            if (Mouse_Buttons.TryGetValue(e.WM, out mouseButtonInfo))
            {
                switch (e.WM)
                {
                    case Win32.WM.LBUTTONDOWN:
                    case Win32.WM.RBUTTONDOWN:
                    case Win32.WM.MBUTTONDOWN:
                    case Win32.WM.XBUTTONDOWN:
                        if (!mouseButtonInfo.IsDown)
                        {
                            mouseButtonInfo.LastGestureWasClick = mouseButtonInfo.IsClick;
                            mouseButtonInfo.LastDownTimestamp = DateTime.Now;
                        }
                        break;
                    case Win32.WM.LBUTTONUP:
                    case Win32.WM.RBUTTONUP:
                    case Win32.WM.MBUTTONUP:
                    case Win32.WM.XBUTTONUP:
                        if (mouseButtonInfo.IsDown)
                        {
                            mouseButtonInfo.LastUpTimestamp = DateTime.Now;
                        }
                        break;
                }
                e.IsClickEvent = mouseButtonInfo.IsClick;
            }

            #endregion

            // send to client
            if (Mubox.Configuration.MuboxConfigSection.Default.IsCaptureEnabled && (activeClient != null))
            {
                //if (!shouldCloneMouse)
                //{
                //    e.Handled = !(activeClient.IsLocalAddress && (e.WM == Win32.WM.MOUSEMOVE));
                //    if (!activeClient.IsLocalAddress || e.WM != Win32.WM.MOUSEMOVE)
                //    {
                //        e.Point = new Point(relX, relY);
                //        e.WindowStationHandle = activeClient.WindowStationHandle;
                //        e.WindowHandle = activeClient.WindowHandle;
                //        activeClient.Dispatch(e);
                //    }
                //}
                //else
                {
                    e.Handled = (e.WM != Win32.WM.MOUSEMOVE);

                    ClientBase[] clients = shouldCloneMouse
                        ? GetCachedClients()
                        : new[] { activeClient };

                    if (e.WM == Win32.WM.MOUSEMOVE)
                    {
                        // track mouse position
                        TrackMousePositionClientRelative(e, activeClient, clients);

                        if (mouseButtonInfo != null)
                        {
                            var lastMouseDownTimestampExpiry = mouseButtonInfo.LastDownTimestamp.AddMilliseconds(Mubox.Configuration.MuboxConfigSection.Default.MouseBufferMilliseconds);
                            if (e.CreatedTime.Ticks < lastMouseDownTimestampExpiry.Ticks) // TODO: this logic seems incorrect, this seems like a hacky attempt to hide 'mouse cursor centering' performed by games when panning views. this is the only place it is used
                            {
                                clients = new ClientBase[0];
                            }
                            e.Handled = e.Handled || (mouseButtonInfo.IsDown && lastMouseDownTimestampExpiry.Ticks > DateTime.Now.Ticks);
                        }

                        if (clients.Length > 0)
                        {
                            List<ClientBase> reducedClientsForMouseMove = new List<ClientBase>();
                            List<string> addressExclusionTable = new List<string>(ClientBase.localAddressTable);
                            foreach (var item in clients)
                            {
                                if (!addressExclusionTable.Contains(item.Address))
                                {
                                    reducedClientsForMouseMove.Add(item);
                                    addressExclusionTable.Add(item.Address);
                                }
                            }
                            clients = reducedClientsForMouseMove.ToArray();
                        }
                    }

                    foreach (ClientBase client in clients)
                    {
                        ForwardMouseEvent(e, client);
                    }
                }
            }
        }

        private void TrackMousePositionClientRelative(MouseInput e, ClientBase activeClient, ClientBase[] clients)
        {
            ClientBase clientForCoordinateNormalization = activeClient;
            if (clientForCoordinateNormalization == null || !clientForCoordinateNormalization.IsLocalAddress || !IsMouseInClientArea(e, GetScreenRelativeClientRect(clientForCoordinateNormalization)))
            {
                // look for a local client which the mouse coordinates "belong to" and re-assign "clientForCoordinateNormalization"
                clientForCoordinateNormalization = null;
                foreach (var item in clients)
                {
                    if (item.IsLocalAddress && IsMouseInClientArea(e, GetScreenRelativeClientRect(item)))
                    {
                        clientForCoordinateNormalization = item;
                        break;
                    }
                }
            }
            if (clientForCoordinateNormalization != null)
            {
                // track client-relative position
                this.L_e_Point = new Point(Math.Ceiling((double)(e.Point.X - clientForCoordinateNormalization.CachedScreenFromClientRect.Left) * (65536.0 / (double)clientForCoordinateNormalization.CachedScreenFromClientRect.Width)), Math.Ceiling((double)(e.Point.Y - clientForCoordinateNormalization.CachedScreenFromClientRect.Top) * (65536.0 / (double)clientForCoordinateNormalization.CachedScreenFromClientRect.Height)));
            }
        }

        private static bool IsMouseInClientArea(MouseInput e, Win32.Windows.RECT calcRect)
        {
            return (calcRect.Left <= (int)e.Point.X && calcRect.Top <= (int)e.Point.Y) && (calcRect.Right >= (int)e.Point.X && calcRect.Bottom >= (int)e.Point.Y);
        }

        private static Win32.Windows.RECT GetScreenRelativeClientRect(ClientBase item)
        {
            if (item.CachedScreenFromClientRectExpiry.Ticks < DateTime.Now.Ticks)
            {
                Win32.Windows.RECT calcRect;
                Win32.Windows.RECT clientRect;
                Win32.Windows.GetWindowRect(item.WindowHandle, out calcRect);
                Win32.Windows.GetClientRect(item.WindowHandle, out clientRect);
                int borderOffset = ((calcRect.Width - clientRect.Width) / 2);
                calcRect.Left += borderOffset;
                calcRect.Bottom -= borderOffset;
                calcRect.Top = calcRect.Bottom - clientRect.Height;
                item.CachedScreenFromClientRect = calcRect;
                item.CachedScreenFromClientRectExpiry = DateTime.Now.AddSeconds(3);
            }
            return item.CachedScreenFromClientRect;
        }

        private ClientBase[] GetCachedClients()
        {
            ClientBase[] result = clientsCached;
            if (result == null)
            {
                this.Dispatcher.Invoke((Action)delegate()
                {
                    clientsCached = ClientWindowProvider.ToArray();
                    result = clientsCached;
                });
            }
            return result;
        }

        private int thisCenterX;
        private int thisCenterWindowX;
        private int thisCenterClientAreaX;

        private int thisCenterY;
        private int thisCenterWindowY;
        private int thisCenterClientAreaY;

        private void Mouse_Screen_OnResolutionChanged()
        {
            Mouse_AbsoluteMovement_Screen_ResolutionX = Win32.SystemMetrics.GetSystemMetrics(Win32.SystemMetrics.SM.SM_CXSCREEN);
            Mouse_AbsoluteMovement_Screen_ResolutionY = Win32.SystemMetrics.GetSystemMetrics(Win32.SystemMetrics.SM.SM_CYSCREEN);
            // center server window on screen
            this.Dispatcher.BeginInvoke((Action)delegate()
            {
                double thisLeft = ((double)this.Mouse_AbsoluteMovement_Screen_ResolutionX - this.Width) / 2;
                double thisTop = ((double)this.Mouse_AbsoluteMovement_Screen_ResolutionY - this.Height) / 2;

                thisCenterX = Mouse_AbsoluteMovement_Screen_ResolutionX / 2;
                thisCenterWindowX = (int)this.Width / 2;
                thisCenterClientAreaX = thisCenterWindowX; // TODO: compensate for window border, if not hidden

                thisCenterY = Mouse_AbsoluteMovement_Screen_ResolutionY / 2;
                thisCenterWindowY = (int)this.Height / 2;
                thisCenterClientAreaY = thisCenterWindowX; // TODO: compensate for window border and title/caption, if not hidden

                if (this.Left != thisLeft || this.Top != thisTop)
                {
                    this.Left = thisLeft;
                    this.Top = thisTop;
                }
            });
            Mouse_AbsoluteMovement_Screen_Resolution_UpdateTimestampTicks = DateTime.Now.Ticks;
        }

        private static bool ShouldCloneMouse(MouseInput e)
        {
            return
                (
                    ((Mubox.Configuration.MuboxConfigSection.Default.MouseCloneMode == MouseCloneModeType.Toggled) && Win32.IsToggled(Win32.VK.Capital))
                    ||
                    ((Mubox.Configuration.MuboxConfigSection.Default.MouseCloneMode == MouseCloneModeType.Pressed) && Win32.IsPressed(Win32.VK.Capital))
                );
        }

        private void ForwardMouseEvent(MouseInput e, ClientBase clientBase)
        {
            MouseInput le = new MouseInput();
            le.IsAbsolute = true;
            le.MouseData = e.MouseData;
            le.Point = L_e_Point;
            le.Time = e.Time;
            le.WindowDesktopHandle = clientBase.WindowDesktopHandle;
            le.WindowStationHandle = clientBase.WindowStationHandle;
            le.WindowHandle = clientBase.WindowHandle;
            le.WM = e.WM;
            clientBase.Dispatch(le);
        }

        private bool isSwitchingClients;

        private static bool SetMulticastOneTime = true;

        public void CoerceActivation(IntPtr targetClientWindowHandle)
        {
            this.Dispatcher.Invoke((Action)delegate()
            {
                try
                {
                    for (int i = 0; i < listBox1.Items.Count; i++)
                    {
                        ClientBase client = (listBox1.Items[i] as ClientBase);
                        if (client.WindowHandle == targetClientWindowHandle)
                        {
                            listBox1.SelectedIndex = i;
                            ActivateSelectedClientAndRefreshView();
                        }
                    }
                }
                catch (Exception ex)
                {
                    Debug.WriteLine(ex.Message);
                    Debug.WriteLine(ex.StackTrace);
                }
            });
        }

        private void RefreshClientView()
        {
            CollectionViewSource view = this.FindResource("ClientListViewSource") as CollectionViewSource;
            if (view != null)
            {
                view.View.Refresh();
            }
        }

        public Dictionary<Mubox.Win32.VK, Dictionary<Mubox.Win32.WM, Func<KeyboardInput, bool>>> Keyboard_ActionMap { get; set; }

        private Func<KeyboardInput, bool> Keyboard_DefaultAction = null;

        private Dictionary<Win32.VK, Dictionary<Mubox.Win32.WM, Func<KeyboardInput, bool>>> Keyboard_ActionMap_CreateNew(Func<KeyboardInput, bool> keyboardAction)
        {
            Dictionary<Win32.VK, Dictionary<Win32.WM, Func<KeyboardInput, bool>>> actionMap = new Dictionary<Win32.VK, Dictionary<Win32.WM, Func<KeyboardInput, bool>>>();
            foreach (Win32.VK vk in Enum.GetValues(typeof(Win32.VK)))
            {
                actionMap[vk] = new Dictionary<Win32.WM, Func<KeyboardInput, bool>>();
            }

            // set default keyboard action
            this.Keyboard_DefaultAction = keyboardAction;

            return actionMap;
        }

        private void Keyboard_ActionMap_SetDefaultActions(Dictionary<Win32.VK, Dictionary<Win32.WM, Func<KeyboardInput, bool>>> actionMap)
        {
            // toggle input capture
            actionMap[Win32.VK.ScrollLock] = actionMap[Win32.VK.NumLock];
            actionMap[Win32.VK.NumLock][Win32.WM.KEYUP] = (e) =>
            {
                SetInputCapture(!Mubox.Configuration.MuboxConfigSection.Default.IsCaptureEnabled, (Win32.VK)e.VK == Win32.VK.NumLock);
                return false;
            };

            // accept switched-to client
            actionMap[Win32.VK.LeftMenu] = actionMap[Win32.VK.Menu];
            actionMap[Win32.VK.RightMenu] = actionMap[Win32.VK.Menu];
            actionMap[Win32.VK.Menu][Win32.WM.KEYUP] = (e) =>
            {
                if (Mubox.Configuration.MuboxConfigSection.Default.DisableAltTabHook)
                {
                    if (isSwitchingClients)
                    {
                        isSwitchingClients = false;
                    }
                    e.Handled = false;
                    return true;
                }
                if (isSwitchingClients && Mubox.Configuration.MuboxConfigSection.Default.IsCaptureEnabled)
                {
                    ClientBase activeClient = this.ActiveClient;

                    //ClientBase[] clients = null;
                    if (isSwitchingClients)
                    {
                        isSwitchingClients = false;

                        // deactivate any active clients
                        this.Dispatcher.BeginInvoke((Action)delegate()
                        {
                            ActivateSelectedClientAndRefreshView();
                        });
                    }
                }
                return false;
            };

            // replace windows task-switcher
            actionMap[Win32.VK.Tab][Win32.WM.SYSKEYDOWN] = (e) =>
            {
                if (Mubox.Configuration.MuboxConfigSection.Default.DisableAltTabHook)
                {
                    if (isSwitchingClients)
                    {
                        isSwitchingClients = false;
                    }
                    e.Handled = false;
                    return true;
                }
                if (Mubox.Configuration.MuboxConfigSection.Default.IsCaptureEnabled)
                {
                    e.Handled = true;
                    this.Dispatcher.BeginInvoke((Action)delegate()
                    {
                        if (!isSwitchingClients)
                        {
                            isSwitchingClients = true;
                            if (!this.IsVisible)
                            {
                                this.Show();
                                this.Topmost = true;
                                Debug.WriteLine("Attempted to Show Switcher");
                            }
                        }
                        else
                        {
                            if (this.IsVisible)
                            {
                                this.Topmost = true;
                            }
                        }
                        bool reverseSelection = Mubox.Configuration.MuboxConfigSection.Default.ReverseClientSwitching ? !Win32.IsPressed(Win32.VK.Shift) : Win32.IsPressed(Win32.VK.Shift);
                        if (reverseSelection)
                        {
                            // reverse-selection
                            if (listBox1.SelectedIndex - 1 <= -1)
                            {
                                if (listBox1.Items.Count > 0)
                                {
                                    listBox1.SelectedIndex = listBox1.Items.Count - 1;
                                }
                            }
                            else
                            {
                                listBox1.SelectedIndex = listBox1.SelectedIndex - 1;
                            }
                        }
                        else
                        {
                            // forward-selection
                            if (listBox1.SelectedIndex + 1 >= listBox1.Items.Count)
                            {
                                if (listBox1.Items.Count > 0)
                                {
                                    listBox1.SelectedIndex = 0;
                                }
                            }
                            else
                            {
                                listBox1.SelectedIndex = listBox1.SelectedIndex + 1;
                            }
                        }
                    });
                }
                return false;
            };

            actionMap[Win32.VK.Capital][Win32.WM.KEYDOWN] = (e) =>
            {
                // work around for mouse hook breakage under W7, e.g. resetting the mouse hook
                Control.Input.MouseInputHook.Stop();
                Control.Input.MouseInputHook.Start();
                e.Handled = false;
                return true;
            };
            actionMap[Win32.VK.Capital][Win32.WM.KEYUP] = (e) =>
            {
                e.Handled = false;
                return true;
            };
        }

        private void ActivateSelectedClientAndRefreshView()
        {
            try
            {
                ClientBase client = listBox1.SelectedItem as ClientBase;
                ActiveClient = client;
                if (client != null)
                {
                    client.Activate();
                    RefreshClientView();
                }
                else
                {
                    this.Hide();
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex.Message);
                Debug.WriteLine(ex.StackTrace);
            }
        }

        private void KeyboardInputHook_KeyboardInputReceived(KeyboardInput e)
        {
            if (this.Keyboard_DefaultAction != null)
            {
                if (this.Keyboard_DefaultAction(e) || e.Handled)
                {
                    return;
                }
            }

            Func<KeyboardInput, bool> action = null;
            if (Keyboard_ActionMap[(Win32.VK)e.VK].TryGetValue(e.WM, out action))
            {
                if (action(e) || e.Handled)
                {
                    return;
                }
            }

            if (!Mubox.Configuration.MuboxConfigSection.Default.IsCaptureEnabled)
            {
                return;
            }

            ClientBase activeClient = this.ActiveClient;

            if (Mubox.Configuration.MuboxConfigSection.Default.EnableMulticast && !ActiveClientOnly(e))
            {
                e.Handled = true;

                ClientBase[] clients = GetCachedClients();

                KeySetting globalKeySetting;
                if (MuboxConfigSection.Default.Keys.TryGetKeySetting((Win32.VK)e.VK, out globalKeySetting))
                {
                    if (globalKeySetting.RoundRobinKey)
                    {
                        if (clients.Length > 0)
                        {
                            int clientIndex = roundRobinTable[(int)e.VK] + 1;
                            ClientBase client = clients[(clientIndex + 1) % clients.Length];
                            roundRobinTable[(int)e.VK] = (byte)clientIndex;
                            InputToClient(e, client);
                            return;
                        }
                    }
                }

                foreach (ClientBase client in clients)
                {
                    InputToClient(e, client);
                }
            }
            else if (activeClient != null)
            {
                e.Handled = true;
                ForwardEventToClient(e, activeClient);
            }
        }

        private static void InputToClient(KeyboardInput e, ClientBase client)
        {
            // this method basically applies CAS settings and calls ForwardEventToClient
            byte cas = 0;
            var clientSettings = Mubox.Configuration.MuboxConfigSection.Default.Teams.ActiveTeam.ActiveClient;
            if (clientSettings != null)
            {
                if (clientSettings.Name.Equals(client.DisplayName, StringComparison.InvariantCultureIgnoreCase))
                {
                    Mubox.Configuration.KeySetting keySetting;
                    if (clientSettings.Keys.TryGetKeySetting((Win32.VK)e.VK, out keySetting) && keySetting.EnableNoModActiveClient)
                    {
                        cas = (byte)e.CAS;
                        e.CAS = (Win32.CAS)0;
                    }
                }
            }
            ForwardEventToClient(e, client);
            if (cas != 0)
            {
                e.CAS = (Win32.CAS)cas;
            }
        }

        private static bool ActiveClientOnly(KeyboardInput e)
        {
            // TODO optimize !!! iterating 'all keys' looking for a match is a waste of CPU, perhaps a bitmap of Win32.VK makes sense, but it needs to be synchronized against the config (perhaps on Save)
            bool activeClientOnly = false;
            foreach (var item in Mubox.Configuration.MuboxConfigSection.Default.Keys.Cast<Mubox.Configuration.KeySetting>())
            {
                if (item.ActiveClientOnly && (Win32.VK)e.VK == item.InputKey)
                {
                    activeClientOnly = true;
                    break;
                }
            }
            return activeClientOnly;
        }

        private static void ForwardEventToClient(KeyboardInput e, ClientBase clientBase)
        {
            try
            {
                e.WindowStationHandle = clientBase.WindowStationHandle;
                e.WindowHandle = clientBase.WindowHandle;
                clientBase.Dispatch(e);
            }
            catch (Exception ex)
            {
                Mubox.Control.Network.Server.RemoveClient(clientBase);
                Debug.WriteLine(ex.Message);
                Debug.WriteLine(ex.StackTrace);
            }
        }

        private void TryAutoEnableMulticastOnce()
        {
            if (SetMulticastOneTime)
            {
                if (!Mubox.Configuration.MuboxConfigSection.Default.EnableMulticast)
                {
                    Mubox.Configuration.MuboxConfigSection.Default.EnableMulticast = true;
                    Mubox.Configuration.MuboxConfigSection.Default.Save();
                }
                SetMulticastOneTime = false;
            }
        }

        public void SetInputCapture(bool enable, bool showServerUI)
        {
            if (enable)
            {
                TryAutoEnableMulticastOnce();
                if (!Mubox.Configuration.MuboxConfigSection.Default.IsCaptureEnabled)
                {
                    Mubox.Configuration.MuboxConfigSection.Default.IsCaptureEnabled = true;
                    this.Dispatcher.Invoke((Action)delegate()
                    {
                        try
                        {
                            ActiveClient = listBox1.SelectedItem as ClientBase;
                            if (ActiveClient != null)
                            {
                                ActiveClient.Activate();
                            }
                            else
                            {
                                this.Hide();
                            }
                        }
                        catch (Exception ex)
                        {
                            Debug.WriteLine(ex.Message);
                            Debug.WriteLine(ex.StackTrace);
                        }
                    });
                }
            }
            else
            {
                if (Mubox.Configuration.MuboxConfigSection.Default.IsCaptureEnabled)
                {
                    Mubox.Configuration.MuboxConfigSection.Default.IsCaptureEnabled = false;
                    if (showServerUI)
                    {
                        this.Dispatcher.Invoke((Action)delegate()
                        {
                            this.Show();
                            this.Topmost = true;
                        });
                    }
                }
            }
        }

        public void ToggleInputCapture(bool showServerUI)
        {
            SetInputCapture(!Mubox.Configuration.MuboxConfigSection.Default.IsCaptureEnabled, showServerUI);
        }

        #region ClientWindowProvider

        /// <summary>
        /// ClientWindowProvider Dependency Property
        /// </summary>
        public static readonly DependencyProperty ClientWindowProviderProperty =
            DependencyProperty.Register("ClientWindowProvider", typeof(ObservableCollection<ClientBase>), typeof(ServerWindow),
                new FrameworkPropertyMetadata((ObservableCollection<ClientBase>)new ObservableCollection<ClientBase>()));

        /// <summary>
        /// Gets or sets the ClientWindowProvider property.  This dependency property
        /// indicates a collection of ClientBase objects visible in the UI.
        /// </summary>
        public ObservableCollection<ClientBase> ClientWindowProvider
        {
            get { return (ObservableCollection<ClientBase>)GetValue(ClientWindowProviderProperty); }
            set { SetValue(ClientWindowProviderProperty, value); }
        }

        #endregion

        private void Server_ClientAccepted(object sender, Mubox.Control.Network.Server.ServerEventArgs e)
        {
            this.Dispatcher.BeginInvoke((Action<Mubox.Control.Network.Server.ServerEventArgs>)delegate(Mubox.Control.Network.Server.ServerEventArgs args)
            {
                (args.Client as NetworkClient).ClientActivated += ServerWindow_ClientActivated;
                ClientWindowProvider.Add(args.Client);
            }, e);
            if (e.Client is NetworkClient)
            {
                (e.Client as NetworkClient).CoerceActivation += new EventHandler<EventArgs>(ServerWindow_CoerceActivation);
            }
            e.Client.IsAttachedChanged += Client_IsAttachedChanged;
            clientsCached = null;
        }

        private void ServerWindow_ClientActivated(object sender, EventArgs e)
        {
            this.Dispatcher.BeginInvoke((Action)delegate()
            {
                // set "ActiveClient" of MuboxConfig, primarily for CAS Modifiers feature
                NetworkClient networkClient = sender as NetworkClient;
                if (networkClient != null && !string.IsNullOrEmpty(networkClient.DisplayName))
                {
                    Mubox.Configuration.ClientSettings settings = Mubox.Configuration.MuboxConfigSection.Default.Teams.ActiveTeam.Clients.GetOrCreateNew(networkClient.DisplayName);
                    if (settings != null)
                    {
                        Mubox.Configuration.MuboxConfigSection.Default.Teams.ActiveTeam.ActiveClient = settings;
                    }
                }

                this.Hide();
            });
        }

        private void Server_ClientRemoved(object sender, Mubox.Control.Network.Server.ServerEventArgs e)
        {
            this.Dispatcher.BeginInvoke((Action<Mubox.Control.Network.Server.ServerEventArgs>)delegate(Mubox.Control.Network.Server.ServerEventArgs args)
            {
                ClientWindowProvider.Remove(args.Client);
                RefreshClientView();
            }, e);
            if (e.Client is NetworkClient)
            {
                (e.Client as NetworkClient).CoerceActivation -= ServerWindow_CoerceActivation;
            }
            e.Client.IsAttachedChanged -= Client_IsAttachedChanged;
            clientsCached = null;
        }

        private void ServerWindow_CoerceActivation(object sender, EventArgs e)
        {
            this.CoerceActivation((sender as ClientBase).WindowHandle);
        }

        private void Client_IsAttachedChanged(object sender, EventArgs e)
        {
            ClientBase client = sender as ClientBase;
            if (client != null)
            {
                if (!client.IsAttached)
                {
                    this.Dispatcher.BeginInvoke((Action)delegate()
                    {
                        this.ClientWindowProvider.Remove(client);
                    });
                    if (client is NetworkClient)
                    {
                        (client as NetworkClient).CoerceActivation -= ServerWindow_CoerceActivation;
                    }
                }
            }
            clientsCached = null;
        }

        private void buttonStartServer_Click(object sender, RoutedEventArgs e)
        {
            string[] portNumberList = textListenPort.Text.Trim().Split(',');
            if ((string)buttonStartServer.Content == "Start")
            {
                buttonStartServer.Content = "Stop";
                try
                {
                    int portNumber = int.Parse(portNumberList[0]);
                    Mubox.Control.Network.Server.Start(portNumber);
                    textListenPort.IsEnabled = false;
                }
                catch (Exception ex)
                {
                    try
                    {
                        Window connectionDialog = Activator.CreateInstance(portNumberList[0], portNumberList[1]).Unwrap() as Window;
                        connectionDialog.Tag = portNumberList;
                        connectionDialog.Show();
                        textListenPort.IsEnabled = false;
                    }
                    catch
                    {
                        MessageBox.Show("Bad Port Number: " + ex.Message, "Mubox Server", MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                }
            }
            else
            {
                buttonStartServer.Content = "Start";
                Mubox.Control.Network.Server.Stop();
                textListenPort.IsEnabled = true;
            }
        }

        protected override void OnClosing(System.ComponentModel.CancelEventArgs e)
        {
            Mubox.Control.Input.KeyboardInputHook.KeyboardInputReceived -= KeyboardInputHook_KeyboardInputReceived;
            Mubox.Control.Input.MouseInputHook.MouseInputReceived -= MouseInputHook_MouseInputReceived;

            Mubox.Control.Input.KeyboardInputHook.Stop();
            Mubox.Control.Input.MouseInputHook.Stop();

            Mubox.Control.Network.Server.Stop();

            ClientBase[] clients = GetCachedClients();
            this.Dispatcher.Invoke((Action)delegate()
            {
                ClientWindowProvider.Clear();
            });

            foreach (ClientBase client in clients)
            {
                NetworkClient networkClient = client as NetworkClient;
                if (networkClient != null)
                {
                    try
                    {
                        networkClient.Detach();
                    }
                    catch (Exception ex)
                    {
                        Debug.WriteLine(ex.Message);
                        Debug.WriteLine(ex.StackTrace);
                    }
                }
            }

            base.OnClosing(e);
        }

        protected override void OnInitialized(EventArgs e)
        {
            base.OnInitialized(e);

            InitializeThemes();
        }

        private void InitializeThemes()
        {
            List<Mubox.View.Themes.ThemeDescriptor> themesList = new List<Mubox.View.Themes.ThemeDescriptor>();
            AddThemeResource(themesList, "Default");
            AddThemeResource(themesList, "Black");
            AddThemeResource(themesList, "Orange");
            AddThemeResource(themesList, "Brown");
            AddThemeResource(themesList, "Red");
            AddThemeResource(themesList, "Green");
            AddThemeResource(themesList, "Blue");
            AddThemeResource(themesList, "ExtraLife");
            comboThemeSelector.SelectionChanged += (L_sender, L_e) =>
            {
                Mubox.View.Themes.ThemeDescriptor theme = (comboThemeSelector.SelectedItem as Mubox.View.Themes.ThemeDescriptor);
                if (Application.Current.Resources.MergedDictionaries.Count > 0)
                {
                    Application.Current.Resources.MergedDictionaries.Clear();
                }
                if (theme != null)
                {
                    Application.Current.Resources.MergedDictionaries.Add(theme.Resources);
                    Mubox.Configuration.MuboxConfigSection.Default.PreferredTheme = theme.Name;
                }
                else
                {
                    Mubox.Configuration.MuboxConfigSection.Default.PreferredTheme = "Default";
                }
                Mubox.Configuration.MuboxConfigSection.Default.Save();
            };
            comboThemeSelector.ItemsSource = themesList;
            comboThemeSelector.DisplayMemberPath = "Name";
        }

        private void AddThemeResource(List<Mubox.View.Themes.ThemeDescriptor> themesList, string themeName)
        {
            try
            {
                var uri = new Uri("pack://siteoforigin:,,,/View/Themes/" + themeName + ".xaml", UriKind.Absolute);
                var resourceInfo = Application.GetRemoteStream(uri);
                var reader = new System.Windows.Markup.XamlReader();
                ResourceDictionary resourceDictionary = (ResourceDictionary)reader.LoadAsync(resourceInfo.Stream);
                Mubox.View.Themes.ThemeDescriptor theme = new Mubox.View.Themes.ThemeDescriptor
                {
                    Name = themeName,
                    Resources = resourceDictionary
                };
                themesList.Add(theme);
                if (Mubox.Configuration.MuboxConfigSection.Default.PreferredTheme == themeName)
                {
                    Application.Current.Resources.MergedDictionaries.Add(theme.Resources);
                    comboThemeSelector.SelectedItem = theme;
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex.Message);
                Debug.WriteLine(ex.StackTrace);
            }
        }

        private bool isStartedYet;

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            this.Hide();
            this.Dispatcher.BeginInvoke((Action)delegate()
            {
                try
                {
                    if (isStartedYet)
                        return;
                    isStartedYet = true;

                    Mubox.Control.Input.KeyboardInputHook.KeyboardInputReceived += KeyboardInputHook_KeyboardInputReceived;
                    Mubox.Control.Input.MouseInputHook.MouseInputReceived += MouseInputHook_MouseInputReceived;
                    Mubox.Control.Input.KeyboardInputHook.Start();
                    Mubox.Control.Input.MouseInputHook.Start();

                    this.buttonStartServer_Click(null, null);

                    SetInputCapture(true, false);
                }
                catch (Exception ex)
                {
                    Debug.WriteLine(ex.Message);
                    Debug.WriteLine(ex.StackTrace);
                }
            });
        }

        private void buttonExit_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                this.Close();
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex.Message);
                Debug.WriteLine(ex.StackTrace);
            }
        }

        private void buttonConfigureMulticast_Click(object sender, RoutedEventArgs e)
        {
            MulticastConfigDialog.ShowStaticDialog();
        }

        private void listBox1_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (!Mubox.Configuration.MuboxConfigSection.Default.IsCaptureEnabled)
            {
                ToggleInputCapture(true);
            }
        }

        private void textListenPort_TextChanged(object sender, TextChangedEventArgs e)
        {
            // TODO persist to registry
        }
    }
}