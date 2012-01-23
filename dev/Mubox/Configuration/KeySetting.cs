using System.ComponentModel;
using System.Configuration;

namespace Mubox.Configuration
{
    public class KeySetting
        : ConfigurationElement, INotifyPropertyChanged
    {
        [ConfigurationProperty("InputKey", IsRequired = true, IsKey = true)]
        public Win32.VK InputKey
        {
            get { return (Win32.VK)base["InputKey"]; }
            set { if (!InputKey.Equals(value)) { base["InputKey"] = value; this.OnPropertyChanged(o => o.InputKey); } }
        }

        [ConfigurationProperty("OutputKey", IsRequired = true, IsKey = false)]
        public Win32.VK OutputKey
        {
            get { return (Win32.VK)base["OutputKey"]; }
            set { if (!OutputKey.Equals(value)) { base["OutputKey"] = value; this.OnPropertyChanged(o => o.OutputKey); } }
        }

        [ConfigurationProperty("OutputModifiers", IsRequired = false, DefaultValue = default(Win32.CAS))]
        public Win32.CAS OutputModifiers
        {
            get { return (Win32.CAS)base["OutputModifiers"]; }
            set { if (!OutputModifiers.Equals(value)) { base["OutputModifiers"] = value; this.OnPropertyChanged(o => o.OutputModifiers); } }
        }

        /// <summary>
        /// <para>if true then "ActiveClient" will Ignore "OutputModifiers"</para>
        /// </summary>
        [ConfigurationProperty("EnableNoModActiveClient", IsRequired = false, DefaultValue = default(bool))]
        public bool EnableNoModActiveClient
        {
            get { return (bool)base["EnableNoModActiveClient"]; }
            set { if (!EnableNoModActiveClient.Equals(value)) { base["EnableNoModActiveClient"] = value; this.OnPropertyChanged(o => o.EnableNoModActiveClient); } }
        }

        [ConfigurationProperty("SendToDesktop", IsRequired = false, DefaultValue = default(bool))]
        public bool SendToDesktop
        {
            get { return (bool)base["SendToDesktop"]; }
            set { if (!SendToDesktop.Equals(value)) { base["SendToDesktop"] = value; this.OnPropertyChanged(o => o.SendToDesktop); } }
        }

        [ConfigurationProperty("ActiveClientOnly", IsRequired = false, DefaultValue = default(bool))]
        public bool ActiveClientOnly
        {
            get { return (bool)base["ActiveClientOnly"]; }
            set { if (!ActiveClientOnly.Equals(value)) { base["ActiveClientOnly"] = value; this.OnPropertyChanged(o => o.ActiveClientOnly); } }
        }

        [ConfigurationProperty("RoundRobinKey", IsRequired = false, DefaultValue = default(bool))]
        public bool RoundRobinKey
        {
            get { return (bool)base["RoundRobinKey"]; }
            set { if (!RoundRobinKey.Equals(value)) { base["RoundRobinKey"] = value; this.OnPropertyChanged(o => o.RoundRobinKey); } }
        }

        public event PropertyChangedEventHandler PropertyChanged;
    }
}