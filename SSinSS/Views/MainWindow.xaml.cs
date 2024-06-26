﻿using Microsoft.Win32;
using PROBot;
using PROProtocol;
using PROShine.Views;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Media;
using System.Windows.Input;
using System.Collections.ObjectModel;
using PROBot.Modules;
using System.Linq;

namespace PROShine
{
    public partial class MainWindow : Window
    {
        public BotClient Bot { get; private set; }

        public TeamView Team { get; private set; }
        public InventoryView Inventory { get; private set; }
        public ChatView Chat { get; private set; }
        public PlayersView Players { get; private set; }
        public MapView Map { get; private set; }
        public BattleView Battle { get; private set; }
        public static bool scriptProvided = false, loginable = false;
        public static Account account_ss { get;  set; }
        public static string filePath_ss { get; set; }

        private struct TabView
        {
            public UserControl View;
            public ContentControl Content;
            public ToggleButton Button;
        }
        private List<TabView> _views = new List<TabView>();

        public FileLogger FileLog { get; private set; }

        DateTime _refreshPlayers;
        int _refreshPlayersDelay;
        DateTime _lastQueueBreakPointTime;
        int? _lastQueueBreakPoint;

        private int _queuePosition;

        private ObservableCollection<OptionSlider> _sliderOptions;
        private ObservableCollection<TextOption> _textOptions;

        public MainWindow()
        {
#if !DEBUG
            AppDomain.CurrentDomain.UnhandledException += CurrentDomain_UnhandledException;
            TaskScheduler.UnobservedTaskException += TaskScheduler_UnobservedTaskException;
#endif
            Thread.CurrentThread.Name = "UI Thread";

            Bot = new BotClient();
            Bot.StateChanged += Bot_StateChanged;
            Bot.ClientChanged += Bot_ClientChanged;
            Bot.AutoReconnector.StateChanged += Bot_AutoReconnectorStateChanged;
            Bot.StaffAvoider.StateChanged += Bot_StaffAvoiderStateChanged;
            Bot.PokemonEvolver.StateChanged += Bot_PokemonEvolverStateChanged;
            Bot.ConnectionOpened += Bot_ConnectionOpened;
            Bot.ConnectionClosed += Bot_ConnectionClosed;
            Bot.MessageLogged += Bot_LogMessage;
            Bot.SliderCreated += Bot_SliderCreated;
            Bot.SliderRemoved += Bot_SliderRemoved;
            Bot.TextboxCreated += Bot_TextboxCreated;
            Bot.TextboxRemoved += Bot_TextboxRemoved;
            
            InitializeComponent();

            AutoEvolveSwitch.IsChecked = Bot.Settings.AutoEvolve;
            AvoidStaffSwitch.IsChecked = Bot.Settings.AvoidStaff;
            AutoReconnectSwitch.IsChecked = Bot.Settings.AutoReconnect;

            Bot.PokemonEvolver.IsEnabled = Bot.Settings.AutoEvolve;
            Bot.StaffAvoider.IsEnabled = Bot.Settings.AvoidStaff;
            Bot.AutoReconnector.IsEnabled = Bot.Settings.AutoReconnect;

            if (!string.IsNullOrEmpty(Bot.Settings.LastScript))
            {
                string fileName = Path.GetFileName(Bot.Settings.LastScript);
                MenuReloadScript.Header = "Reload " + fileName;
                MenuReloadScript.IsEnabled = true;
                MenuExploreScript.Header = "Explore " + fileName;
                MenuExploreScript.IsEnabled = true;
            }

            App.InitializeVersion();

            Team = new TeamView(Bot);
            Inventory = new InventoryView();
            Chat = new ChatView(Bot);
            Players = new PlayersView(Bot);
            Map = new MapView(Bot);
            Battle = new BattleView(Bot, this);

            FileLog = new FileLogger();

            _refreshPlayers = DateTime.UtcNow;
            _refreshPlayersDelay = 5000;

            AddView(Team, TeamContent, TeamButton, true);
            AddView(Inventory, InventoryContent, InventoryButton);
            AddView(Chat, ChatContent, ChatButton);
            AddView(Players, PlayersContent, PlayersButton);
            AddView(Map, MapContent, MapButton);
            AddView(Battle, BattleContent, BattleButton);

            SetTitle(null);

            LogMessage("Running " + App.Name + " by " + App.Author + ", version " + App.Version);
            if (App.IsBeta)
            {
                LogMessage("This is a BETA version. Bugs, crashes and bans might occur.");
                LogMessage("Report any problem on the forums and join the Discord chat for the latest information.");
            }

            Task.Run(() => UpdateClients());

            OptionSliders.ItemsSource = _sliderOptions = new ObservableCollection<OptionSlider>();
            TextOptions.ItemsSource = _textOptions = new ObservableCollection<TextOption>();


            if (App.Args.Length > 0)
            {
                StartByArgs();
            }
        }

