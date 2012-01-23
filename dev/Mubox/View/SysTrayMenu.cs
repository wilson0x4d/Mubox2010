using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Media;
using Mubox.Model;

namespace Mubox.View
{
    public class SysTrayMenu
        : ContextMenu
    {
        public SysTrayMenu(Action helpCallback, Action exitApplicationCallback)
        {
            try
            {
                Resources["imageShortcutIcon"] = new Image
                {
                    Source = (new ImageSourceConverter()).ConvertFromString("pack://application:,,,/Mubox;component/Content/Images/GotoShortcutsHS.png") as ImageSource
                };
                Resources["imageNavForwardIcon"] = new Image
                {
                    Source = (new ImageSourceConverter()).ConvertFromString("pack://application:,,,/Mubox;component/Content/Images/NavForward.png") as ImageSource
                };
                Resources["imageMenuHelpIcon"] = new Image
                {
                    Source = (new ImageSourceConverter()).ConvertFromString("pack://application:,,,/Mubox;component/Content/Images/HelpIcon.png") as ImageSource
                };
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex.Message);
                Debug.WriteLine(ex.StackTrace);
            }

            try
            {
                System.Drawing.Point mousePosition = new System.Drawing.Point(System.Windows.Forms.Screen.PrimaryScreen.WorkingArea.Right - 16, System.Windows.Forms.Screen.PrimaryScreen.WorkingArea.Bottom - 24);
                Win32.Cursor.GetCursorPos(out mousePosition);

                List<object> quickLaunchMenuItems = new List<object>();
                ItemsSource = quickLaunchMenuItems;

                MenuItem menuItem = null;

                foreach (var team in Mubox.Configuration.MuboxConfigSection.Default.Teams.OfType<Mubox.Configuration.TeamSettings>())
                {
                    menuItem = CreateTeamShortcutMenu(team);
                    if (menuItem != null)
                    {
                        quickLaunchMenuItems.Add(menuItem);
                    }
                }
                quickLaunchMenuItems.Add(new Separator());

                // New Mubox Client
                menuItem = new MenuItem();
                menuItem.Click += (sender, e) =>
                {
                    try
                    {
                        string clientName = Mubox.View.PromptForClientNameDialog.PromptForClientName();
                        // TODO try and enforce "unique" client names, e.g. if we already have a ClientX running, don't allow a second ClientX without warning.

                        var clientSettings = Mubox.Configuration.MuboxConfigSection.Default.Teams.ActiveTeam.Clients.GetOrCreateNew(clientName);
                        clientSettings.CanLaunch = true;
                        Mubox.Configuration.MuboxConfigSection.Default.Save();

                        ClientState clientState = new ClientState(clientSettings);
                        Mubox.View.Client.ClientWindow clientWindow = new Mubox.View.Client.ClientWindow(clientState);
                        clientWindow.Show();
                    }
                    catch (Exception ex)
                    {
                        Debug.WriteLine(ex.Message);
                        Debug.WriteLine(ex.StackTrace);
                    }
                };
                menuItem.Header = "_Configure New Mubox Client...";
                menuItem.Icon = Resources["imageSettingsIcon"];
                quickLaunchMenuItems.Add(menuItem);

                // Launch Mubox Server
                quickLaunchMenuItems.Add(new Separator());
                if (Mubox.View.Server.ServerWindow.Instance == null)
                {
                    menuItem = new MenuItem();
                    menuItem.Click += (sender, e) =>
                    {
                        CreateServerUI();
                    };
                    menuItem.Header = "Mubox _Server...";
                    quickLaunchMenuItems.Add(menuItem);
                }
                else
                {
                    // "Disable 'Client Switching' Feature"
                    menuItem = new MenuItem();
                    menuItem.IsCheckable = true;
                    menuItem.IsChecked = Mubox.Configuration.MuboxConfigSection.Default.DisableAltTabHook;
                    menuItem.Click += (sender, e) =>
                    {
                        Mubox.Configuration.MuboxConfigSection.Default.DisableAltTabHook = !Mubox.Configuration.MuboxConfigSection.Default.DisableAltTabHook;
                        Mubox.Configuration.MuboxConfigSection.Default.Save();
                    };
                    menuItem.Header = "Disable \"Client Switching\" Feature";
                    menuItem.ToolTip = "Enable this option to use the default Windows Task Switcher instead of the Mubox Server UI, this only affects Client Switching.";
                    quickLaunchMenuItems.Add(menuItem);

                    // "Reverse Client Switching"
                    menuItem = new MenuItem();
                    menuItem.IsCheckable = true;
                    menuItem.IsChecked = Mubox.Configuration.MuboxConfigSection.Default.ReverseClientSwitching;
                    menuItem.Click += (sender, e) =>
                    {
                        Mubox.Configuration.MuboxConfigSection.Default.ReverseClientSwitching = !Mubox.Configuration.MuboxConfigSection.Default.ReverseClientSwitching;
                        Mubox.Configuration.MuboxConfigSection.Default.Save();
                    };
                    menuItem.Header = "Reverse Client Switching";
                    menuItem.ToolTip = "Enable this option to reverse the order that Client Switcher will switch between clients.";
                    quickLaunchMenuItems.Add(menuItem);

                    // "Toggle Server UI"
                    menuItem = new MenuItem();
                    menuItem.Click += (sender, e) =>
                    {
                        if (Mubox.View.Server.ServerWindow.Instance != null)
                        {
                            Mubox.View.Server.ServerWindow.Instance.SetInputCapture((Mubox.View.Server.ServerWindow.Instance.Visibility == Visibility.Visible), (Mubox.View.Server.ServerWindow.Instance.Visibility != Visibility.Visible));
                        }
                    };
                    menuItem.Header = "Toggle Server UI";
                    menuItem.ToolTip = "Show/Hide the Server UI";
                    quickLaunchMenuItems.Add(menuItem);

                    quickLaunchMenuItems.Add(new Separator());

                    // "Enable Input Capture"
                    menuItem = new MenuItem();
                    menuItem.IsCheckable = true;
                    menuItem.IsChecked = Mubox.Configuration.MuboxConfigSection.Default.IsCaptureEnabled;
                    menuItem.Click += (sender, e) =>
                    {
                        if (Mubox.View.Server.ServerWindow.Instance != null)
                        {
                            Mubox.View.Server.ServerWindow.Instance.ToggleInputCapture(false);
                        }
                    };
                    menuItem.Header = "Enable Input Capture";
                    menuItem.ToolTip = "'Input Capture' includes both Mouse and Keyboard Input";
                    quickLaunchMenuItems.Add(menuItem);

                    // "Configure Keyboard"
                    menuItem = new MenuItem();
                    menuItem.Click += (sender, e) =>
                    {
                        Mubox.View.Server.MulticastConfigDialog.ShowStaticDialog();
                    };
                    menuItem.Header = "Configure Keyboard..";
                    quickLaunchMenuItems.Add(menuItem);

                    if (Mubox.Configuration.MuboxConfigSection.Default.IsCaptureEnabled)
                    {
                        // "Enable Multicast"
                        menuItem = new MenuItem();
                        menuItem.IsCheckable = true;
                        menuItem.IsChecked = Mubox.Configuration.MuboxConfigSection.Default.EnableMulticast;
                        menuItem.Click += (sender, e) =>
                        {
                            Mubox.Configuration.MuboxConfigSection.Default.EnableMulticast = !Mubox.Configuration.MuboxConfigSection.Default.EnableMulticast;
                            Mubox.Configuration.MuboxConfigSection.Default.Save();
                        };
                        menuItem.Header = "Enable Multicast";
                        menuItem.ToolTip = "'Keyboard Multicast' replicates your Key Presses to all Clients.";
                        quickLaunchMenuItems.Add(menuItem);

                        // "Enable Mouse Capture"
                        quickLaunchMenuItems.Add(new Separator());
                        menuItem = new MenuItem();
                        menuItem.IsCheckable = true;
                        menuItem.IsChecked = Mubox.Configuration.MuboxConfigSection.Default.EnableMouseCapture;
                        menuItem.Click += (sender, e) =>
                        {
                            Mubox.Configuration.MuboxConfigSection.Default.EnableMouseCapture = !Mubox.Configuration.MuboxConfigSection.Default.EnableMouseCapture;
                            Mubox.Configuration.MuboxConfigSection.Default.Save();
                        };
                        menuItem.Header = "Enable Mouse Capture";
                        menuItem.ToolTip = "Disable Mouse Capture if you use a third-party application for the Mouse.";
                        quickLaunchMenuItems.Add(menuItem);

                        if (Mubox.Configuration.MuboxConfigSection.Default.EnableMouseCapture)
                        {
                            {
                                // "Mouse Clone" Menu
                                List<MenuItem> mouseCloneModeMenu = new List<MenuItem>();

                                // "Disabled"
                                menuItem = new MenuItem();
                                menuItem.IsCheckable = true;
                                menuItem.IsChecked = Mubox.Configuration.MuboxConfigSection.Default.MouseCloneMode == MouseCloneModeType.Disabled;
                                menuItem.Click += (sender, e) =>
                                {
                                    Mubox.Configuration.MuboxConfigSection.Default.MouseCloneMode = MouseCloneModeType.Disabled;
                                    Mubox.Configuration.MuboxConfigSection.Default.Save();
                                };
                                menuItem.Header = "Disabled";
                                menuItem.ToolTip = "Use this option to Disable the Mouse Clone feature.";
                                mouseCloneModeMenu.Add(menuItem);

                                // "Toggled"
                                menuItem = new MenuItem();
                                menuItem.IsCheckable = true;
                                menuItem.IsChecked = (Mubox.Configuration.MuboxConfigSection.Default.MouseCloneMode == Mubox.Model.MouseCloneModeType.Toggled);
                                menuItem.Click += (sender, e) =>
                                {
                                    Mubox.Configuration.MuboxConfigSection.Default.MouseCloneMode = Mubox.Model.MouseCloneModeType.Toggled;
                                    Mubox.Configuration.MuboxConfigSection.Default.Save();
                                };
                                menuItem.Header = "Toggled";
                                menuItem.ToolTip = "Mouse Clone is Active while CAPS LOCK is ON, and Inactive while CAPS LOCK is OFF.";
                                mouseCloneModeMenu.Add(menuItem);

                                // "Pressed"
                                menuItem = new MenuItem();
                                menuItem.IsCheckable = true;
                                menuItem.IsChecked = (Mubox.Configuration.MuboxConfigSection.Default.MouseCloneMode == MouseCloneModeType.Pressed);
                                menuItem.Click += (sender, e) =>
                                {
                                    Mubox.Configuration.MuboxConfigSection.Default.MouseCloneMode = Mubox.Model.MouseCloneModeType.Pressed;
                                    Mubox.Configuration.MuboxConfigSection.Default.Save();
                                };
                                menuItem.Header = "Pressed";
                                menuItem.ToolTip = "Mouse Clone is Active while CAPS LOCK Key is pressed, and Inactive while CAPS LOCK Key is released.";
                                mouseCloneModeMenu.Add(menuItem);

                                menuItem = new MenuItem();
                                menuItem.Header = "Mouse Clone";
                                menuItem.ItemsSource = mouseCloneModeMenu;
                                quickLaunchMenuItems.Add(menuItem);
                            }
                            {
                                // "Mouse Buffer" Option
                                List<MenuItem> mouseClickBufferMenu = new List<MenuItem>();

                                foreach (double time in new double[] { 0.0, 100.0, 150.0, 200.0, 250.0, 500.0, 750.0, 1000.0 })
                                {
                                    // "Disabled"
                                    CreateMouseBufferMenuItem(menuItem, mouseClickBufferMenu, time);
                                }

                                menuItem = new MenuItem();
                                menuItem.Header = "Mouse Buffer";
                                menuItem.ToolTip = "Mouse Buffer prevents Mouse Movement from interrupting a Click gesture.";
                                menuItem.ItemsSource = mouseClickBufferMenu;
                                quickLaunchMenuItems.Add(menuItem);
                            }
                        }
                    }

                    quickLaunchMenuItems.Add(new Separator());

                    // "Auto-Start Server"
                    menuItem = new MenuItem();
                    menuItem.IsCheckable = true;
                    menuItem.IsChecked = Mubox.Configuration.MuboxConfigSection.Default.AutoStartServer;
                    menuItem.Click += (sender, e) =>
                    {
                        Mubox.Configuration.MuboxConfigSection.Default.AutoStartServer = !Mubox.Configuration.MuboxConfigSection.Default.AutoStartServer;
                        Mubox.Configuration.MuboxConfigSection.Default.Save();
                    };
                    menuItem.Header = "Auto Start Server";
                    quickLaunchMenuItems.Add(menuItem);
                }

                // Show Help
                quickLaunchMenuItems.Add(new Separator());
                menuItem = new MenuItem();
                menuItem.Click += (sender, e) =>
                {
                    helpCallback();
                };
                menuItem.Icon = Resources["imageMenuHelpIcon"];
                menuItem.Header = "Help...";
                quickLaunchMenuItems.Add(menuItem);

                // Cancel QuickLaunch Menu
                quickLaunchMenuItems.Add(new Separator());
                menuItem = new MenuItem();
                menuItem.Click += (sender, e) =>
                {
                    // NOP
                };
                menuItem.Header = "Cancel Menu";
                quickLaunchMenuItems.Add(menuItem);

                // Exit QuickLaunch Application
                menuItem = new MenuItem();
                menuItem.Click += (sender, e) =>
                {
                    exitApplicationCallback();
                    foreach (System.Windows.Window window in System.Windows.Application.Current.Windows)
                    {
                        try
                        {
                            window.Close();
                        }
                        catch (Exception ex)
                        {
                            Debug.WriteLine(ex.Message);
                            Debug.WriteLine(ex.StackTrace);
                        }
                    }
                    Mubox.View.Server.ServerWindow.Instance = null;
                    exitApplicationCallback();
                };
                menuItem.Header = "E_xit Mubox";
                quickLaunchMenuItems.Add(menuItem);
                Placement = System.Windows.Controls.Primitives.PlacementMode.AbsolutePoint;
                VerticalOffset = mousePosition.Y - 2;
                HorizontalOffset = mousePosition.X - 8;
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex.Message);
                Debug.WriteLine(ex.StackTrace);
            }
        }

        private MenuItem CreateTeamShortcutMenu(Configuration.TeamSettings team)
        {
            var menuItem = default(MenuItem);

            // Shortcuts Menu Item
            List<object> quickLaunchClientShortcuts = new List<object>();
            Mubox.Configuration.ClientSettingsCollection clients = team.Clients;

            menuItem = new MenuItem();
            menuItem.Header = "Start All";
            menuItem.Click += (sender, e) =>
            {
                // TODO: need to create a team selector (not launcher), maybe just a 'Teams' menu, need to allow team selection by hotkey, allow hotkey definition from a sub-menu e.g. MENU={"Select","Set/Clear HotKey.."}
                // TODO: allow characters to be shared between multiple Teams, two teams should not 'Launch' the same 'Character' more than once. Characters will be known uniquely only by 'Name'
                LaunchTeam(team);
            };
            quickLaunchClientShortcuts.Add(menuItem);

            // "Auto-Launch Game on Client Start"
            menuItem = new MenuItem();
            menuItem.IsCheckable = true;
            menuItem.IsChecked = Mubox.Configuration.MuboxConfigSection.Default.AutoLaunchGame;
            menuItem.Click += (sender, e) =>
            {
                Mubox.Configuration.MuboxConfigSection.Default.AutoLaunchGame = !Mubox.Configuration.MuboxConfigSection.Default.AutoLaunchGame;
                Mubox.Configuration.MuboxConfigSection.Default.Save();
            };
            menuItem.Header = "Auto-Launch Game on Client Start";
            menuItem.ToolTip =
                "Enable this option to Automatically Launch your Game when the Mubox Client is started via the Quick Launch Menu." + Environment.NewLine +
                "Note, the game will not run until the client successfully connects to the Server, once a Server Connection is established the Launch will continue.";
            quickLaunchClientShortcuts.Add(menuItem);

            quickLaunchClientShortcuts.Add(new Separator());
            foreach (var o in clients)
            {
                var client = o as Mubox.Configuration.ClientSettings;
                if (!client.CanLaunch || (Mubox.View.Client.ClientWindowCollection.Instance.Count((dlg) => dlg.ClientState.Settings.Name.ToUpper() == client.Name.ToUpper()) != 0))
                {
                    continue;
                }

                client.PerformConnectOnLoad = true;
                quickLaunchClientShortcuts.Add(
                    QuickLaunchMenu_CreateClientItem(client.Name)
                    );
            }

            menuItem = new MenuItem();
            menuItem.Header = team.Name;
            menuItem.Icon = Resources["imageShortcutIcon"];
                        
            if (quickLaunchClientShortcuts.Count > 3)
            {
                menuItem = new MenuItem();
                menuItem.Header = team.Name;
                menuItem.Icon = Resources["imageShortcutIcon"];
                menuItem.ItemsSource = quickLaunchClientShortcuts;

                var lMenuItem = new MenuItem();
                lMenuItem.IsCheckable = true;
                lMenuItem.IsChecked = (Mubox.Configuration.MuboxConfigSection.Default.Teams.Default.Equals(team.Name));
                lMenuItem.Header = "Select Team"; // TODO: need hotkey support
                lMenuItem.Click += (s, e) =>
                    {
                        Mubox.Configuration.MuboxConfigSection.Default.Teams.ActiveTeam = team;
                    };
                quickLaunchClientShortcuts.Insert(0, lMenuItem);
            }
            else
            {
                menuItem.IsCheckable = true;
                menuItem.IsChecked = (Mubox.Configuration.MuboxConfigSection.Default.Teams.Default.Equals(team.Name));
                menuItem.Click += (sender, e) =>
                {
                    Mubox.Configuration.MuboxConfigSection.Default.Teams.ActiveTeam = team;
                };
            }

            return menuItem;
        }

        private static void LaunchTeam(Mubox.Configuration.TeamSettings team)
        {
            Mubox.Configuration.MuboxConfigSection.Default.Teams.ActiveTeam = team;
            
            foreach (var o in team.Clients)
            {
                var character = o as Mubox.Configuration.ClientSettings;
                if (character.CanLaunch)
                {
                    if (Mubox.View.Client.ClientWindowCollection.Instance.Count((dlg) => dlg.ClientState.Settings.Name.ToUpper().Equals(character.Name.ToUpper(), StringComparison.InvariantCultureIgnoreCase)) == 0)
                    {
                        Mubox.View.Client.ClientWindow clientWindow = new Mubox.View.Client.ClientWindow(new Mubox.Model.ClientState(character));
                        clientWindow.Show();
                    }
                }
            }
        }

        private static MenuItem CreateMouseBufferMenuItem(MenuItem menuItem, List<MenuItem> mouseClickBufferMenu, double time)
        {
            menuItem = new MenuItem();
            menuItem.IsCheckable = true;
            menuItem.IsChecked = Mubox.Configuration.MuboxConfigSection.Default.MouseBufferMilliseconds == time;
            menuItem.Click += (sender, e) =>
            {
                Mubox.Configuration.MuboxConfigSection.Default.MouseBufferMilliseconds = time;
                Mubox.Configuration.MuboxConfigSection.Default.Save();
            };
            menuItem.Header = time == 0.0 ? "Disabled" : ((int)time).ToString() + "ms";
            menuItem.ToolTip = "Use this option to set the Click Buffer to " + menuItem.Header;
            mouseClickBufferMenu.Add(menuItem);
            return menuItem;
        }

        public static void CreateServerUI()
        {
            try
            {
                if (Mubox.View.Server.ServerWindow.Instance != null)
                {
                    try
                    {
                        Mubox.View.Server.ServerWindow.Instance.Close();
                        Mubox.View.Server.ServerWindow.Instance = null;
                    }
                    catch (Exception ex)
                    {
                        Debug.WriteLine(ex.Message);
                        Debug.WriteLine(ex.StackTrace);
                    }
                }
                Mubox.View.Server.ServerWindow.Instance = new Mubox.View.Server.ServerWindow();
                Mubox.View.Server.ServerWindow.Instance.Closing += (L_s, L_e) =>
                {
                    Mubox.View.Server.ServerWindow.Instance = null;
                };
                Mubox.View.Server.ServerWindow.Instance.Show();
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex.Message);
                Debug.WriteLine(ex.StackTrace);
            }
        }

        private MenuItem QuickLaunchMenu_CreateClientItem(string clientName)
        {
            RoutedEventHandler clientStartEventHandler = (sender, e) =>
            {
                var clientSettings = Mubox.Configuration.MuboxConfigSection.Default.Teams.ActiveTeam.Clients.GetOrCreateNew(clientName);
                Mubox.Configuration.MuboxConfigSection.Default.Save();
                ClientState clientState = new ClientState(clientSettings);
                Mubox.View.Client.ClientWindow clientWindow = new Mubox.View.Client.ClientWindow(clientState);
                clientWindow.Show();
            };

            RoutedEventHandler clientDeleteEventHandler = (sender, e) =>
            {
                Mubox.Configuration.MuboxConfigSection.Default.Teams.ActiveTeam.Clients.Remove(clientName);
                Mubox.Configuration.MuboxConfigSection.Default.Save();
            };

            MenuItem clientMenuItem = new MenuItem();
            clientMenuItem.Header = clientName;
            // clientMenuItem.Click += clientLaunchEventHandler;

            MenuItem clientLaunchMenuItem = new MenuItem();
            clientLaunchMenuItem.Header = "_Start";
            clientLaunchMenuItem.Click += clientStartEventHandler;

            MenuItem clientDeleteMenuItem = new MenuItem();
            clientDeleteMenuItem.Header = "_Remove From List";
            clientDeleteMenuItem.Click += clientDeleteEventHandler;

            clientMenuItem.ItemsSource = new object[] {
                clientLaunchMenuItem,
                clientDeleteMenuItem
            };

            return clientMenuItem;
        }
    }
}