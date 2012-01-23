using System;
using System.ComponentModel;
using System.Configuration;

namespace Mubox.Configuration
{
    public class FileReplicationSetting
        : ConfigurationElement, INotifyPropertyChanged
    {
        [ConfigurationProperty("Source", IsRequired = true, IsKey = true)]
        public string Source
        {
            get { return (string)base["Source"]; }
            set { if (!Source.Equals(value, StringComparison.InvariantCultureIgnoreCase)) { base["Source"] = value; this.OnPropertyChanged(o => o.Source); } }
        }

        [ConfigurationProperty("Destination", IsRequired = true, IsKey = false)]
        public string Destination
        {
            get { return (string)base["Destination"]; }
            set { if (!Destination.Equals(value, StringComparison.InvariantCultureIgnoreCase)) { base["Destination"] = value; this.OnPropertyChanged(o => o.Destination); } }
        }

        public event PropertyChangedEventHandler PropertyChanged;

        public override string ToString()
        {
            return (Source ?? "undefined") + " => " + (Destination ?? "undefined");
        }
    }
}