        public async void StartByArgs()
        {
            
            string[] args = App.Args;//Environment.GetCommandLineArgs();
            account_ss = new Account("");
            filePath_ss = "";
            string help_text = "How to write the command\t\tConstraints (if any)\n1. Prefix -u followed by your PRO Username\t\tNo Constraints\n2. Prefix -p followed by your PRO Password\t\tNo Constraints\n3. Prefix -s followed by your PRO Server name\tEither \"silver\" or \"gold\"\n4. Prefix -ph followed by your Proxy host\t\tNo constraints\n5. Prefix -pt followed by your Proxy Port\t\tMust always be Numerical and non-zero\n6. Prefix -pu followed by your Proxy Username\tNo Constraints\n7. Prefix -pp followed by your Proxy Password\tNo Constraints\n8. Prefix -ver followed by your Proxy Version\t\tUse 4 or 5 for SOCKS4 and SOCKS5 Respectively"
                                  + "\nThe command should look like this if u are using a proxy server: <path of bot> -u <PRO username> -p <PRO Password> -s <Server name> -ph <Proxy host> -pt <Proxy port> -pu <proxy username> -pp <proxy password> -ver <Proxy version>"
                                  + "\nIt is to be noted that if u are not using proxy, u just need to provide your PRO Username, PRO Password, and PRO server";

            if (args.Contains("-help") || args.Contains("-h") || (args.Length < 6 && args.Length > 0))
            {
                LogMessage(help_text);
            }

            else if (args.Length == 6)
            {
                bool DevIDGen = true;
                loginable = true;
                for (int i = 0; i < args.Length; i++)
                {
                    if (args[i] == "-u")
                    {
                        account_ss.Name = args[i + 1];
                    }
                    else if (args[i] == "-p")
                    {
                        account_ss.Password = args[i + 1];
                    }
                    else if (args[i] == "-s")
                    {
                        account_ss.Server = args[i + 1];
                    }
                    if (Guid.TryParse((HardwareHash.GenerateRandom().ToString()).Trim(), out Guid deviceId) && DevIDGen == true)
                    {
                        account_ss.DeviceId = deviceId;
                        DevIDGen = false;
                    }
                }
            }
            else if (args.Length > 6)
            {
                bool DevIDGen = true;
                loginable = true;
                for (int i = 0; i < args.Length; i++)
                {
                    if (args[i] == "-u")
                    {
                        account_ss.Name = args[i + 1];
                    }
                    else if (args[i] == "-p")
                    {
                        account_ss.Password = args[i + 1];
                    }
                    else if (args[i] == "-s")
                    {
                        account_ss.Server = args[i + 1].Trim().ToUpperInvariant();
                    }
                    else if (args[i] == "-ph")
                    {
                        account_ss.Socks.Host = args[i + 1];
                    }
                    else if (args[i] == "-pt")
                    {
                        try
                        {
                            account_ss.Socks.Port = int.Parse(args[i + 1]);
                        }
                        catch (Exception ex) { }
                    }
                    else if (args[i] == "-pu")
                    {
                        account_ss.Socks.Username = args[i + 1];
                    }
                    else if (args[i] == "-pp")
                    {
                        account_ss.Socks.Password = args[i + 1];
                    }
                    if (Guid.TryParse((HardwareHash.GenerateRandom().ToString()).Trim(), out Guid deviceId) && DevIDGen == true)
                    {
                        account_ss.DeviceId = deviceId;
                        DevIDGen = false;
                    }
                    else if (args[i] == "-ver")
                    {
                        account_ss.Socks.Version = (SocksVersion)int.Parse(args[i + 1]);
                    }
                    else if (args[i] == "-path")
                    {
                        filePath_ss = args[i + 1].Trim();
                        scriptProvided = true;
                    }
                }
            }
            //-----------------------------------------------
            if (loginable)
            {                
                Bot.Login(account_ss);
                loginable = false;
                
            }
            //-----------------------------------------------
        }

        public async void StartByScript()
        {
            if (Bot.Game != null && scriptProvided)
            {
                //await Task.Delay(10000);
                scriptProvided = false;
                Bot.Start();
                
            }
            else if(scriptProvided)
            {
                //Bot.Update();
            }

        }

        public void Bot_SliderRemoved(OptionSlider option)
        {
            Dispatcher.InvokeAsync(delegate
            {
                if (_sliderOptions.Count == 1 && _textOptions.Count == 0)
                {
                    OptionsButton.Content = "Show Options";
                    OptionsButton.Visibility = Visibility.Collapsed;
                    OptionSliders.Visibility = Visibility.Collapsed;
                    TextOptions.Visibility = Visibility.Collapsed;
                }

                _sliderOptions.Remove(option);
                OptionSliders.Items.Refresh();
            });
        }

        public void Bot_TextboxRemoved(TextOption option)
        {
            Dispatcher.InvokeAsync(delegate
            {
                if (_textOptions.Count == 1 && _sliderOptions.Count == 0)
                {
                    OptionsButton.Content = "Show Options";
                    OptionsButton.Visibility = Visibility.Collapsed;
                    OptionSliders.Visibility = Visibility.Collapsed;
                    TextOptions.Visibility = Visibility.Collapsed;
                }

                _textOptions.Remove(option);
                TextOptions.Items.Refresh();
            });
        }

        private void Options_Click(object sender, RoutedEventArgs e)
        {
            if (OptionSliders.Visibility == Visibility.Collapsed)
            {
                OptionsButton.Content = "Hide Options";
                OptionSliders.Visibility = Visibility.Visible;
                TextOptions.Visibility = Visibility.Visible;
            }
            else
            {
                OptionsButton.Content = "Show Options";
                OptionSliders.Visibility = Visibility.Collapsed;
                TextOptions.Visibility = Visibility.Collapsed;
            }
        }

        private void TextBox_KeyDown(object sender, KeyEventArgs e)
        {
            // On pressing enter, take focus from the textbox and give it to the selected button in _views
            // This is necessary to update the content of the TextOption
            if (e.Key == Key.Enter || e.Key == Key.Return)
                foreach (TabView view in _views)
                    if (view.Button.IsChecked.Value)
                        Keyboard.Focus(view.Button);
        }

        public void Bot_TextboxCreated(TextOption option)
        {
            Dispatcher.InvokeAsync(delegate
            {
                OptionsButton.Visibility = Visibility.Visible;
                _textOptions.Add(option);
                TextOptions.Items.Refresh();
            });
        }

        public void Bot_SliderCreated(OptionSlider option)
        {
            Dispatcher.InvokeAsync(delegate
            {
                OptionsButton.Visibility = Visibility.Visible;
                _sliderOptions.Add(option);
                OptionSliders.Items.Refresh();
            });
        }

        private void AddView(UserControl view, ContentControl content, ToggleButton button, bool visible = false)
        {
            _views.Add(new TabView
            {
                View = view,
                Content = content,
                Button = button
            });
            content.Content = view;
            if (visible)
            {
                content.Visibility = Visibility.Visible;
                button.IsChecked = true;
            }
            else
            {
                content.Visibility = Visibility.Collapsed;
            }
            button.Click += ViewButton_Click;
        }

