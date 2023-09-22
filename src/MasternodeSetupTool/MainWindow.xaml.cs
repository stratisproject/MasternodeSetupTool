using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Media;
using System.Windows.Threading;
using NBitcoin;
using Color = System.Windows.Media.Color;

namespace MasternodeSetupTool
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window, IStateHandler
    {
        private const string MainStackPanelTag = "Main";
        private const string StatusBarTextBlockTag = "txtStatusBar";
        private const double DefaultButtonHeight = 20;

        private readonly StackPanel stackPanel;
        private readonly TextBlock statusBar;

        private bool createdButtons = false;

        private StateMachine stateMachine;

        private readonly DispatcherTimer timer;

        private bool PrintStacktraces
        {
            get
            {
#if DEBUG
                return true;
#endif
#pragma warning disable CS0162 // Unreachable code detected
                return false;
#pragma warning restore CS0162 // Unreachable code detected
            }
        }

        private Style FlatStyle
        {
            get
            {
                return (Style)this.FindResource(ToolBar.ButtonStyleKey);
            }
        }

        public MainWindow(string[] args)
        {
            InitializeComponent();
            
            this.stackPanel = (StackPanel)this.FindName(MainStackPanelTag);
            this.statusBar = (TextBlock)this.FindName(StatusBarTextBlockTag);

            NetworkType networkType = NetworkType.Mainnet;

            if (args.Any(a => a.Contains("-testnet")))
                networkType = NetworkType.Testnet;

            if (args.Any(a => a.Contains("-regtest")))
                networkType = NetworkType.Regtest;

            this.stateMachine = new StateMachine(networkType, this);

            this.timer = new DispatcherTimer
            {
                Interval = TimeSpan.FromMilliseconds(200)
            };

            this.timer.Tick += StateMachine_TickAsync;
            this.timer.Start();
        }

        private async void StateMachine_TickAsync(object? sender, EventArgs e)
        {
            await this.stateMachine.TickAsync();
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            Button button = (Button)sender;

            if (button == null)
            {
                return;
            }

            switch (button.Tag.ToString())
            {
                case "RunMasterNode":
                    {
                        this.stateMachine.OnRunNode();
                        break;
                    }
                case "SetupMasterNode":
                    {
                        this.stateMachine.OnSetupNode();
                        break;
                    }
            }
        }

        private void LogWithBrush(string message, Brush? brush = null, string? updateTag = null)
        {
            this.statusBar.Dispatcher.Invoke(() =>
            {
                var inline = new Run(message + "\n");
                inline.Tag = updateTag;

                if (brush != null)
                {
                    inline.Foreground = brush;
                }

                InlineCollection inlines = this.statusBar.Inlines;
                Inline lastInline = inlines.LastInline;

                if (updateTag != null && lastInline != null && string.Equals(lastInline.Tag, updateTag)) 
                {
                    inlines.Remove(lastInline);
                }

                inlines.Add(inline);
                this.logScrollArea.ScrollToBottom();
            });
        }

        private void Log(string message, string? updateTag = null)
        {
            this.statusBar.Dispatcher.Invoke(() =>
            {
                LogWithBrush(message, brush: null, updateTag);
            });
        }

        private void Log(string message, Color color, string? updateTag = null)
        {
            this.statusBar.Dispatcher.Invoke(() =>
            {
                LogWithBrush(message, new SolidColorBrush(color), updateTag);
            });
        }

        private void LogError(string message)
        {
            Log(message, Color.FromRgb(255, 51, 51));
        }

        public void Info(string message, string? updateTag = null)
        {
            Log(message, updateTag: updateTag);
        }

        public void Error(string message)
        {
            LogError(message);
        }

        public void Error(Exception exception)
        {
            if (this.PrintStacktraces)
            {
                LogError($"{exception}");
            }
        }

        public void Error(string message, Exception exception)
        {
            Error(message);
            Error(exception);
        }

        private string WalletTypeName(NodeType nodeType)
        {
            return nodeType == NodeType.MainChain ? "collateral" : "mining";
        }

        public static string? GetInformationalVersion() =>
            Assembly
                .GetExecutingAssembly()
                ?.GetCustomAttribute<AssemblyInformationalVersionAttribute>()
                ?.InformationalVersion;

        public async Task OnStart()
        {
            if (!this.createdButtons)
            {
                this.createdButtons = true;

                Style flatStyle = this.FlatStyle;

                try
                {
                    var button = new Button
                    {
                        Content = "Run Masternode",
                        Tag = "RunMasterNode",
                        Margin = new Thickness(16.0, 4.0, 16.0, 4.0),
                        Padding = new Thickness(4.0),
                        Background = new SolidColorBrush(Color.FromRgb(255, 255, 255)),
                    };
                    button.Click += new RoutedEventHandler(Button_Click);
                    this.stackPanel.Children.Add(button);

                    button = new Button
                    {
                        Content = "Register Masternode",
                        Tag = "SetupMasterNode",
                        Margin = new Thickness(16.0, 4.0, 16.0, 4.0),
                        Padding = new Thickness(4.0),
                        Background = new SolidColorBrush(Color.FromRgb(255, 255, 255)),
                    };

                    button.Click += new RoutedEventHandler(Button_Click);

                    this.stackPanel.Children.Add(button);
                }
                catch (Exception ex)
                {

                }
            }
        }

        public async Task OnProgramVersionAvailable(string? version)
        {
            if (version != null)
            {
                var thread1 = System.Threading.Thread.CurrentThread;
                this.VersionText.Text = $"Version: {version}";
            }
        }

        public async Task OnFederationKeyMissing()
        {
            MessageBox.Show("Federation key does not exist", "Key file missing", MessageBoxButton.OK, MessageBoxImage.Warning, MessageBoxResult.Yes);
        }

        public async Task OnNodeFailedToStart(NodeType nodeType, string? reason = null)
        {
            Error($"Cannot start the {nodeType} node, aborting...");
            if (reason != null)
            {
                Error($"Reason: {reason}");
            }
        }

        public async Task<bool> OnAskForEULA()
        {
            return MessageBox.Show("100K collateral is required to operate a Masternode; in addition, a balance of 500.1 CRS is required to fund the registration transaction. Are you happy to proceed?",
                            "End-User License Agreement",
                            MessageBoxButton.YesNo,
                            MessageBoxImage.Warning,
                            MessageBoxResult.No) == MessageBoxResult.Yes;
        }
        
        public async Task<bool> OnAskForNewFederationKey()
        {
            return MessageBox.Show(
                    "Federation key exists. Shall we create a new one?",
                    "Key file already present",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Warning,
                    MessageBoxResult.No) == MessageBoxResult.Yes;
        }

        public async Task OnShowNewFederationKey(string pubKey, string savePath)
        {
            MessageBox.Show($"Your Masternode public key is: {pubKey}");
            MessageBox.Show($"Your private key has been saved in the root Cirrus data folder:\r\n{savePath}. Please ensure that you keep a backup of this file.");
        }

        public async Task<bool> OnAskToRunIfAlreadyMember()
        {
            return MessageBox.Show("Your node is already a member of a federation. Do you want to run the Masternode Dashboard instead?",
                                                   "Member of a federation",
                                                   MessageBoxButton.YesNo,
                                                   MessageBoxImage.Warning,
                                                   MessageBoxResult.No) == MessageBoxResult.Yes;
        }

        public async Task OnAlreadyMember()
        {
            Info("Your node is already a member of a federation. Consider using 'Run Masternode' instead.");
        }

        public async Task<WalletSource?> OnAskForWalletSource(NodeType nodeType)
        {
            var dialog = new CreateRestoreUseExisting();
            dialog.ShowDialog();

            if (dialog.Choice == CreateRestoreUseExisting.ButtonChoice.CreateWallet)
            {
                return WalletSource.NewWallet;
            }

            if (dialog.Choice == CreateRestoreUseExisting.ButtonChoice.RestoreWallet)
            {
                return WalletSource.RestoreWallet;
            }

            if (dialog.Choice == CreateRestoreUseExisting.ButtonChoice.UseExistingWallet)
            {
                return WalletSource.UseExistingWallet;
            }

            return null;
        }

        public async Task<string?> OnChooseWallet(List<WalletItem> wallets, NodeType nodeType)
        {
            var selectionDialog = new WalletSelectionDialog(wallets);
            selectionDialog.ShowDialog();

            return selectionDialog.SelectedWalletName;
        }

        public async Task<string?> OnChooseAddress(List<AddressItem> addresses, NodeType nodeType)
        {
            var selectionDialog = new AddressSelectionDialog(addresses);
            selectionDialog.ShowDialog();

            return selectionDialog.SelectedAddress;
        }

        public async Task OnWaitingForCollateral()
        {
            Log($"Waiting for collateral wallet to have a balance of at least {RegistrationService.CollateralRequirement} STRAX", updateTag: "OnWaitingForCollateral");
        }

        public async Task OnWaitingForRegistrationFee()
        {
            Log("Waiting for registration fee to be sent to the mining wallet...", updateTag: "OnWaitingForRegistrationFee");
        }

        public async Task OnMissingRegistrationFee(string address)
        {
            Error($"Insufficient balance to pay registration fee. Please send 500.1 CRS to the mining wallet on address: {address}");
        }

        public async Task OnRegistrationCanceled()
        {
            LogError("Registration cancelled.");
        }

        public async Task OnRegistrationComplete()
        {
            Log("Registration complete");
        }

        public async Task OnRegistrationFailed()
        {
            Error("Failed to register your masternode, aborting...");
        }

        public async Task<bool> OnAskForMnemonicConfirmation(NodeType nodeType, string mnemonic)
        {
            var dialog = new ConfirmationDialog($"Enter mnemonic for the {WalletTypeName(nodeType)} wallet", "Mnemonic", mnemonic, false);
            dialog.ShowDialog();
            return dialog.DialogResult == true;
        }

        public async Task<string?> OnAskForUserMnemonic(NodeType nodeType)
        {
            var inputBox = new InputBox($"Please enter your mnemonic for the {WalletTypeName(nodeType)} ({nodeType}) wallet", "Mnemonic");
            return inputBox.ShowDialog();
        }

        public async Task<string?> OnAskForWalletName(NodeType nodeType, bool newWallet)
        {
            var inputBox = new InputBox($"Please enter {nodeType} wallet name:");
            return inputBox.ShowDialog();
        }

        public async Task<string?> OnAskForPassphrase(NodeType nodeType)
        {
            var dialog = new ConfirmationDialog($"Enter passphrase for the {nodeType} wallet",
                "Passphrase",
                "",
                true,
                allowEmpty: true);

            dialog.ShowDialog();

            if (dialog.DialogResult != true)
            {
                return null;
            }

            return dialog.Text2.Text ?? "";
        }

        public async Task<string?> OnAskForWalletPassword(NodeType nodeType)
        {
            var dialog = new ConfirmationDialog(
                titleText: $"Enter {WalletTypeName(nodeType)} wallet password ({nodeType})",
                labelText: "Password",
                firstTextContent: "",
                firstTextEditable: true);

            dialog.ShowDialog();

            if (dialog.DialogResult != true)
            {
                return null;
            }

            return dialog.Text2.Text ?? string.Empty;
        }

        public async Task<string?> OnAskCreatePassword(NodeType nodeType)
        {
            var dialog = new ConfirmationDialog(
                titleText: $"Enter a new {WalletTypeName(nodeType)} wallet password ({nodeType})",
                labelText: "Password",
                firstTextContent: "",
                firstTextEditable: true);

            dialog.ShowDialog();

            if (dialog.DialogResult != true)
            {
                return null;
            }

            return dialog.Text2.Text ?? string.Empty;
        }

        public async Task<bool> OnAskReenterPassword(NodeType nodeType)
        {
            return MessageBox.Show("The password you entered is incorrect. Do you want to enter it again?",
                                   "Incorrect password",
                                   MessageBoxButton.YesNo,
                                   MessageBoxImage.Warning,
                                   MessageBoxResult.No) == MessageBoxResult.No;
        }

        public async Task OnWalletNameExists(NodeType nodeType)
        {
            MessageBox.Show("A wallet with this name already exists", "Error");
        }

        public async Task OnMnemonicIsInvalid(NodeType nodeType)
        {
            MessageBox.Show("Please ensure that you enter a valid mnemonic", "Error", MessageBoxButton.OK);
        }

        public async Task OnMnemonicExists(NodeType nodeType)
        {
            LogError($"The {WalletTypeName(nodeType)} wallet with this mnemonic already exists.");
            LogError("Please provide a new mnemonic.");
        }

        public async Task OnWalletExistsOrInvalid(NodeType nodeType)
        {
            MessageBox.Show("A wallet with this name already exists", "Error");
        }

        public async Task OnWalletSyncing(NodeType nodeType, int progress)
        {
            Log($"{nodeType} ({WalletTypeName(nodeType)}) wallet is {progress}% synced", updateTag: $"{nodeType}WalletSyncing");
        }

        public async Task OnWalletSynced(NodeType nodeType)
        {
            Log($"{nodeType} ({WalletTypeName(nodeType)}) wallet synced successfuly.", updateTag: $"{nodeType}WalletSyncing");
        }

        public async Task OnShowWalletName(NodeType nodeType, string walletName)
        {
            //TODO
        }

        public async Task OnShowWalletAddress(NodeType nodeType, string address)
        {
            //TODO
        }

        public async Task OnRestoreWalletFailed(NodeType nodeType)
        {
            LogError($"Can not restore a {WalletTypeName(nodeType)} wallet, aborting...");
        }

        public async Task OnCreateWalletFailed(NodeType nodeType)
        {
            LogError($"Can not create a {WalletTypeName(nodeType)} wallet, aborting...");
        }

        public async Task OnResyncFailed(NodeType nodeType)
        {
            LogError($"Cannot resync {WalletTypeName(nodeType)} wallet, aborting...");
        }
    }
}
