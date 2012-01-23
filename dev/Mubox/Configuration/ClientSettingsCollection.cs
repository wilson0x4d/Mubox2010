using System;
using System.Configuration;
using System.Linq;

namespace Mubox.Configuration
{
    [ConfigurationCollection(typeof(ClientSettings))]
    public class ClientSettingsCollection
        : ConfigurationElementCollection
    {
        protected override ConfigurationElement CreateNewElement()
        {
            return (new ClientSettings()) as ConfigurationElement;
        }

        protected override object GetElementKey(ConfigurationElement element)
        {
            return (element as ClientSettings).Name;
        }

        internal ClientSettings CreateNew(string name)
        {
            if (string.IsNullOrEmpty(name))
            {
                throw new ArgumentException("Invalid Name", "name");
            }
            var element = CreateNewElement();
            var settings = element as ClientSettings;
            settings.Name = name;
            base.BaseAdd(element);
            return settings;
        }

        public ClientSettings GetOrCreateNew(string characterName)
        {
            foreach (var characterSettings in this.OfType<ClientSettings>())
            {
                if (characterSettings != null)
                {
                    if (characterSettings.Name.Equals(characterName, StringComparison.InvariantCultureIgnoreCase))
                    {
                        return characterSettings;
                    }
                }
            }
            return CreateNew(characterName);
        }

        public void Remove(string name)
        {
            base.BaseRemove(name);
        }
    }
}