        private void ViewButton_Click(object sender, RoutedEventArgs e)
        {
            foreach (TabView view in _views)
            {
                if (view.Button == sender)
                {
                    view.Content.Visibility = Visibility.Visible;
                    view.Button.IsChecked = true;
                    _refreshPlayersDelay = view.View == Players ? 200 : 5000;
                }
                else
                {
                    view.Content.Visibility = Visibility.Collapsed;
                    view.Button.IsChecked = false;
                }
            }
        }

        private void SetTitle(string username)
        {
            Title = username == null ? "" : username + " - ";
            Title += App.Name + " " + App.Version;
#if DEBUG
            Title += " (debug)";
#endif
        }

        private void TaskScheduler_UnobservedTaskException(object sender, UnobservedTaskExceptionEventArgs e)
        {
            Dispatcher.InvokeAsync(() => HandleUnhandledException(e.Exception.InnerException));
        }

        private void CurrentDomain_UnhandledException(object sender, UnhandledExceptionEventArgs e)
        {
            HandleUnhandledException(e.ExceptionObject as Exception);
        }

        private void HandleUnhandledException(Exception ex)
        {
            try
            {
                if (ex != null)
                {
                    File.WriteAllText("crash_" + DateTime.Now.ToString("yyyy-MM-dd_HH-mm-ss") + ".txt",
                        App.Name + " " + App.Version + " crash report: " + Environment.NewLine + ex);
                }
                MessageBox.Show(App.Name + " encountered a fatal error. The application will now terminate." + Environment.NewLine +
                    "An error file has been created next to the application.", App.Name + " - Fatal error", MessageBoxButton.OK, MessageBoxImage.Error);
                Environment.Exit(0);
            }
            catch
            {
            }
        }

        private void UpdateClients()
        {
            lock (Bot)
            {
                if (Bot.Game != null)
                {
                    Bot.Game.Update();                                   
                }
                Bot.Update();
            }
            Battle.UpdateBattleHUD();
            Task.Delay(1).ContinueWith((previous) => UpdateClients());
        }

        private void LoginMenuItem_Click(object sender, RoutedEventArgs e)
        {
            OpenLoginWindow();
        }

        private void OpenLoginWindow()
        {
            LoginWindow login = new LoginWindow(Bot) { Owner = this };
            bool? result = login.ShowDialog();
            if (result != true)
            {
                return;
            }

            LogMessage("Connecting to the server...");
            LoginButton.IsEnabled = false;
            LoginMenuItem.IsEnabled = false;
            Account account = new Account(login.Username);
            lock (Bot)
            {
                account.Password = login.Password;
                account.Server = login.Server;
                account.DeviceId = login.DeviceId;
                if (login.HasProxy)
                {
                    account.Socks.Version = (SocksVersion)login.ProxyVersion;
                    account.Socks.Host = login.ProxyHost;
                    account.Socks.Port = login.ProxyPort;
                    account.Socks.Username = login.ProxyUsername;
                    account.Socks.Password = login.ProxyPassword;
                }
                LogMessage("Device Id: " + account.DeviceId);
                LogMessage("Proxy Version: " + account.Socks.Version);
                LogMessage("Proxy Host: " + account.Socks.Host);
                LogMessage("Proxy Port: " + account.Socks.Port);
                Bot.Login(account);
            }
        }

        private void LogoutMenuItem_Click(object sender, RoutedEventArgs e)
        {
            Logout();
        }

        private void Logout()
        {
            LogMessage("Logging out...");
            lock (Bot)
            {   
                Bot.Logout(false);
            }
        }

        private void MenuPathScript_Click(object sender, RoutedEventArgs e)
        {
            LoadScript();
        }

        private void LoadScript(string filePath = null)
        {
            if (filePath == null)
            {
                OpenFileDialog openDialog = new OpenFileDialog
                {
                    Filter = App.Name + " Scripts|*.lua;*.txt|All Files|*.*"
                };
                bool? result = openDialog.ShowDialog();

                if (!(result.HasValue && result.Value))
                    return;

                filePath = openDialog.FileName;
            }

            try
            {
                lock (Bot)
                {
                    Bot.Settings.LastScript = filePath;
                    MenuReloadScript.Header = "Reload " + Path.GetFileName(filePath);
                    MenuReloadScript.IsEnabled = true;
                    MenuExploreScript.Header = "Explore " + Path.GetFileName(filePath);
                    MenuExploreScript.IsEnabled = true;
                    Bot.SliderOptions.Clear();
                    Bot.TextOptions.Clear();
                    _sliderOptions.Clear();
                    _textOptions.Clear();
                    OptionSliders.Items.Refresh();
                    TextOptions.Items.Refresh();
                    OptionsButton.Content = "Show Options";
                    OptionsButton.Visibility = Visibility.Collapsed;
                    OptionSliders.Visibility = Visibility.Collapsed;
                    TextOptions.Visibility = Visibility.Collapsed;

                    Bot.LoadScript(filePath);
                    MenuPathScript.Header =
                        "Script: \"" + Bot.Script.Name + "\"" + Environment.NewLine + filePath;
                    LogMessage("Script \"{0}\" by \"{1}\" successfully loaded", Bot.Script.Name, Bot.Script.Author);
                    if (!string.IsNullOrEmpty(Bot.Script.Description))
                    {
                        LogMessage(Bot.Script.Description);
                    }
                    UpdateBotMenu();
                }
            }
            catch (Exception ex)
            {
                string filename = Path.GetFileName(filePath);
#if DEBUG
                LogMessage("Could not load script {0}: " + Environment.NewLine + "{1}", filename, ex);
#else
                LogMessage("Could not load script {0}: " + Environment.NewLine + "{1}", filename, ex.Message);
#endif
            }
        }

        private void BotStartMenuItem_Click(object sender, RoutedEventArgs e)
        {
            lock (Bot)
            {
                Bot.Start();
            }
        }

        private void BotStopMenuItem_Click(object sender, RoutedEventArgs e)
        {
            lock (Bot)
            {
                Bot.Stop();
            }
        }

        private void Client_PlayerAdded(PlayerInfos player)
        {
            if (_refreshPlayers < DateTime.UtcNow)
            {
                Dispatcher.InvokeAsync(delegate
                {
                    Players.RefreshView();
                });
                _refreshPlayers = DateTime.UtcNow.AddMilliseconds(_refreshPlayersDelay);
            }
        }

