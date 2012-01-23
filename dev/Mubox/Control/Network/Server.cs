using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Net;
using System.Net.Sockets;
using Mubox.Model.Client;

namespace Mubox.Control.Network
{
    public static class Server
    {
        private static TcpListener Listener { get; set; }

        private static bool IsListening { get; set; }

        private static List<ClientBase> Clients { get; set; }

        public static void Start(int portNumber)
        {
            if (Clients == null)
            {
                Clients = new List<ClientBase>();
            }
            if ((Listener == null) || ((Listener.LocalEndpoint as IPEndPoint).Port != portNumber))
            {
                if (Listener != null)
                {
                    try
                    {
                        Listener.Stop();
                    }
                    catch (Exception ex)
                    {
                        Debug.WriteLine(ex.Message);
                        Debug.WriteLine(ex.StackTrace);
                    }
                }
                Listener = new TcpListener(IPAddress.Any, portNumber);
            }
            if (!IsListening)
            {
                Listener.Start();
                Listener.BeginAcceptSocket(AcceptSocketCallback, null);
                IsListening = true;
            }
        }

        public static void Stop()
        {
            if (Listener != null)
            {
                if (IsListening)
                {
                    try
                    {
                        IsListening = false;
                        Listener.Stop();
                    }
                    catch (Exception ex)
                    {
                        Debug.WriteLine(ex.Message);
                        Debug.WriteLine(ex.StackTrace);
                    }
                }
            }
        }

        public static void AcceptSocketCallback(IAsyncResult ar)
        {
            try
            {
                Socket socket = Listener.EndAcceptSocket(ar);
                try
                {
                    if (socket != null)
                    {
                        socket.NoDelay = true;
                        socket.LingerState.Enabled = false;
                        NetworkClient client = null;

                        (System.Windows.Application.Current).Dispatcher.Invoke((Action)delegate()
                        {
                            try
                            {
                                client = new NetworkClient(socket);
                            }
                            catch (Exception ex)
                            {
                                client = null;
                                Debug.WriteLine(ex.Message);
                                Debug.WriteLine(ex.StackTrace);
                            }
                        });

                        if (client != null)
                        {
                            Clients.Add(client);
                            OnClientAccepted(client);
                            client.Attach();
                        }
                    }
                }
                catch (Exception lex)
                {
                    Debug.WriteLine(lex.Message);
                    Debug.WriteLine(lex.StackTrace);
                }
                finally
                {
                    Listener.BeginAcceptSocket(AcceptSocketCallback, null);
                }
            }
            catch (Exception ex)
            {
                IsListening = false;
                Debug.WriteLine(ex.Message);
                Debug.WriteLine(ex.StackTrace);
            }
        }

        public sealed class ServerEventArgs : EventArgs
        {
            public ClientBase Client { get; set; }
        }

        public static event EventHandler<ServerEventArgs> ClientAccepted;

        private static void OnClientAccepted(ClientBase client)
        {
            if (ClientAccepted != null)
            {
                ClientAccepted(Listener, new ServerEventArgs
                {
                    Client = client
                });
            }
        }

        public static event EventHandler<ServerEventArgs> ClientRemoved;

        public static void RemoveClient(ClientBase client)
        {
            Clients.Remove(client);
            if (ClientRemoved != null)
            {
                ClientRemoved(Listener, new ServerEventArgs
                {
                    Client = client
                });
            }
        }
    }
}