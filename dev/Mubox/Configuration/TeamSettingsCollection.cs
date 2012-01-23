using System;
using System.ComponentModel;
using System.Configuration;

namespace Mubox.Configuration
{
    [ConfigurationCollection(typeof(TeamSettings))]
    public class TeamSettingsCollection
        : ConfigurationElementCollection, INotifyPropertyChanged
    {
        [ConfigurationProperty("Default", IsRequired = true, IsKey = false)]
        public string Default
        {
            get { return (string)base["Default"]; }
            set { if (!Default.Equals(value, StringComparison.InvariantCultureIgnoreCase)) { base["Default"] = value; this.OnPropertyChanged(o => o.Default); } }
        }

        public TeamSettings ActiveTeam
        {
            get
            {
                // TODO: HACK: need to implement team selection support :( this at least allows users to edit the config directly
                return GetOrCreateNew(Default);
            }
            set
            {
                Default = value.Name;
            }
        }

        protected override ConfigurationElement CreateNewElement()
        {
            return (new TeamSettings()) as ConfigurationElement;
        }

        protected override object GetElementKey(ConfigurationElement element)
        {
            return (element as TeamSettings).Name;
        }

        internal TeamSettings CreateNew(string name)
        {
            if (string.IsNullOrEmpty(name))
            {
                throw new ArgumentException("Invalid Name", "name");
            }
            var element = CreateNewElement();
            var settings = element as TeamSettings;
            settings.Name = name;
            base.BaseAdd(element);
            Default = name;
            return settings;
        }

        public TeamSettings GetOrCreateNew(string name)
        {
            if (string.IsNullOrWhiteSpace(name))
            {
                name = "Default";
            }
            var settings = default(Configuration.TeamSettings);
            foreach (var o in Mubox.Configuration.MuboxConfigSection.Default.Teams)
            {
                settings = o as Configuration.TeamSettings;
                if (settings != null)
                {
                    if (settings.Name.Equals(name, StringComparison.InvariantCultureIgnoreCase))
                    {
                        return settings;
                    }
                }
            }
            return CreateNew(name);
        }

        public void Remove(string name)
        {
            base.BaseRemove(name);
        }

        public event PropertyChangedEventHandler PropertyChanged;
    }
}