        private void Client_PlayerUpdated(PlayerInfos player)
        {
            if (_refreshPlayers < DateTime.UtcNow)
            {
                Dispatcher.InvokeAsync(delegate
                {
                    Players.RefreshView();
                });
                _refreshPlayers = DateTime.UtcNow.AddMilliseconds(_refreshPlayersDelay);
            }
        }

        private void Client_PlayerRemoved(PlayerInfos player)
        {
            if (_refreshPlayers < DateTime.UtcNow)
            {
                Dispatcher.InvokeAsync(delegate
                {
                    Players.RefreshView();
                });
                _refreshPlayers = DateTime.UtcNow.AddMilliseconds(_refreshPlayersDelay);
            }
        }

        private void Bot_ConnectionOpened()
        {
            Dispatcher.InvokeAsync(delegate
            {
                lock (Bot)
                {
                    if (Bot.Game != null)
                    {
                        SetTitle(Bot.Account.Name + " - " + Bot.Game.Server);
                        UpdateBotMenu();
                        LogoutMenuItem.IsEnabled = true;
                        LoginButton.IsEnabled = true;
                        LoginButtonIcon.Icon = FontAwesome.WPF.FontAwesomeIcon.SignOut;
                        LogMessage("Connected, authenticating...");
                        
                    }
                }
            });
        }

        private void Bot_ConnectionClosed()
        {
            Dispatcher.InvokeAsync(delegate
            {
                _lastQueueBreakPoint = null;
                LoginMenuItem.IsEnabled = true;
                LogoutMenuItem.IsEnabled = false;
                LoginButton.IsEnabled = true;
                LoginButtonIcon.Icon = FontAwesome.WPF.FontAwesomeIcon.SignIn;
                UpdateBotMenu();
                StatusText.Text = "Offline";
                StatusText.Foreground = Brushes.DarkRed;
                Players.ClearList();
            });
        }

        private void Client_LoggedIn()
        {
            Dispatcher.InvokeAsync(delegate
            {
                _lastQueueBreakPoint = null;
                LogMessage("Authenticated successfully!");
                lock (Bot)
                {
                    if (Bot.Game != null && Bot.Game.IsCreatingNewCharacter)
                    {
                        LogMessage("This is a new account, with no character. A random character will be created if you start the bot.");
                    }
                }
                UpdateBotMenu();
                StatusText.Text = "Online";
                StatusText.Foreground = Brushes.DarkGreen;
                
            });
        }

        private void Client_AuthenticationFailed(AuthenticationResult reason)
        {
            Dispatcher.InvokeAsync(delegate
            {
                string message = "";
                switch (reason)
                {
                    case AuthenticationResult.AlreadyLogged:
                        message = "Already logged in";
                        break;
                    case AuthenticationResult.Banned:
                        message = "You are banned from PRO";
                        break;
                    case AuthenticationResult.EmailNotActivated:
                        message = "Email not activated";
                        break;
                    case AuthenticationResult.InvalidPassword:
                        message = "Invalid password";
                        break;
                    case AuthenticationResult.InvalidUser:
                        message = "Invalid username";
                        break;
                    case AuthenticationResult.InvalidVersion:
                        message = "Outdated client, please wait for an update";
                        break;
                    case AuthenticationResult.Locked:
                    case AuthenticationResult.Locked2:
                        message = "Server locked for maintenance";
                        break;
                    case AuthenticationResult.OtherServer:
                        message = "Already logged in on another server";
                        break;
                }
                LogMessage("Authentication failed: " + message);
            });
        }

        private void Bot_StateChanged(BotClient.State state)
        {
            Dispatcher.InvokeAsync(delegate
            {
                UpdateBotMenu();
                string stateText;
                if (BotClient.State.Started == state)
                {
                    stateText = "started";
                    StartScriptButtonIcon.Icon = FontAwesome.WPF.FontAwesomeIcon.Pause;
                }
                else if (BotClient.State.Paused == state)
                {
                    stateText = "paused";
                    StartScriptButtonIcon.Icon = FontAwesome.WPF.FontAwesomeIcon.Play;
                }
                else
                {
                    stateText = "stopped";
                    StartScriptButtonIcon.Icon = FontAwesome.WPF.FontAwesomeIcon.Play;
                }
                LogMessage("Bot " + stateText);
            });
        }

        private void Bot_LogMessage(string message)
        {
            Dispatcher.InvokeAsync(delegate
            {
                LogMessage(message);
            });
        }

        private void Bot_AutoReconnectorStateChanged(bool value)
        {
            Dispatcher.InvokeAsync(delegate
            {
                Bot.Settings.AutoReconnect = value;
                if (AutoReconnectSwitch.IsChecked == value) return;
                AutoReconnectSwitch.IsChecked = value;
            });
        }

        private void Bot_StaffAvoiderStateChanged(bool value)
        {
            Dispatcher.InvokeAsync(delegate
            {
                Bot.Settings.AvoidStaff = value;
                if (AvoidStaffSwitch.IsChecked == value) return;
                AvoidStaffSwitch.IsChecked = value;
            });
        }

        private void Bot_PokemonEvolverStateChanged(bool value)
        {
            Dispatcher.InvokeAsync(delegate
            {
                Bot.Settings.AutoEvolve = value;
                if (AutoEvolveSwitch.IsChecked == value) return;
                AutoEvolveSwitch.IsChecked = value;
            });
        }

