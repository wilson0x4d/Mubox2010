using System;
using System.ComponentModel;
using System.Configuration;

namespace Mubox.Configuration
{
    public class TeamSettings
        : ConfigurationElement, INotifyPropertyChanged
    {
        [ConfigurationProperty("Name", IsRequired = true, IsKey = true)]
        public string Name
        {
            get { return (string)base["Name"]; }
            set { if (!Name.Equals(value, StringComparison.InvariantCultureIgnoreCase)) { base["Name"] = value; this.OnPropertyChanged(o => o.Name); } }
        }

        [ConfigurationProperty("Clients")]
        public ClientSettingsCollection Clients
        {
            get { return (ClientSettingsCollection)base["Clients"]; }
            set { base["Clients"] = value; }
        }

        public ClientSettings ActiveClient { get; set; }

        public event PropertyChangedEventHandler PropertyChanged;
    }
}