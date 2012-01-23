using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Windows;
using Mubox.Model.Input;

namespace Mubox.Model.Client
{
    // TODO ClientBase and NetworkClient probably need to be factored out of the codebase
    public class ClientBase
        : DependencyObject
    {
        protected ClientBase()
        {
            ClientId = Guid.NewGuid();
        }

        #region ClientId

        public Guid ClientId { get; set; }

        #endregion
        #region Address

        /// <summary>
        /// Address Dependency Property
        /// </summary>
        public static readonly DependencyProperty AddressProperty =
            DependencyProperty.Register("Address", typeof(string), typeof(ClientBase),
                new FrameworkPropertyMetadata((string)"127.0.0.1"));

        /// <summary>
        /// Gets or sets the Address property.  This dependency property
        /// indicates the Address of this Machine.
        /// </summary>
        public string Address
        {
            get { return (string)GetValue(AddressProperty); }
            set { SetValue(AddressProperty, value); }
        }

        internal static List<string> localAddressTable = InitializeLocalAddressTable();

        private static List<string> InitializeLocalAddressTable()
        {
            List<string> localAddressTable = new List<string>();
            localAddressTable.Add("127.0.0.1");
            localAddressTable.AddRange(System.Net.Dns.GetHostAddresses(Environment.MachineName).Select((a) => a.ToString()));
            foreach (string addressString in localAddressTable)
            {
                Debug.WriteLine("Local Address: " + addressString);
            }
            return localAddressTable;
        }

        private bool isLocalAddress;
        private bool isLocalAddressInitialized;

        public bool IsLocalAddress
        {
            get
            {
                if (isLocalAddressInitialized)
                {
                    return isLocalAddress;
                }
                isLocalAddress = ((this.Address == "127.0.0.1") || localAddressTable.Contains(this.Address));
                isLocalAddressInitialized = true;
                return isLocalAddress;
            }
        }

        #endregion
        #region IsAttached

        private bool _isAttached { get; set; }

        public bool IsAttached
        {
            get
            {
                return _isAttached;
            }
            set
            {
                if (_isAttached != value)
                {
                    _isAttached = value;
                    if (IsAttachedChanged != null)
                    {
                        IsAttachedChanged(this, EventArgs.Empty);
                    }
                }
            }
        }

        public event EventHandler<EventArgs> IsAttachedChanged;

        #endregion
        #region LastActivatedTimestamp

        public long LastActivatedTimestamp { get; set; }

        #endregion
        #region DisplayName

        /// <summary>
        /// DisplayName Dependency Property
        /// </summary>
        public static readonly DependencyProperty DisplayNameProperty =
            DependencyProperty.Register("DisplayName", typeof(string), typeof(ClientBase),
                new FrameworkPropertyMetadata((string)"",
                    new PropertyChangedCallback(OnDisplayNameChanged)));

        /// <summary>
        /// Gets or sets the DisplayName property.  This dependency property
        /// indicates the Display Name of the Client.
        /// </summary>
        public string DisplayName
        {
            get { return (string)GetValue(DisplayNameProperty); }
            set { SetValue(DisplayNameProperty, value); }
        }

        /// <summary>
        /// Handles changes to the DisplayName property.
        /// </summary>
        private static void OnDisplayNameChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            ClientBase clientBase = d as ClientBase;
            if (d != null)
            {
                Debug.WriteLine("SrxDisplayNameChanged for " + clientBase.ClientId.ToString() + " to " + clientBase.DisplayName ?? "");
                ((ClientBase)d).OnDisplayNameChanged(e);
            }
        }

        /// <summary>
        /// Provides derived classes an opportunity to handle changes to the DisplayName property.
        /// </summary>
        protected virtual void OnDisplayNameChanged(DependencyPropertyChangedEventArgs e)
        {
        }