        private void Bot_ClientChanged()
        {
            lock (Bot)
            {
                if (Bot.Game != null)
                {
                    Bot.Game.LoggedIn += Client_LoggedIn;
                    Bot.Game.AuthenticationFailed += Client_AuthenticationFailed;
                    Bot.Game.QueueUpdated += Client_QueueUpdated;
                    Bot.Game.PositionUpdated += Client_PositionUpdated;
                    Bot.Game.PokemonsUpdated += Client_PokemonsUpdated;
                    Bot.Game.InventoryUpdated += Client_InventoryUpdated;
                    Bot.Game.BattleStarted += Client_BattleStarted;
                    Bot.Game.BattleMessage += Client_BattleMessage;
                    Bot.Game.BattleEnded += Client_BattleEnded;
                    Bot.Game.DialogOpened += Client_DialogOpened;
                    Bot.Game.ChatMessage += Chat.Client_ChatMessage;
                    Bot.Game.ChannelMessage += Chat.Client_ChannelMessage;
                    Bot.Game.EmoteMessage += Chat.Client_EmoteMessage;
                    Bot.Game.ChannelSystemMessage += Chat.Client_ChannelSystemMessage;
                    Bot.Game.ChannelPrivateMessage += Chat.Client_ChannelPrivateMessage;
                    Bot.Game.PrivateMessage += Chat.Client_PrivateMessage;
                    Bot.Game.LeavePrivateMessage += Chat.Client_LeavePrivateMessage;
                    Bot.Game.RefreshChannelList += Chat.Client_RefreshChannelList;
                    Bot.Game.SystemMessage += Client_SystemMessage;
                    Bot.Game.PlayerAdded += Client_PlayerAdded;
                    Bot.Game.PlayerUpdated += Client_PlayerUpdated;
                    Bot.Game.PlayerRemoved += Client_PlayerRemoved;
                    Bot.Game.InvalidPacket += Client_InvalidPacket;
                    Bot.Game.PokeTimeUpdated += Client_PokeTimeUpdated;
                    Bot.Game.ShopOpened += Client_ShopOpened;
                    Bot.Game.MoveRelearnerOpened += Client_MoveRelearnerOpened;
                    Bot.Game.MapLoaded += Map.Client_MapLoaded;
                    Bot.Game.PositionUpdated += Map.Client_PositionUpdated;
                    Bot.Game.PlayerAdded += Map.Client_PlayerEnteredMap;
                    Bot.Game.PlayerRemoved += Map.Client_PlayerLeftMap;
                    Bot.Game.PlayerUpdated += Map.Client_PlayerMoved;
                    Bot.Game.NpcReceived += Map.Client_NpcReceived;
                    Bot.Game.BattleUpdated += Battle.BattleUpdated;
                    Bot.Game.BattleStarted += Battle.BattleStarted;
                    Bot.Game.BattleEnded += Battle.BattleEnded;
                    Bot.Game.ActivePokemonChanged += Battle.ActivePokemonChanged;
                    Bot.Game.OpponentChanged += Battle.OpponentChanged;
                    Bot.ConnectionClosed += Battle.ConnectionClosed;
                }
            }
            Dispatcher.InvokeAsync(delegate
            {
                if (Bot.Game != null)
                {
                    FileLog.OpenFile(Bot.Account.Name, Bot.Game.Server.ToString());
                }
                else
                {
                    FileLog.CloseFile();
                }
            });
        }

        private void Client_QueueUpdated(int position)
        {
            Dispatcher.InvokeAsync(delegate
            {
                if (_queuePosition != position)
                {
                    _queuePosition = position;
                    TimeSpan? queueTimeLeft = null;
                    if (_lastQueueBreakPoint != null && position < _lastQueueBreakPoint)
                    {
                        queueTimeLeft = TimeSpan.FromTicks((DateTime.UtcNow - _lastQueueBreakPointTime).Ticks / (_lastQueueBreakPoint.Value - position) * position);
                    }
                    StatusText.Text = "In Queue" + " (" + position + ")";
                    if (queueTimeLeft != null)
                    {
                        StatusText.Text += " ";
                        if (queueTimeLeft.Value.Hours > 0)
                        {
                            StatusText.Text += queueTimeLeft.Value.ToString(@"hh\:mm\:ss");
                        }
                        else
                        {
                            StatusText.Text += queueTimeLeft.Value.ToString(@"mm\:ss");

                        }
                        StatusText.Text += " left";
                    }
                    StatusText.Foreground = Brushes.DarkBlue;
                    if (_lastQueueBreakPoint == null)
                    {
                        _lastQueueBreakPoint = position;
                        _lastQueueBreakPointTime = DateTime.UtcNow;
                    }
                }
            });
        }

        private bool HasItem(string itemName)
        {
            return Bot.Game.HasItemName(itemName.ToUpperInvariant());
        }

