using System.Collections.ObjectModel;

namespace Mubox.View.Client
{
    public sealed class ClientWindowCollection
        : ObservableCollection<Mubox.View.Client.ClientWindow>
    {
        public static ClientWindowCollection Instance { get; private set; }

        static ClientWindowCollection()
        {
            Instance = new ClientWindowCollection();
        }
    }
}