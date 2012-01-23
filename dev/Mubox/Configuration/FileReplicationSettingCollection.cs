using System;
using System.Collections.Specialized;
using System.Configuration;

namespace Mubox.Configuration
{
    [ConfigurationCollection(typeof(FileReplicationSetting))]
    public class FileReplicationSettingCollection
        : ConfigurationElementCollection, INotifyCollectionChanged
    {
        protected override ConfigurationElement CreateNewElement()
        {
            return (new FileReplicationSetting()) as ConfigurationElement;
        }

        protected override object GetElementKey(ConfigurationElement element)
        {
            return (element as FileReplicationSetting).Source;
        }

        internal FileReplicationSetting CreateNew(string source)
        {
            ConfigurationElement e = CreateNewElement();
            FileReplicationSetting s = e as FileReplicationSetting;
            s.Source = source;
            base.BaseAdd(e);
            if (CollectionChanged != null)
            {
                CollectionChanged(this, new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Add, s));
            }
            return s;
        }

        public FileReplicationSetting GetOrCreateNew(string source)
        {
            foreach (FileReplicationSetting s in this)
            {
                if (s.Source.Equals(source, StringComparison.OrdinalIgnoreCase))
                {
                    return s;
                }
            }
            return CreateNew(source);
        }

        public void Remove(string source)
        {
            int index = -1;
            object item = null;
            foreach (FileReplicationSetting s in this)
            {
                index++;
                if (s.Source.Equals(source, StringComparison.OrdinalIgnoreCase))
                {
                    item = s;
                    break;
                }
            }
            base.BaseRemove(source);
            if (index > -1)
            {
                if (CollectionChanged != null)
                {
                    CollectionChanged(this, new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Remove, item, index));
                }
            }
        }

        public bool TryGetKeySetting(string source, out FileReplicationSetting s)
        {
            s = base.BaseGet(source) as FileReplicationSetting;
            return s != null;
        }

        #region INotifyCollectionChanged Members

        public event NotifyCollectionChangedEventHandler CollectionChanged;

        #endregion
    }
}