        private void Client_PositionUpdated(string map, int x, int y)
        {
            Dispatcher.InvokeAsync(delegate
            {
                try
                {
                    string pdseen, pdown, pdevo;
                    int all_badge = 0, kanto = 0, johto = 0, hoenn = 0, sinnoh = 0;
                    // Kanto Badges-------------------------------------------------------------------------
                    if (HasItem("Boulder Badge"))
                    {
                        all_badge += 1;
                        kanto += 1;
                    }
                    if (HasItem("Cascade Badge"))
                    {
                        all_badge += 1;
                        kanto += 1;
                    }
                    if (HasItem("Thunder Badge"))
                    {
                        all_badge += 1;
                        kanto += 1;
                    }
                    if (HasItem("Rainbow Badge"))
                    {
                        all_badge += 1;
                        kanto += 1;
                    }
                    if (HasItem("Soul Badge"))
                    {
                        all_badge += 1;
                        kanto += 1;
                    }
                    if (HasItem("Marsh Badge"))
                    {
                        all_badge += 1;
                        kanto += 1;
                    }
                    if (HasItem("Volcano Badge"))
                    {
                        all_badge += 1;
                        kanto += 1;
                    }
                    if (HasItem("Earth Badge"))
                    {
                        all_badge += 1;
                        kanto += 1;
                    }//------------------------------------------------------------

                    // Johto Badges-------------------------------------------------------------------------
                    if (HasItem("Zephyr Badge"))
                    {
                        all_badge += 1;
                        johto += 1;
                    }
                    if (HasItem("Hive Badge"))
                    {
                        all_badge += 1;
                        johto += 1;
                    }
                    if (HasItem("Plain Badge"))
                    {
                        all_badge += 1;
                        johto += 1;
                    }
                    if (HasItem("Fog Badge"))
                    {
                        all_badge += 1;
                        johto += 1;
                    }
                    if (HasItem("Storm Badge"))
                    {
                        all_badge += 1;
                        johto += 1;
                    }
                    if (HasItem("Mineral Badge"))
                    {
                        all_badge += 1;
                        johto += 1;
                    }
                    if (HasItem("Glacier Badge"))
                    {
                        all_badge += 1;
                        johto += 1;
                    }
                    if (HasItem("Rising Badge"))
                    {
                        all_badge += 1;
                        johto += 1;
                    }//------------------------------------------------------------------------

                    // Hoenn Badges-------------------------------------------------------------------------
                    if (HasItem("Stone Badge"))
                    {
                        all_badge += 1;
                        hoenn += 1;
                    }
                    if (HasItem("Knuckle Badge"))
                    {
                        all_badge += 1;
                        hoenn += 1;
                    }
                    if (HasItem("Dynamo Badge"))
                    {
                        all_badge += 1;
                        hoenn += 1;
                    }
                    if (HasItem("Heat Badge"))
                    {
                        all_badge += 1;
                        hoenn += 1;
                    }
                    if (HasItem("Balance Badge"))
                    {
                        all_badge += 1;
                        hoenn += 1;
                    }
                    if (HasItem("Feather Badge"))
                    {
                        all_badge += 1;
                        hoenn += 1;
                    }
                    if (HasItem("Mind Badge"))
                    {
                        all_badge += 1;
                        hoenn += 1;
                    }
                    if (HasItem("Rain Badge"))
                    {
                        all_badge += 1;
                        hoenn += 1;
                    }//---------------------------------------------------------------------------

                    // Sinnoh Badges-------------------------------------------------------------------------
                    if (HasItem("Coal Badge"))
                    {
                        all_badge += 1;
                        sinnoh += 1;
                    }
                    if (HasItem("Forest Badge"))
                    {
                        all_badge += 1;
                        sinnoh += 1;
                    }
                    if (HasItem("Cobble Badge"))
                    {
                        all_badge += 1;
                        sinnoh += 1;
                    }
                    if (HasItem("Fen Badge"))
                    {
                        all_badge += 1;
                        sinnoh += 1;
                    }
                    if (HasItem("Relic Badge"))
                    {
                        all_badge += 1;
                        sinnoh += 1;
                    }
                    if (HasItem("Mine Badge"))
                    {
                        all_badge += 1;
                        sinnoh += 1;
                    }
                    if (HasItem("Icicle Badge"))
                    {
                        all_badge += 1;
                        sinnoh += 1;
                    }
                    if (HasItem("Beacon Badge"))
                    {
                        all_badge += 1;
                        sinnoh += 1;
                    }//-------------------------------------------------------------------------


                    BadgeText.Text = all_badge.ToString();
                    BadgeText.ToolTip = "Total Badges: " + all_badge.ToString() + "\nKanto: " + kanto + "\nJohto: " + johto + "\nHoenn: " + hoenn + "\nSinnoh: " + sinnoh;
                    BadgeIcon.ToolTip = "Total Badges: " + all_badge.ToString() + "\nKanto: " + kanto + "\nJohto: " + johto + "\nHoenn: " + hoenn + "\nSinnoh: " + sinnoh;


                    pdseen = Bot.Game.PokedexSeen.ToString();
                    pdown = Bot.Game.PokedexOwned.ToString();
                    pdevo = Bot.Game.PokedexEvolved.ToString();

                    PDSeenText.Text = pdseen;
                    PDOwnText.Text = pdown;
                    PDEvoText.Text = pdevo;

                    MapNameText.Text = map;
                    PlayerPositionText.Text = "(" + x + "," + y + ")";
                }
                catch(Exception ex)
                {
                    LogMessage(ex.ToString());
                }
                
            });
        }

        private void Client_PokemonsUpdated()
        {
            Dispatcher.InvokeAsync(delegate
            {
                IList<Pokemon> team;
                lock (Bot)
                {
                    team = Bot.Game.Team.ToArray();
                    if (Bot.Game.IsMember)
                    {
                        MemberText.Text = "1";
                        MemberText.ToolTip = "Membership: Activated";
                        MemberIcon.ToolTip = "Membership: Activated";
                        MemberText.Foreground = Brushes.DarkGreen;
                        MemberIcon.Foreground = Brushes.DarkGreen;
                    }
                    else
                    {
                        MemberText.Text = "0";
                        MemberText.ToolTip = "Membership: Expired!";
                        MemberIcon.ToolTip = "Membership: Expired!";
                        MemberText.Foreground = Brushes.Red;
                        MemberIcon.Foreground = Brushes.Red;
                    }
                }
                Team.PokemonsListView.ItemsSource = team;
                Team.PokemonsListView.Items.Refresh();
                
            });
        }

        private void Client_InventoryUpdated()
        {
            Dispatcher.InvokeAsync(delegate
            {
                string money;
                IList<InventoryItem> items;
                lock (Bot)
                {
                    money = Bot.Game.Money.ToString("#,##0");
                    items = Bot.Game.Items.ToArray();
                }
                MoneyText.Text = money;
                MoneyText.FontWeight = FontWeights.Bold;
                MoneyText.Foreground = Brushes.Blue;
                MoneyIcon.Foreground = Brushes.Blue;
                Inventory.ItemsListView.ItemsSource = items;
                Inventory.ItemsListView.Items.Refresh();
                if (scriptProvided)
                {
                    //await Task.Delay(5000);
                    //Thread.Sleep(5000);
                    LoadScript(filePath_ss);
                    //await Task.Delay(1000);
                    Task.Run(() => StartByScript());
                }
            });
        }

        private void Client_BattleStarted()
        {
            Dispatcher.InvokeAsync(delegate
            {
                StatusText.Text = "In battle";
                StatusText.Foreground = Brushes.Blue;
            });
        }

        private void Client_BattleMessage(string message)
        {
            Dispatcher.InvokeAsync(delegate
            {
                message = Regex.Replace(message, @"\[.+?\]", "");
                LogMessage(message);
            });
        }

        private void Client_BattleEnded()
        {
            Dispatcher.InvokeAsync(delegate
            {
                StatusText.Text = "Online";
                StatusText.Foreground = Brushes.DarkGreen;
            });
        }

