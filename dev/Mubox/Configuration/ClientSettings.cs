using System;
using System.ComponentModel;
using System.Configuration;

namespace Mubox.Configuration
{
    public class ClientSettings
        : ConfigurationElement, INotifyPropertyChanged
    {
        protected override void Init()
        {
            Keys = new KeySettingCollection();
            Files = new FileReplicationSettingCollection();
            base.Init();
        }

        [ConfigurationProperty("Name", IsRequired = true, IsKey = true)]
        public string Name
        {
            get { return (string)base["Name"]; }
            set { if (!Name.Equals(value, StringComparison.InvariantCultureIgnoreCase)) { base["Name"] = value; this.OnPropertyChanged(o => o.Name); } }
        }

        [ConfigurationProperty("CanLaunch", IsRequired = false, DefaultValue = false)]
        public bool CanLaunch
        {
            get { return (bool)base["CanLaunch"]; }
            set { if (!CanLaunch.Equals(value)) { base["CanLaunch"] = value; this.OnPropertyChanged(o => o.CanLaunch); } }
        }

        [ConfigurationProperty("EnableIsolation", IsRequired = false, DefaultValue = false)]
        public bool EnableIsolation
        {
            get { return (bool)base["EnableIsolation"]; }
            set { if (!EnableIsolation.Equals(value)) { base["EnableIsolation"] = value; this.OnPropertyChanged(o => o.EnableIsolation); } }
        }

        [ConfigurationProperty("MemoryMB", IsRequired = false, DefaultValue = 0)]
        public int MemoryMB
        {
            get { return (int)base["MemoryMB"]; }
            set { if (!MemoryMB.Equals(value)) { base["MemoryMB"] = value; this.OnPropertyChanged(o => o.MemoryMB); } }
        }

        [ConfigurationProperty("ServerName", IsRequired = false, DefaultValue = "localhost")]
        public string ServerName
        {
            get { return (string)base["ServerName"]; }
            set { if (!ServerName.Equals(value, StringComparison.InvariantCultureIgnoreCase)) { base["ServerName"] = value; this.OnPropertyChanged(o => o.ServerName); } }
        }

        [ConfigurationProperty("ServerPortNumber", IsRequired = false, DefaultValue = 17520)]
        public int ServerPortNumber
        {
            get { return (int)base["ServerPortNumber"]; }
            set { if (!ServerPortNumber.Equals(value)) { base["ServerPortNumber"] = value; this.OnPropertyChanged(o => o.ServerPortNumber); } }
        }

        #region PerformConnectOnLoad

        [ConfigurationProperty("PerformConnectOnLoad", IsRequired = false, DefaultValue = false)]
        public bool PerformConnectOnLoad
        {
            get { return (bool)base["PerformConnectOnLoad"]; }
            set { if (!PerformConnectOnLoad.Equals(value)) { base["PerformConnectOnLoad"] = value; this.OnPropertyChanged(o => o.PerformConnectOnLoad); } }
        }

        #endregion
        #region ApplicationPath

        [ConfigurationProperty("ApplicationPath", IsRequired = false, DefaultValue = @"C:\WoW\wow.exe")]
        public string ApplicationPath
        {
            get { return (string)base["ApplicationPath"]; }
            set { if (!ApplicationPath.Equals(value, StringComparison.InvariantCultureIgnoreCase)) { base["ApplicationPath"] = value; this.OnPropertyChanged(o => o.ApplicationPath); } }
        }

        #endregion
        #region ApplicationArguments

        [ConfigurationProperty("ApplicationArguments", IsRequired = false, DefaultValue = "-windowed")]
        public string ApplicationArguments
        {
            get { return (string)base["ApplicationArguments"]; }
            set { if (!ApplicationArguments.Equals(value, StringComparison.InvariantCultureIgnoreCase)) { base["ApplicationArguments"] = value; this.OnPropertyChanged(o => o.ApplicationArguments); } }
        }

        #endregion
        #region IsolationPath

        [ConfigurationProperty("IsolationPath", IsRequired = false, DefaultValue = @"C:\MUBOX\")]
        public string IsolationPath
        {
            get { return (string)base["IsolationPath"]; }
            set { if (!IsolationPath.Equals(value, StringComparison.InvariantCultureIgnoreCase)) { base["IsolationPath"] = value; this.OnPropertyChanged(o => o.IsolationPath); } }
        }

        #endregion
        #region ProcessorAffinity

        [ConfigurationProperty("ProcessorAffinity", IsRequired = false)]
        public uint ProcessorAffinity
        {
            get { return (uint)base["ProcessorAffinity"]; }
            set { if (!ProcessorAffinity.Equals(value)) { base["ProcessorAffinity"] = value; this.OnPropertyChanged(o => o.ProcessorAffinity); } }
        }

        #endregion
        #region RememberWindowPosition

        [ConfigurationProperty("RememberWindowPosition", IsRequired = false, DefaultValue = false)]
        public bool RememberWindowPosition
        {
            get { return (bool)base["RememberWindowPosition"]; }
            set { if (!RememberWindowPosition.Equals(value)) { base["RememberWindowPosition"] = value; this.OnPropertyChanged(o => o.RememberWindowPosition); } }
        }

        #endregion
        #region WindowPosition

        [ConfigurationProperty("WindowPosition", IsRequired = false)]
        public System.Windows.Point WindowPosition
        {
            get { return (System.Windows.Point)base["WindowPosition"]; }
            set { if (!WindowPosition.Equals(value)) { base["WindowPosition"] = value; this.OnPropertyChanged(o => o.WindowPosition); } }
        }

        #endregion
        #region WindowSize

        [ConfigurationProperty("WindowSize", IsRequired = false)]
        public System.Windows.Size WindowSize
        {
            get { return (System.Windows.Size)base["WindowSize"]; }
            set { if (!WindowSize.Equals(value)) { base["WindowSize"] = value; this.OnPropertyChanged(o => o.WindowSize); } }
        }

        #endregion
        #region RemoveWindowBorderEnabled

        [ConfigurationProperty("RemoveWindowBorderEnabled", IsRequired = false, DefaultValue = default(bool))]
        public bool RemoveWindowBorderEnabled
        {
            get { return (bool)base["RemoveWindowBorderEnabled"]; }
            set { if (!RemoveWindowBorderEnabled.Equals(value)) { base["RemoveWindowBorderEnabled"] = value; this.OnPropertyChanged(o => o.RemoveWindowBorderEnabled); } }
        }

        #endregion
        #region InstallWoWAddOn

        [ConfigurationProperty("InstallWoWAddOn", IsRequired = false, DefaultValue = default(bool))]
        public bool InstallWoWAddOn
        {
            get { return (bool)base["InstallWoWAddOn"]; }
            set { if (!InstallWoWAddOn.Equals(value)) { base["InstallWoWAddOn"] = value; this.OnPropertyChanged(o => o.InstallWoWAddOn); } }
        }

        #endregion
        #region WindowHandle

        [ConfigurationProperty("WindowHandle", IsRequired = false, DefaultValue = default(uint))]
        public uint WindowHandleInternal
        {
            get { return (uint)base["WindowHandle"]; }
            set { if (!WindowHandleInternal.Equals(value)) { base["WindowHandle"] = value; this.OnPropertyChanged(o => o.WindowHandleInternal); this.OnPropertyChanged(o => o.WindowHandle); } }
        }

        public IntPtr WindowHandle
        {
            get
            {
                return (IntPtr)WindowHandleInternal;
            }
            set
            {
                WindowHandleInternal = (uint)value;
            }
        }

        #endregion
        /*
        #region XXX

        [ConfigurationProperty("XXX", IsRequired = false, DefaultValue = default(YYY))]
        public YYY XXX
        {
            get { return (YYY)base["XXX"]; }
            set { if (!XXX.Equals(value)) { base["XXX"] = value;this.OnPropertyChanged(o => o.XXX"); } }
        }

        #endregion
         * */
        [ConfigurationProperty("Keys")]
        public KeySettingCollection Keys
        {
            get { return (KeySettingCollection)base["Keys"]; }
            set { if (!Keys.Equals(value)) { base["Keys"] = value; this.OnPropertyChanged(o => o.Keys); } }
        }

        [ConfigurationProperty("Files")]
        public FileReplicationSettingCollection Files
        {
            get { return (FileReplicationSettingCollection)base["Files"]; }
            set { if (!Keys.Equals(value)) { base["Files"] = value; this.OnPropertyChanged(o => o.Files); } }
        }

        public event PropertyChangedEventHandler PropertyChanged;
    }
}