        #endregion
        public static string Sanitize(string text)
        {
            byte[] textBytes = Win32.CodePage.ConvertToCodePage(text, 1251);
            text = System.Text.Encoding.ASCII.GetString(textBytes);
            if (string.IsNullOrEmpty(text))
            {
                return "NULL";
            }
            return System.Text.RegularExpressions.Regex.Replace(text, "[^A-Za-z0-9=]*", "");
        }

        public static string Sanitize(byte[] data)
        {
            if ((data == null) || (data.Length == 0))
            {
                return "NULL";
            }
            return Sanitize(Convert.ToBase64String(data));
        }

        #region WindowStationHandle

        public IntPtr WindowStationHandle { get; set; }

        #endregion
        #region WindowDesktopHandle

        public IntPtr WindowDesktopHandle { get; set; }

        #endregion
        #region WindowHandle

        public IntPtr WindowHandle { get; set; }

        #endregion
        #region CachedScreenFromClientRect

        public Win32.Windows.RECT CachedScreenFromClientRect { get; set; }

        public DateTime CachedScreenFromClientRectExpiry { get; set; }

        #endregion
        #region Latency

        /// <summary>
        /// Latency Dependency Property
        /// </summary>
        public static readonly DependencyProperty LatencyProperty =
            DependencyProperty.Register("Latency", typeof(long), typeof(ClientBase),
                new FrameworkPropertyMetadata((long)-1L));

        /// <summary>
        /// Gets or sets the Latency property.  This dependency property
        /// indicates Server to Client roundtrip latency.
        /// </summary>
        public long Latency
        {
            get { return (long)GetValue(LatencyProperty); }
            set { SetValue(LatencyProperty, value); }
        }

        #endregion

        #region PerformanceInfo

        /// <summary>
        /// PerformanceInfo Dependency Property
        /// </summary>
        public static readonly DependencyProperty PerformanceInfoProperty =
            DependencyProperty.Register("PerformanceInfo", typeof(Model.Client.PerformanceInfo), typeof(ClientBase),
                new FrameworkPropertyMetadata((Model.Client.PerformanceInfo)null));

        /// <summary>
        /// Gets or sets the PerformanceInfo property.  This dependency property
        /// indicates Client Performance Info.
        /// </summary>
        public Model.Client.PerformanceInfo PerformanceInfo
        {
            get { return (Model.Client.PerformanceInfo)GetValue(PerformanceInfoProperty); }
            set { SetValue(PerformanceInfoProperty, value); }
        }

        #endregion

        public bool FixAltKey { get; set; }

        public virtual void Dispatch(MouseInput e)
        {
        }

        public virtual void Dispatch(KeyboardInput e)
        {
        }

        public virtual void Dispatch(CommandInput e)
        {
        }

        public virtual void Dispatch(ushort vk)
        {
            Dispatch(new KeyboardInput { WM = Mubox.Win32.WM.KEYDOWN, VK = vk, Time = Win32.SendInputApi.GetTickCount() });
            Dispatch(new KeyboardInput { WM = Mubox.Win32.WM.KEYUP, VK = vk, Time = Win32.SendInputApi.GetTickCount() });
        }

        private void Dispatch(IEnumerable<ushort> vkSet)
        {
            foreach (ushort vk in vkSet)
            {
                Dispatch(vk);
            }
        }

        public virtual void Activate()
        {
            LastActivatedTimestamp = DateTime.Now.Ticks;
        }

        public virtual void Deactivate()
        {
        }

        public virtual void Attach()
        {
            IsAttached = true;
        }

        public virtual void Detach()
        {
            IsAttached = false;
        }

        protected long PingSendTimestampTicks { get; set; }

        public virtual void Ping(IntPtr windowStationHandle, IntPtr windowDesktopHandle, IntPtr windowHandle)
        {
            PingSendTimestampTicks = DateTime.Now.Ticks;
        }

        public override string ToString()
        {
            return this.DisplayName + "/" + this.WindowStationHandle.ToString() + "/" + this.WindowDesktopHandle.ToString() + "/" + this.WindowHandle.ToString();
        }
    }
}