        private void Client_DialogOpened(string message, string[] options)
        {
            Dispatcher.InvokeAsync(delegate
            {
                var content = new StringBuilder(message);
                for (int i = 0; i < options.Length; i++)
                {
                    content.AppendLine()
                        .Append(i + 1)
                        .Append(". ")
                        .Append(options[i]);
                }
                LogMessage(content.ToString());
            });
        }

        private void Client_SystemMessage(string message)
        {
            Dispatcher.InvokeAsync(delegate
            {
                AddSystemMessage(message);
            });
        }

        private void Client_InvalidPacket(string packet, string error)
        {
            Dispatcher.InvokeAsync(delegate
            {
                LogMessage("Received Invalid Packet: " + error + ": " + packet);
            });
        }

        private bool IsMorning()
        {
            DateTime dt = Convert.ToDateTime(Bot.Game.PokemonTime);
            if (dt.Hour >= 4 && dt.Hour < 10)
            {
                return true;
            }
            return false;
        }

        private bool IsNoon()
        {
            DateTime dt = Convert.ToDateTime(Bot.Game.PokemonTime);
            if (dt.Hour >= 10 && dt.Hour < 20)
            {
                return true;
            }
            return false;
        }

        private bool IsNight()
        {
            DateTime dt = Convert.ToDateTime(Bot.Game.PokemonTime);
            if (dt.Hour >= 20 || dt.Hour < 4)
            {
                return true;
            }
            return false;
        }

        private void Client_PokeTimeUpdated(string pokeTime, string weather)
        {
            Dispatcher.InvokeAsync(delegate
            {
                
                if (IsNight())
                {
                    PokeTimeText.Text = pokeTime + " (" + "Night" + ")";
                }
                else if (IsNoon())
                {
                    PokeTimeText.Text = pokeTime + " (" + "Noon" + ")";
                }
                else if (IsMorning())
                {
                    PokeTimeText.Text = pokeTime + " (" + "Morning" + ")";
                }
                else
                {
                    PokeTimeText.Text = pokeTime + " ( " + "UNKNOWN" + " )";
                }

            });
        }

        private void Client_ShopOpened(Shop shop)
        {
            Dispatcher.InvokeAsync(delegate
            {
                StringBuilder content = new StringBuilder();
                content.Append("Shop opened:");
                foreach (ShopItem item in shop.Items)
                {
                    content.AppendLine();
                    content.Append(item.Name);
                    content.Append(" ($").Append(item.Price).Append(')');
                }
                LogMessage(content.ToString());
            });
        }

        private void Client_MoveRelearnerOpened(MoveRelearner moveRelearnManager)
        {
            Dispatcher.InvokeAsync(delegate
            {
                StringBuilder content = new StringBuilder();
                content.Append("Move relearner:");
                foreach (MovesManager.MoveData move in moveRelearnManager.Moves)
                {
                    content.AppendLine();
                    content.Append(MovesManager.Instance.GetTrueName(move.Name));
                    content.Append(" ($2000)");
                }
                LogMessage(content.ToString());
            });
        }

        private void UpdateBotMenu()
        {
            lock (Bot)
            {
                BotStartMenuItem.IsEnabled = Bot.Game != null && Bot.Game.IsConnected && Bot.Script != null && Bot.Running == BotClient.State.Stopped;
                BotStopMenuItem.IsEnabled = Bot.Game != null && Bot.Game.IsConnected && Bot.Running != BotClient.State.Stopped;
            }
        }
        
        private void LogMessage(string message)
        {
            message = "[" + DateTime.Now.ToLongTimeString() + "] " + message;
            AppendLineToTextBox(MessageTextBox, message);
            FileLog.Append(message);
        }

        private void LogMessage(string format, params object[] args)
        {
            LogMessage(string.Format(format, args));
        }

        private void AddSystemMessage(string message)
        {
            LogMessage("System: " + message);
        }

        public static void AppendLineToTextBox(TextBox textBox, string message)
        {
            textBox.AppendText(message + Environment.NewLine);
            if (textBox.Text.Length > 12000)
            {
                string text = textBox.Text;
                text = text.Substring(text.Length - 10000, 10000);
                int index = text.IndexOf(Environment.NewLine);
                if (index != -1)
                {
                    text = text.Substring(index + Environment.NewLine.Length);
                }
                textBox.Text = text;
            }
            textBox.CaretIndex = textBox.Text.Length;
            textBox.ScrollToEnd();
        }

        private void MenuAbout_Click(object sender, RoutedEventArgs e)
        {
            MessageBox.Show(App.Name + " version " + App.Version + ", by " + App.Author + "." + Environment.NewLine + App.Description, App.Name + " - About");
        }

        private void MenuForum_Click(object sender, RoutedEventArgs e)
        {
            Process.Start("https://proshine-bot.com/");
        }

        private void MenuDiscord_Click(object sender, RoutedEventArgs e)
        {
            Process.Start("https://discord.gg/0t8HE2IMuqUTour9");
        }

        private void MenuGitHub_Click(object sender, RoutedEventArgs e)
        {
            Process.Start("https://github.com/Silv3rPRO/proshine");
        }

        private void MenuDonate_Click(object sender, RoutedEventArgs e)
        {
            Process.Start("https://www.patreon.com/proshine");
        }

        private void StartScriptButton_Click(object sender, RoutedEventArgs e)
        {
            lock (Bot)
            {
                if (Bot.Running == BotClient.State.Stopped)
                {
                    Bot.Start();
                }
                else if (Bot.Running == BotClient.State.Started || Bot.Running == BotClient.State.Paused)
                {
                    Bot.Pause();
                }
            }
        }

        private void StopScriptButton_Click(object sender, RoutedEventArgs e)
        {
            lock (Bot)
            {
                Bot.Stop();
            }
        }

        private void LoadScriptButton_Click(object sender, RoutedEventArgs e)
        {
            LoadScript();
        }

        private void AutoEvolveSwitch_Checked(object sender, RoutedEventArgs e)
        {
            lock (Bot)
            {
                Bot.PokemonEvolver.IsEnabled = true;
            }
        }

        private void AutoEvolveSwitch_Unchecked(object sender, RoutedEventArgs e)
        {
            lock (Bot)
            {
                Bot.PokemonEvolver.IsEnabled = false;
            }
        }

