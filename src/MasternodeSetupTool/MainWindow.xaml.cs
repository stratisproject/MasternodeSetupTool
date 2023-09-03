using System;
using System.Drawing;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Media;
using System.Windows.Threading;
using CSharpFunctionalExtensions;
using NBitcoin;
using Stratis.Bitcoin.Features.Wallet.Models;
using static MasternodeSetupTool.RegistrationService;
using Color = System.Windows.Media.Color;

namespace MasternodeSetupTool
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window, ILogger
    {
        private const string MainStackPanelTag = "Main";
        private const string StatusBarTextBlockTag = "txtStatusBar";
        private const double DefaultButtonHeight = 20;

        private readonly StackPanel stackPanel;
        private readonly TextBlock statusBar;

        private readonly RegistrationService registrationService;

        private readonly DispatcherTimer timer;

        private string currentState = "Begin";
        private string? nextState = null;

        private bool createdButtons;

        private string? collateralWalletMnemonic;
        private string? miningWalletMnemonic;

        private string? collateralWalletPassphrase;
        private string? miningWalletPassphrase;

        private string? collateralWalletPassword;
        private string? miningWalletPassword;

        private string? collateralWalletName;
        private string? miningWalletName;

        private string? collateralAddress;
        private string? cirrusAddress;

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

            this.registrationService = new RegistrationService(networkType, this);

            this.timer = new DispatcherTimer
            {
                Interval = TimeSpan.FromSeconds(1)
            };

            this.timer.Tick += StateMachine_TickAsync;
            this.timer.Start();
        }

        private async void StateMachine_TickAsync(object? sender, EventArgs e)
        {
            this.timer.IsEnabled = false;

            if (this.currentState == "Begin")
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
            }

            if (this.nextState == null)
            {
                this.timer.IsEnabled = true;

                return;
            }

            this.currentState = this.nextState;
            this.nextState = null;

            if (await RunBranchAsync())
            {
                this.timer.IsEnabled = true;

                return;
            }

            if (await SetupBranchAsync())
            {
                this.timer.IsEnabled = true;

                return;
            }

            this.timer.IsEnabled = true;
        }

        private async Task<bool> RunBranchAsync()
        {
            // The 'Run' branch

            if (this.currentState == "RunMasterNode_KeyPresent")
            {
                if (!this.registrationService.CheckFederationKeyExists())
                {
                    MessageBox.Show("Federation key does not exist", "Key file missing", MessageBoxButton.OK, MessageBoxImage.Warning, MessageBoxResult.Yes);

                    ResetState();

                    return true;
                }

                this.nextState = "Run_StartMainChain";
            }

            if (this.currentState == "Run_StartMainChain")
            {
                await this.registrationService.StartNodeAsync(NodeType.MainChain, this.registrationService.MainchainNetwork.DefaultAPIPort).ConfigureAwait(true);

                this.nextState = "Run_MainChainSynced";
            }

            if (this.currentState == "Run_MainChainSynced")
            {
                await this.registrationService.EnsureNodeIsInitializedAsync(NodeType.MainChain, this.registrationService.MainchainNetwork.DefaultAPIPort).ConfigureAwait(true);

                await this.registrationService.EnsureMainChainNodeAddressIndexerIsSyncedAsync().ConfigureAwait(true);

                this.nextState = "Run_StartSideChain";
            }

            if (this.currentState == "Run_StartSideChain")
            {
                await this.registrationService.StartNodeAsync(NodeType.SideChain, this.registrationService.SidechainNetwork.DefaultAPIPort).ConfigureAwait(true);

                this.nextState = "Run_SideChainSynced";
            }

            if (this.currentState == "Run_SideChainSynced")
            {
                await this.registrationService.EnsureNodeIsInitializedAsync(NodeType.SideChain, this.registrationService.SidechainNetwork.DefaultAPIPort).ConfigureAwait(true);

                await this.registrationService.EnsureNodeIsSyncedAsync(NodeType.SideChain, this.registrationService.SidechainNetwork.DefaultAPIPort).ConfigureAwait(true);

                this.nextState = "Run_LaunchBrowser";
            }

            if (this.currentState == "Run_LaunchBrowser")
            {
                await this.registrationService.StartMasterNodeDashboardAsync().ConfigureAwait(true);
                this.registrationService.LaunchBrowser($"http://localhost:{RegistrationService.DashboardPort}");

                ResetState();

                return true;
            }

            return false;
        }

        private async Task<bool> SetupBranchAsync()
        {
            if (this.currentState == "SetupMasterNode_Eula")
            {
                if (MessageBox.Show("100K collateral is required to operate a Masternode; in addition, a balance of 500.1 CRS is required to fund the registration transaction. Are you happy to proceed?", "End-User License Agreement", MessageBoxButton.YesNo, MessageBoxImage.Warning, MessageBoxResult.No) != MessageBoxResult.Yes)
                {
                    ResetState();

                    return true;
                }

                this.nextState = "Setup_KeyPresent";
            }

            if (this.currentState == "Setup_KeyPresent")
            {
                if (this.registrationService.CheckFederationKeyExists())
                {
                    if (MessageBox.Show("Federation key exists. Shall we create a new one?", "Key file already present", MessageBoxButton.YesNo, MessageBoxImage.Warning, MessageBoxResult.No) == MessageBoxResult.No)
                    {
                        this.nextState = "Setup_CreateRestoreUseExisting_StartMainChain";
                        return true;
                    }

                    this.registrationService.DeleteFederationKey();
                }

                this.nextState = "Setup_CreateKey";
            }

            if (this.currentState == "Setup_CreateKey")
            {
                string savePath = this.registrationService.CreateFederationKey();

                MessageBox.Show($"Your Masternode public key is: {this.registrationService.PubKey}");
                MessageBox.Show($"Your private key has been saved in the root Cirrus data folder:\r\n{savePath}. Please ensure that you keep a backup of this file.");

                this.nextState = "Setup_CreateRestoreUseExisting_StartMainChain";
            }

            if (this.currentState == "Setup_CreateRestoreUseExisting_StartMainChain")
            {
                // All 3 sub-branches of this state require the mainchain and sidechain nodes to be initialized, so do that first.
                if (!await this.registrationService.StartNodeAsync(NodeType.MainChain, this.registrationService.MainchainNetwork.DefaultAPIPort).ConfigureAwait(true))
                {
                    Error("Cannot start the Mainchain node, aborting...");
                    ResetState();

                    return true;
                }

                this.nextState = "Setup_CreateRestoreUseExisting_MainChainSynced";
            }

            if (this.currentState == "Setup_CreateRestoreUseExisting_MainChainSynced")
            {
                await this.registrationService.EnsureNodeIsInitializedAsync(NodeType.MainChain, this.registrationService.MainchainNetwork.DefaultAPIPort).ConfigureAwait(true);

                await this.registrationService.EnsureMainChainNodeAddressIndexerIsSyncedAsync().ConfigureAwait(true);

                this.nextState = "Setup_CreateRestoreUseExisting_StartSideChain";
            }

            if (this.currentState == "Setup_CreateRestoreUseExisting_StartSideChain")
            {
                if (!await this.registrationService.StartNodeAsync(NodeType.SideChain, this.registrationService.SidechainNetwork.DefaultAPIPort).ConfigureAwait(true))
                {
                    Error("Cannot start the Sidechain node, aborting...");
                    ResetState();

                    return true;
                }

                this.nextState = "Setup_CreateRestoreUseExisting_SideChainSynced";
            }

            if (this.currentState == "Setup_CreateRestoreUseExisting_SideChainSynced")
            {
                await this.registrationService.EnsureNodeIsInitializedAsync(NodeType.SideChain, this.registrationService.MainchainNetwork.DefaultAPIPort).ConfigureAwait(true);

                await this.registrationService.EnsureNodeIsSyncedAsync(NodeType.SideChain, this.registrationService.MainchainNetwork.DefaultAPIPort).ConfigureAwait(true);

                this.nextState = "Setup_CreateRestoreUseExisting_Select";
            }

            if (this.currentState == "Setup_CreateRestoreUseExisting_Select")
            {
                var dialog = new CreateRestoreUseExisting();
                dialog.ShowDialog();

                if (dialog.Choice == CreateRestoreUseExisting.ButtonChoice.CreateWallet)
                {
                    this.nextState = "Setup_CreateRestoreUseExisting_Create";
                }

                if (dialog.Choice == CreateRestoreUseExisting.ButtonChoice.RestoreWallet)
                {
                    this.nextState = "Setup_CreateRestoreUseExisting_Restore";
                }

                if (dialog.Choice == CreateRestoreUseExisting.ButtonChoice.UseExistingWallet)
                {
                    this.nextState = "Setup_CreateRestoreUseExisting_UseExisting";
                }

                if (dialog.Choice == null)
                {
                    LogError("Registration cancelled.");
                    ResetState();
                    return true;
                }
            }

            if (this.currentState == "Setup_CreateRestoreUseExisting_Create")
            {

                if (!await HandleCreateWalletsAsync(NodeType.MainChain, createNewMnemonic: true))
                {
                    this.nextState = "Setup_CreateRestoreUseExisting_Select";
                    return true;
                }

                this.nextState = "Setup_CreateRestoreUseExisting_Create_Mining";
            }

            if (this.currentState == "Setup_CreateRestoreUseExisting_Create_Mining")
            {

                if (!await HandleCreateWalletsAsync(NodeType.SideChain, createNewMnemonic: true))
                {
                    this.nextState = "Setup_CreateRestoreUseExisting_Select";
                    return true;
                }

                this.nextState = "Setup_CreateRestoreUseExisting_Create_AskForCollateral";
            }

            if (this.currentState == "Setup_CreateRestoreUseExisting_Create_AskForCollateral")
            {
                this.collateralAddress = await this.registrationService.GetFirstWalletAddressAsync(this.registrationService.MainchainNetwork.DefaultAPIPort, this.collateralWalletName).ConfigureAwait(true);

                MessageBox.Show($"Your collateral address is: {this.collateralAddress}", "Collateral Address", MessageBoxButton.OK);

                // The 3 sub-branches recombine after this and can share common states.
                this.nextState = "Setup_CreateRestoreUseExisting_CheckForCollateral";
            }

            if (this.currentState == "Setup_CreateRestoreUseExisting_CheckForCollateral")
            {
                if (await this.registrationService.CheckWalletBalanceAsync(this.registrationService.MainchainNetwork.DefaultAPIPort, this.collateralWalletName, RegistrationService.CollateralRequirement).ConfigureAwait(true))
                    this.nextState = "Setup_CreateRestoreUseExisting_CheckForRegistrationFee";
                else
                    Log($"Waiting for collateral wallet to have a balance of at least {RegistrationService.CollateralRequirement} STRAX", updateTag: this.currentState);
            }

            if (this.currentState == "Setup_CreateRestoreUseExisting_CheckForRegistrationFee")
            {
                if (await this.registrationService.CheckWalletBalanceAsync(this.registrationService.SidechainNetwork.DefaultAPIPort, this.miningWalletName, RegistrationService.FeeRequirement).ConfigureAwait(true))
                    this.nextState = "Setup_CreateRestoreUseExisting_PerformRegistration";
                else
                    this.nextState = "Setup_CreateRestoreUseExisting_PerformCrossChain";
            }

            if (this.currentState == "Setup_CreateRestoreUseExisting_PerformCrossChain")
            {
                if (MessageBox.Show("Insufficient balance in the mining wallet. Perform a cross-chain transfer of 500.1 STRAX?", "Registration Fee Missing", MessageBoxButton.YesNo, MessageBoxImage.Warning, MessageBoxResult.No) == MessageBoxResult.No)
                {
                    ResetState(); // TODO: Maybe we don't have to go all the way back to the beginning, but it is unclear what should be done if they select 'No'

                    return true;
                }

                this.cirrusAddress = await this.registrationService.GetFirstWalletAddressAsync(this.registrationService.SidechainNetwork.DefaultAPIPort, this.miningWalletName).ConfigureAwait(true);

                if (await this.registrationService.PerformCrossChainTransferAsync(this.registrationService.MainchainNetwork.DefaultAPIPort, this.collateralWalletName, this.collateralWalletPassword, "500.1", this.cirrusAddress, this.collateralAddress).ConfigureAwait(true))
                {
                    this.nextState = "Setup_CreateRestoreUseExisting_WaitForCrossChainTransfer";
                }
            }

            if (this.currentState == "Setup_CreateRestoreUseExisting_WaitForCrossChainTransfer")
            {
                Log("Waiting for registration fee to be sent via cross-chain transfer...", updateTag: this.currentState);

                if (await this.registrationService.CheckWalletBalanceAsync(this.registrationService.SidechainNetwork.DefaultAPIPort, this.miningWalletName, RegistrationService.FeeRequirement).ConfigureAwait(true))
                {
                    this.nextState = "Setup_CreateRestoreUseExisting_PerformRegistration";
                }
            }

            if (this.currentState == "Setup_CreateRestoreUseExisting_PerformRegistration")
            {
                await this.registrationService.CallJoinFederationRequestAsync(this.collateralAddress, this.collateralWalletName, this.collateralWalletPassword, this.miningWalletName, this.miningWalletPassword).ConfigureAwait(true);

                this.nextState = "Setup_CreateRestoreUseExisting_WaitForRegistration";
            }

            if (this.currentState == "Setup_CreateRestoreUseExisting_WaitForRegistration")
            {
                if (await this.registrationService.MonitorJoinFederationRequestAsync().ConfigureAwait(true))
                {
                    Log("Registration complete");
                    this.nextState = "Run_LaunchBrowser";
                }
            }

            if (this.currentState == "Setup_CreateRestoreUseExisting_Restore")
            {
                if (!await HandleCreateWalletsAsync(NodeType.MainChain, createNewMnemonic: false))
                {
                    this.nextState = "Setup_CreateRestoreUseExisting_Select";
                    return true;
                }

                this.nextState = "Setup_CreateRestoreUseExisting_Restore_Mining";
            }

            if (this.currentState == "Setup_CreateRestoreUseExisting_Restore_Mining")
            {
                if (!await HandleCreateWalletsAsync(NodeType.SideChain, createNewMnemonic: false))
                {
                    this.nextState = "Setup_CreateRestoreUseExisting_Select";
                    return true;
                }

                this.nextState = "Setup_CreateRestoreUseExisting_Create_AskForCollateral";
            }

            if (this.currentState == "Setup_CreateRestoreUseExisting_UseExisting")
            {
                if (!await HandleExistingWalletNameAsync(NodeType.MainChain))
                {
                    this.nextState = "Setup_CreateRestoreUseExisting_Select";
                    return true;
                }

                this.nextState = "Setup_CreateRestoreUseExisting_UseExisting_CollateralPassword";
            }

            if (this.currentState == "Setup_CreateRestoreUseExisting_UseExisting_CollateralPassword")
            {
                if (!HandlePassword(NodeType.MainChain))
                {
                    this.nextState = "Setup_CreateRestoreUseExisting_Select";
                    return true;
                }

                this.nextState = "Setup_CreateRestoreUseExisting_UseExisting_Mining";
            }

            if (this.currentState == "Setup_CreateRestoreUseExisting_UseExisting_Mining")
            {
                if (!await HandleExistingWalletNameAsync(NodeType.SideChain))
                {
                    this.nextState = "Setup_CreateRestoreUseExisting_Select";
                    return true;
                }

                this.nextState = "Setup_CreateRestoreUseExisting_UseExisting_MiningPassword";
            }

            if (this.currentState == "Setup_CreateRestoreUseExisting_UseExisting_MiningPassword")
            {
                if (!HandlePassword(NodeType.SideChain))
                {
                    this.nextState = "Setup_CreateRestoreUseExisting_Select";
                    return true;
                }

                this.nextState = "Setup_CreateRestoreUseExisting_UseExisting_CheckMainWalletSynced";
            }

            if (this.currentState == "Setup_CreateRestoreUseExisting_UseExisting_CheckMainWalletSynced")
            {
                if (await HandleWalletSyncAsync(NodeType.MainChain))
                {
                    this.nextState = "Setup_CreateRestoreUseExisting_UseExisting_CheckSideWalletSynced";
                }
                else
                {
                    return true;
                }
            }

            if (this.currentState == "Setup_CreateRestoreUseExisting_UseExisting_CheckSideWalletSynced")
            {
                if (await HandleWalletSyncAsync(NodeType.SideChain))
                {
                    // Now we can jump back into the same sequence as the other 2 sub-branches.
                    this.nextState = "Setup_CreateRestoreUseExisting_Create_AskForCollateral";
                }
                else
                {
                    return true;
                }
            }

            return false;
        }

        private async Task<bool> HandleExistingWalletNameAsync(NodeType nodeType)
        {
            string? walletName;
            do
            {
                var inputBox = new InputBox($"Please enter your {WalletTypeName(nodeType)} ({nodeType}) wallet name:");

                walletName = inputBox.ShowDialog();

                if (walletName == null)
                {
                    return false;
                }

                if (!string.IsNullOrEmpty(walletName))
                {
                    try
                    {
                        Network network = nodeType == NodeType.MainChain
                            ? this.registrationService.MainchainNetwork
                            : this.registrationService.SidechainNetwork;

                        if (await this.registrationService.FindWalletByNameAsync(network.DefaultAPIPort, walletName).ConfigureAwait(true))
                        {
                            break;
                        }
                    }
                    catch
                    {
                    }
                }

                MessageBox.Show($"Please ensure that you enter a valid {WalletTypeName(nodeType)} ({nodeType}) wallet name", "Error", MessageBoxButton.OK);
            } while (true);

            if (nodeType == NodeType.MainChain)
            {
                this.collateralWalletName = walletName;
            }
            else
            {
                this.miningWalletName = walletName;
            }

            return true;
        }

        private async Task<bool> HandleNewWalletNameAsync(NodeType nodeType)
        {
            string? walletName;
            do
            {
                var inputBox = new InputBox($"Please enter new {WalletTypeName(nodeType)} ({nodeType}) wallet name:");

                walletName = inputBox.ShowDialog();

                if (walletName == null)
                {
                    return false;
                }

                if (!string.IsNullOrEmpty(walletName))
                {
                    try
                    {
                        Network network = nodeType == NodeType.MainChain
                            ? this.registrationService.MainchainNetwork
                            : this.registrationService.SidechainNetwork;

                        if (!await this.registrationService.FindWalletByNameAsync(network.DefaultAPIPort, walletName).ConfigureAwait(true))
                        {
                            break;
                        }
                        else
                        {
                            MessageBox.Show("A wallet with this name already exists", "Error");
                        }
                    }
                    catch
                    {
                    }
                }

                MessageBox.Show($"Please ensure that you enter a valid (and non-existing) {WalletTypeName(nodeType)} ({nodeType}) wallet name", "Error", MessageBoxButton.OK);
            } while (true);

            if (nodeType == NodeType.MainChain)
            {
                this.collateralWalletName = walletName;
            }
            else
            {
                this.miningWalletName = walletName;
            }

            return true;
        }

        private bool HandleNewMnemonic(NodeType nodeType)
        {
            var mnemonic = string.Join(' ', new Mnemonic("English", WordCount.Twelve).Words);

            var dialog = new ConfirmationDialog($"Enter mnemonic for the {WalletTypeName(nodeType)} wallet", "Mnemonic", mnemonic, false);
            dialog.ShowDialog();

            if (dialog.DialogResult != true)
            {
                return false;
            }

            if (nodeType == NodeType.MainChain)
            {
                this.collateralWalletMnemonic = mnemonic;
            }
            else
            {
                this.miningWalletMnemonic = mnemonic;
            }

            return true;
        }

        private bool HandleUserMnemonic(NodeType nodeType)
        {
            string? mnemonic;

            do
            {
                var inputBox = new InputBox($"Please enter your mnemonic for the {WalletTypeName(nodeType)} ({nodeType}) wallet", "Mnemonic");

                mnemonic = inputBox.ShowDialog();

                if (mnemonic == null)
                {
                    return false;
                }

                if (!string.IsNullOrEmpty(mnemonic))
                {
                    try
                    {
                        // Test the mnemonic to ensure validity.
                        var temp = new Mnemonic(mnemonic, Wordlist.English);

                        // If there was no exception, break out of the loop and continue.
                        break;
                    }
                    catch
                    {
                    }
                }

                MessageBox.Show("Please ensure that you enter a valid mnemonic", "Error", MessageBoxButton.OK);
            } while (true);

            if (nodeType == NodeType.MainChain)
            {
                this.collateralWalletMnemonic = mnemonic;
            }
            else
            {
                this.miningWalletMnemonic = mnemonic;
            }

            return true;
        }

        private bool HandlePassphrase(NodeType nodeType)
        {
            var dialog = new ConfirmationDialog($"Enter passphrase for the {WalletTypeName(nodeType)} wallet",
                "Passphrase",
                "",
                true,
                allowEmpty: true);

            dialog.ShowDialog();

            if (dialog.DialogResult != true)
            {
                return false;
            }

            string result = dialog.Text2.Text ?? "";

            if (nodeType == NodeType.MainChain)
            {
                this.collateralWalletPassphrase = result;
            }
            else
            {
                this.miningWalletPassphrase = result;
            }

            return true;
        }

        private bool HandlePassword(NodeType nodeType)
        {
            var dialog = new ConfirmationDialog(
                $"Enter {WalletTypeName(nodeType)} wallet password ({nodeType})", 
                "Password", 
                "", 
                true);

            dialog.ShowDialog();

            if (dialog.DialogResult != true)
            {
                return false;
            }

            if (nodeType == NodeType.MainChain)
            {
                this.collateralWalletPassword = dialog.Text2.Text;
            }
            else
            {
                this.miningWalletPassword = dialog.Text2.Text;
            }

            return true;
        }

        private async Task<bool> HandleWalletCreationAsync(NodeType nodeType, bool createNewWallet)
        {
            Network network = nodeType == NodeType.MainChain
                ? this.registrationService.MainchainNetwork
                : this.registrationService.SidechainNetwork;

            string? walletName = nodeType == NodeType.MainChain
                ? this.collateralWalletName
                : this.miningWalletName;

            string? walletMnemonic = nodeType == NodeType.MainChain
                ? this.collateralWalletMnemonic
                : this.miningWalletMnemonic;

            string? walletPassphrase = nodeType == NodeType.MainChain
                ? this.collateralWalletPassphrase
                : this.miningWalletPassphrase;

            string? walletPassword = nodeType == NodeType.MainChain
                ? this.collateralWalletPassword
                : this.miningWalletPassword;

            while (true)
            {
                try
                {
                    if (walletName == null
                        || walletMnemonic == null
                        || walletPassphrase == null
                        || walletPassword == null
                        || !await this.registrationService.RestoreWalletAsync(network.DefaultAPIPort, nodeType, walletName, walletMnemonic, walletPassphrase, walletPassword, createNewWallet).ConfigureAwait(true))
                    {
                        string action = createNewWallet ? "create" : "restore";
                        LogError($"Cannot {action} {WalletTypeName(nodeType)} wallet, aborting...");
                        return false;
                    }
                    break;
                }
                catch (WalletCollisionException)
                {
                    LogError($"The {WalletTypeName(nodeType)} wallet with this mnemonic already exists.");
                    LogError("Please provide a new mnemonic.");

                    if (!HandleNewMnemonic(nodeType))
                    {
                        LogError("New mnemonic was not provided. Aborting...");
                        return false;
                    }
                }
            }

            if (!await this.registrationService.ResyncWalletAsync(network.DefaultAPIPort, walletName).ConfigureAwait(true))
            {
                LogError($"Cannot resync {WalletTypeName(nodeType)} wallet, aborting...");
                return false;
            }

            return true;
        }

        private async Task<bool> HandleWalletSyncAsync(NodeType nodeType)
        {
            string logTag = "HandleWalletSyncAsync" + nodeType;

            Network network = nodeType == NodeType.MainChain
                ? this.registrationService.MainchainNetwork
                : this.registrationService.SidechainNetwork;

            string? walletName = nodeType == NodeType.MainChain
                ? this.collateralWalletName
                : this.miningWalletName;

            if (walletName == null)
            {
                throw new ArgumentException("Wallet name can not be null.");
            }

            int percentSynced = await this.registrationService.WalletSyncProgressAsync(network.DefaultAPIPort, walletName).ConfigureAwait(true);
            Log($"{nodeType} ({WalletTypeName(nodeType)}) wallet is {percentSynced}% synced", updateTag: logTag);

            if (await this.registrationService.IsWalletSyncedAsync(network.DefaultAPIPort, walletName).ConfigureAwait(true))
            {
                Log($"{nodeType} ({WalletTypeName(nodeType)}) wallet synced successfuly.", updateTag: logTag);
                return true;
            }
            else
            {
                return false;
            }
        }

        private async Task<bool> HandleCreateWalletsAsync(NodeType nodeType, bool createNewMnemonic)
        {
            if (createNewMnemonic)
            {
                if (!HandleNewMnemonic(nodeType))
                {
                    return false;
                }
            }
            else
            {
                if (!HandleUserMnemonic(nodeType))
                {
                    return false;
                }
            }

            if (!await HandleNewWalletNameAsync(nodeType))
            {
                return false;
            }

            if (!HandlePassphrase(nodeType))
            {
                return false;
            }

            if (!HandlePassword(nodeType))
            {
                return false;
            }

            if (!await HandleWalletCreationAsync(nodeType, createNewMnemonic))
            {
                return false;
            }

            try
            {
                while (!await HandleWalletSyncAsync(nodeType))
                {
                    await Task.Delay(TimeSpan.FromSeconds(1));
                }
            }
            catch
            {
                return false;
            }

            return true;
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
                        this.nextState = "RunMasterNode_KeyPresent";

                        break;
                    }
                case "SetupMasterNode":
                    {
                        this.nextState = "SetupMasterNode_Eula";

                        break;
                    }
            }
        }

        private void ResetState()
        {
            this.nextState = null;
            this.currentState = "Begin";
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
    }
}
