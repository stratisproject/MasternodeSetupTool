using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Media;
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

        private bool createdButtons;

        private StateMachine stateMachine;

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

        public Task OnStart()
        {
            if (!this.createdButtons)
            {
                this.createdButtons = true;

                Style flatStyle = this.FlatStyle;

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

            return Task.CompletedTask;
        }

        public Task OnProgramVersionAvailable(string? version)
        {
            if (version != null)
            {
                this.VersionText.Text = $"Version: {version}";
            }

            return Task.CompletedTask;
        }

        public Task OnFederationKeyMissing()
        {
            MessageBox.Show("Federation key does not exist", "Key file missing", MessageBoxButton.OK, MessageBoxImage.Warning, MessageBoxResult.Yes);
            return Task.CompletedTask;
        }

        public Task OnNodeFailedToStart(NodeType nodeType, string? reason = null)
        {
            Error($"Cannot start the {nodeType} node, aborting...");
            if (reason != null)
            {
                Error($"Reason: {reason}");
            }
            return Task.CompletedTask;
        }

        public Task<bool> OnAskForEULA()
        {
            return Task.Run(
                () => MessageBox.Show("100K collateral is required to operate a Masternode; in addition, a balance of 500.1 CRS is required to fund the registration transaction. Are you happy to proceed?",
                            "End-User License Agreement",
                            MessageBoxButton.YesNo,
                            MessageBoxImage.Warning,
                            MessageBoxResult.No) == MessageBoxResult.Yes);
        }

        public Task<bool> OnAskForNewFederationKey()
        {
            return Task.Run(() =>
            {
                return MessageBox.Show(
                    "Federation key exists. Shall we create a new one?",
                    "Key file already present",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Warning,
                    MessageBoxResult.No) == MessageBoxResult.Yes;
            });
        }

        public Task OnShowNewFederationKey(string pubKey, string savePath)
        {
            return Task.Run(() =>
            {
                MessageBox.Show($"Your Masternode public key is: {pubKey}");
                MessageBox.Show($"Your private key has been saved in the root Cirrus data folder:\r\n{savePath}. Please ensure that you keep a backup of this file.");
            });
        }

        public Task<bool> OnAskToRunIfAlreadyMember()
        {
            return Task.Run(() =>
            {
                return MessageBox.Show("Your node is already a member of a federation. Do you want to run the Masternode Dashboard instead?",
                                       "Member of a federation",
                                       MessageBoxButton.YesNo,
                                       MessageBoxImage.Warning,
                                       MessageBoxResult.No) == MessageBoxResult.Yes;
            });

        }

        public Task OnAlreadyMember()
        {
            return Task.Run(() =>
            {
                Info("Your node is already a member of a federation. Consider using 'Run Masternode' instead.");
            });
        }

        public Task<WalletSource?> OnAskForWalletSource(NodeType nodeType)
        {
            return Task.Run<WalletSource?>(() =>
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
            });
        }

        public Task<string?> OnChooseWallet(List<WalletItem> wallets, NodeType nodeType)
        {
            return Task.Run(() =>
            {
                var selectionDialog = new WalletSelectionDialog(wallets);
                selectionDialog.ShowDialog();

                return selectionDialog.SelectedWalletName;
            });
        }

        public Task<string?> OnChooseAddress(List<AddressItem> addresses, NodeType nodeType)
        {
            return Task.Run(() =>
            {
                var selectionDialog = new AddressSelectionDialog(addresses);
                selectionDialog.ShowDialog();

                return selectionDialog.SelectedAddress;
            });
        }

        public Task OnWaitingForCollateral()
        {
            return Task.Run(() =>
            {
                Log($"Waiting for collateral wallet to have a balance of at least {RegistrationService.CollateralRequirement} STRAX", updateTag: "OnWaitingForCollateral");
            });
        }

        public Task OnWaitingForRegistrationFee()
        {
            return Task.Run(() =>
            {
                Log("Waiting for registration fee to be sent to the mining wallet...", updateTag: "OnWaitingForRegistrationFee");
            });
        }

        public Task OnMissingRegistrationFee(string address)
        {
            return Task.Run(() =>
            {
                Error($"Insufficient balance to pay registration fee. Please send 500.1 CRS to the mining wallet on address: {address}");
            });
        }

        public Task OnRegistrationCanceled()
        {
            return Task.Run(() =>
            {
                LogError("Registration cancelled.");
            });
        }

        public Task OnRegistrationComplete()
        {
            return Task.Run(() =>
            {
                Log("Registration complete");
            });
        }

        public Task OnRegistrationFailed()
        {
            return Task.Run(() =>
            {
                Error("Failed to register your masternode, aborting...");
            });
        }

        public Task<bool> OnAskForMnemonicConfirmation(NodeType nodeType, string mnemonic)
        {
            return Task.Run(() =>
            {
                var dialog = new ConfirmationDialog($"Enter mnemonic for the {WalletTypeName(nodeType)} wallet", "Mnemonic", mnemonic, false);
                dialog.ShowDialog();
                return dialog.DialogResult == true;
            });
        }

        public Task<string?> OnAskForUserMnemonic(NodeType nodeType)
        {
            return Task.Run(() =>
            {
                var inputBox = new InputBox($"Please enter your mnemonic for the {WalletTypeName(nodeType)} ({nodeType}) wallet", "Mnemonic");
                return inputBox.ShowDialog();
            });
        }

        public Task<string?> OnAskForWalletName(NodeType nodeType, bool newWallet)
        {
            return Task.Run(() =>
            {
                var inputBox = new InputBox($"Please enter {nodeType} wallet name:");
                return inputBox.ShowDialog();
            });
        }

        public Task<string?> OnAskForPassphrase(NodeType nodeType)
        {
            return Task.Run(() =>
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
            });
        }

        public Task<string?> OnAskForWalletPassword(NodeType nodeType)
        {
            return Task.Run(() =>
            {
                var dialog = new ConfirmationDialog(
                    $"Enter {WalletTypeName(nodeType)} wallet password ({nodeType})",
                    "Password",
                    "",
                    true);

                dialog.ShowDialog();

                if (dialog.DialogResult != true)
                {
                    return null;
                }

                return dialog.Text2.Text ?? string.Empty;
            });
        }

        public Task<string?> OnAskCreatePassword(NodeType nodeType)
        {
            return Task.Run(() =>
            {
                var dialog = new ConfirmationDialog(
                    $"Enter a new {WalletTypeName(nodeType)} wallet password ({nodeType})",
                    "Password",
                    "",
                    true);

                dialog.ShowDialog();

                if (dialog.DialogResult != true)
                {
                    return null;
                }

                return dialog.Text2.Text ?? string.Empty;
            });
        }

        public Task<bool> OnAskReenterPassword(NodeType nodeType)
        {
            return Task.Run(() =>
            {
                return MessageBox.Show("The password you entered is incorrect. Do you want to enter it again?",
                                       "Incorrect password",
                                       MessageBoxButton.YesNo,
                                       MessageBoxImage.Warning,
                                       MessageBoxResult.No) == MessageBoxResult.No;
            });
        }

        public Task OnWalletNameExists(NodeType nodeType)
        {
            return Task.Run(() =>
            {
                MessageBox.Show("A wallet with this name already exists", "Error");
            });
        }

        public Task OnMnemonicIsInvalid(NodeType nodeType)
        {
            return Task.Run(() =>
            {
                MessageBox.Show("Please ensure that you enter a valid mnemonic", "Error", MessageBoxButton.OK);
            });
        }

        public Task OnMnemonicExists(NodeType nodeType)
        {
            return Task.Run(() =>
            {
                LogError($"The {WalletTypeName(nodeType)} wallet with this mnemonic already exists.");
                LogError("Please provide a new mnemonic.");
            });
        }

        public Task OnWalletExistsOrInvalid(NodeType nodeType)
        {
            return Task.Run(() =>
            {
                MessageBox.Show("A wallet with this name already exists", "Error");
            });
        }

        public Task OnWalletSyncing(NodeType nodeType, int progress)
        {
            return Task.Run(() =>
            {
                Log($"{nodeType} ({WalletTypeName(nodeType)}) wallet is {progress}% synced", updateTag: $"{nodeType}WalletSyncing");
            });
        }

        public Task OnWalletSynced(NodeType nodeType)
        {
            return Task.Run(() =>
            {
                Log($"{nodeType} ({WalletTypeName(nodeType)}) wallet synced successfuly.", updateTag: $"{nodeType}WalletSyncing");
            });
        }

        public Task OnShowWalletName(NodeType nodeType, string walletName)
        {
            return Task.Run(() =>
            {
                //TODO
            });
        }

        public Task OnShowWalletAddress(NodeType nodeType, string address)
        {
            return Task.Run(() =>
            {
                //TODO
            });
        }

        public Task OnRestoreWalletFailed(NodeType nodeType)
        {
            return Task.Run(() =>
            {
                LogError($"Can not restore a {WalletTypeName(nodeType)} wallet, aborting...");
            });
        }

        public Task OnCreateWalletFailed(NodeType nodeType)
        {
            return Task.Run(() =>
            {
                LogError($"Can not create a {WalletTypeName(nodeType)} wallet, aborting...");
            });
        }

        public Task OnResyncFailed(NodeType nodeType)
        {
            return Task.Run(() => 
            {
                LogError($"Cannot resync {WalletTypeName(nodeType)} wallet, aborting...");
            });
        }
    }
}