        private void AvoidStaffSwitch_Checked(object sender, RoutedEventArgs e)
        {
            lock (Bot)
            {
                Bot.StaffAvoider.IsEnabled = true;
            }
        }

        private void AvoidStaffSwitch_Unchecked(object sender, RoutedEventArgs e)
        {
            lock (Bot)
            {
                Bot.StaffAvoider.IsEnabled = false;
            }
        }

        private void AutoReconnectSwitch_Checked(object sender, RoutedEventArgs e)
        {
            lock (Bot)
            {
                Bot.AutoReconnector.IsEnabled = true;
            }
        }

        private void AutoReconnectSwitch_Unchecked(object sender, RoutedEventArgs e)
        {
            lock (Bot)
            {
                Bot.AutoReconnector.IsEnabled = false;
            }
        }

        private void PokePokedexButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                Bot.Game.SendPokedexRequest();
                string pokedexdata = "Total Pokemon\nKanto: " + Bot.Game.KantoAllPoke.ToString().PadRight(8) + "Johto: " + Bot.Game.JohtoAllPoke.ToString() + "\nHoenn: " + Bot.Game.HoennAllPoke.ToString().PadRight(8) + "Sinnoh: " + Bot.Game.SinnohAllPoke.ToString().PadRight(8) + "Others: " + Bot.Game.OtherAllPoke.ToString()
                                     + "\n\nKanto Pokemon\nSeen: " + Bot.Game.KantoSeen.ToString().PadRight(8) + "Captured: " + Bot.Game.KantoOwned.ToString().PadRight(8) + "Evolved: " + Bot.Game.KantoEvolved.ToString()
                                     + "\n\nJohto Pokemon\nSeen: " + Bot.Game.JohtoSeen.ToString().PadRight(8) + "Captured: " + Bot.Game.JohtoOwned.ToString().PadRight(8) + "Evolved: " + Bot.Game.JohtoEvolved.ToString()
                                     + "\n\nHoenn Pokemon\nSeen: " + Bot.Game.HoennSeen.ToString().PadRight(8) + "Captured: " + Bot.Game.HoennOwned.ToString().PadRight(8) + "Evolved: " + Bot.Game.HoennEvolved.ToString()
                                     + "\n\nSinnoh Pokemon\nSeen: " + Bot.Game.SinnohSeen.ToString().PadRight(8) + "Captured: " + Bot.Game.SinnohOwned.ToString().PadRight(8) + "Evolved: " + Bot.Game.SinnohEvolved.ToString()
                                     + "\n\nOther Pokemon\nSeen: " + Bot.Game.OtherSeen.ToString().PadRight(8) + "Captured: " + Bot.Game.OtherOwned.ToString().PadRight(8) + "Evolved: " + Bot.Game.OtherEvolved.ToString();

                //string pokedexdata = "TotalPokemon\nKanto:" + Bot.Game.KantoAllPoke.ToString() + "\tJohto:" + Bot.Game.JohtoAllPoke.ToString() + "\nHoenn:" + Bot.Game.HoennAllPoke.ToString() + "\tSinnoh:" + Bot.Game.SinnohAllPoke.ToString() + "\tOthers:" + Bot.Game.OtherAllPoke.ToString() + "\nKantoPokemon\nSeen:" + Bot.Game.KantoSeen.ToString() + "\tCaptured:" + Bot.Game.KantoOwned.ToString() + "\tEvolved:" + Bot.Game.KantoEvolved.ToString() + "\nJohtoPokemon\nSeen:" + Bot.Game.JohtoSeen.ToString() + "\tCaptured:" + Bot.Game.JohtoOwned.ToString() + "\tEvolved:" + Bot.Game.JohtoEvolved.ToString() + "\nHoennPokemon\nSeen:" + Bot.Game.HoennSeen.ToString() + "\tCaptured:" + Bot.Game.HoennOwned.ToString() + "\tEvolved:" + Bot.Game.HoennEvolved.ToString() + "\nSinnohPokemon\nSeen:" + Bot.Game.SinnohSeen.ToString() + "\tCaptured:" + Bot.Game.SinnohOwned.ToString() + "\tEvolved:" + Bot.Game.SinnohEvolved.ToString() + "\nOtherPokemon\nSeen:" + Bot.Game.OtherSeen.ToString() + "\tCaptured:" + Bot.Game.OtherOwned.ToString() + "\tEvolved:" + Bot.Game.OtherEvolved.ToString(); 

                Clipboard.SetDataObject(pokedexdata + Environment.NewLine + GameClient.Evolution_Left + Environment.NewLine + "Total Evolutions Left: " + (GameClient.Evolution_Counter+8));
                PokeIconButton.ToolTip = pokedexdata;

                LogMessage(pokedexdata);
                LogMessage("Copied To Clipboard Also");

            }
            catch (Exception ex)
            {
                LogMessage("Error: " + ex);
            }
        }

        private void LoginButton_Click(object sender, RoutedEventArgs e)
        {
            bool shouldLogin = false;
            lock (Bot)
            {
                if (Bot.Game == null || !Bot.Game.IsConnected)
                {
                    shouldLogin = true;
                }
                else
                {
                    Logout();
                }
            }
            if (shouldLogin)
            {
                OpenLoginWindow();
            }
        }

        private void MainWindow_OnDrop(object sender, DragEventArgs e)
        {
            string[] file = e.Data?.GetData(DataFormats.FileDrop) as string[];
            if (file != null && file.Length > 0)
            {
                LoadScript(file[0]);
            }
        }

        private void ReloadScript_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrEmpty(Bot.Settings.LastScript))
                return;
            LoadScript(Bot.Settings.LastScript);
        }

        private void ReloadHotkey_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            if (string.IsNullOrEmpty(Bot.Settings.LastScript))
                return;
            LoadScript(Bot.Settings.LastScript);
        }

        private void MenuExploreScript_Click(object sender, RoutedEventArgs e)
        {
            if (!File.Exists(Bot.Settings.LastScript))
                return;

            Process.Start("explorer.exe", "/select, " + Bot.Settings.LastScript);
        }
    }
}
