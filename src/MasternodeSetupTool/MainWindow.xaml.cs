using System;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;
using NBitcoin;

namespace MasternodeSetupTool
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
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
        private string mnemonic;
        private string passphrase;
        private string password;

        private string collateralWalletName;
        private string miningWalletName;

        private string collateralAddress;
        private string cirrusAddress;

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

            this.registrationService = new RegistrationService(networkType, this.statusBar);

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

                    var button = new Button
                    {
                        Content = "Run Masternode",
                        Tag = "RunMasterNode",
                        Height = DefaultButtonHeight
                    };

                    button.Click += new RoutedEventHandler(Button_ClickAsync);
                    this.stackPanel.Children.Add(button);

                    button = new Button
                    {
                        Content = "Setup Masternode",
                        Tag = "SetupMasterNode",
                        Height = DefaultButtonHeight
                    };

                    button.Click += new RoutedEventHandler(Button_ClickAsync);

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

            if (await RunBranch())
            {
                this.timer.IsEnabled = true;

                return;
            }

            if (await SetupBranch())
            {
                this.timer.IsEnabled = true;

                return;
            }

            this.timer.IsEnabled = true;
        }

        private async Task<bool> RunBranch()
        {
            // The 'Run' branch

            if (this.currentState == "RunMasterNode_KeyPresent")
            {
                if (!this.registrationService.CheckFederationKeyExists())
                {
                    MessageBox.Show("Federation key does not exist", "Key file missing", MessageBoxButton.OK, MessageBoxImage.Warning, MessageBoxResult.Yes);

                    this.nextState = null;
                    this.currentState = "Begin";

                    return true;
                }

                this.nextState = "Run_StartMainChain";
            }

            if (this.currentState == "Run_StartMainChain")
            {
                await this.registrationService.StartNodeAsync(NodeType.MainChain).ConfigureAwait(false);

                this.nextState = "Run_MainChainSynced";
            }

            if (this.currentState == "Run_MainChainSynced")
            {
                await this.registrationService.EnsureNodeIsInitializedAsync(NodeType.MainChain, this.registrationService.MainchainNetwork.DefaultAPIPort).ConfigureAwait(false);

                await this.registrationService.EnsureNodeIsSyncedAsync(NodeType.MainChain, this.registrationService.MainchainNetwork.DefaultAPIPort).ConfigureAwait(false);

                this.nextState = "Run_StartSideChain";
            }

            if (this.currentState == "Run_StartSideChain")
            {
                await this.registrationService.StartNodeAsync(NodeType.SideChain).ConfigureAwait(false);

                this.nextState = "Run_SideChainSynced";
            }

            if (this.currentState == "Run_SideChainSynced")
            {
                await this.registrationService.EnsureNodeIsInitializedAsync(NodeType.SideChain, this.registrationService.MainchainNetwork.DefaultAPIPort).ConfigureAwait(false);

                await this.registrationService.EnsureNodeIsSyncedAsync(NodeType.SideChain, this.registrationService.MainchainNetwork.DefaultAPIPort).ConfigureAwait(false);

                this.nextState = "Run_LaunchBrowser";
            }

            if (this.currentState == "Run_LaunchBrowser")
            {
                await this.registrationService.StartMasterNodeDashboardAsync().ConfigureAwait(false);
                await this.registrationService.LaunchBrowserAsync($"http://localhost:{RegistrationService.DashboardPort}").ConfigureAwait(false);

                this.nextState = null;
                this.currentState = "Begin";

                return true;
            }

            return false;
        }

        private async Task<bool> SetupBranch()
        {
            if (this.currentState == "SetupMasterNode_Eula")
            {
                if (MessageBox.Show("100K collateral is required to operate a Masternode; in addition, a balance of 500.1 CRS is required to fund the registration transaction. Are you happy to proceed?", "End-User License Agreement", MessageBoxButton.YesNo, MessageBoxImage.Warning, MessageBoxResult.No) != MessageBoxResult.Yes)
                {
                    this.nextState = null;
                    this.currentState = "Begin";

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
                        this.nextState = null;
                        this.currentState = "Begin";

                        return true;
                    }

                    this.registrationService.DeleteFederationKey();
                }

                this.nextState = "Setup_CreateKey";
            }

            if (this.currentState == "Setup_CreateKey")
            {
                do
                {
                    var inputBox = new InputBox($"Please enter a passphrase (this can be anything, but please write it down):");

                    this.passphrase = inputBox.ShowDialog();

                    if (!string.IsNullOrEmpty(this.passphrase))
                        break;

                    MessageBox.Show("Please ensure that you enter a valid passphrase", "Error", MessageBoxButton.OK);
                } while (true);

                string savePath = this.registrationService.CreateFederationKey();

                MessageBox.Show($"Your Masternode public key is: {this.registrationService.PubKey}");
                MessageBox.Show($"Your private key has been saved in the root Cirrus data folder:\r\n{savePath}. Please ensure that you keep a backup of this file.");

                this.nextState = "Setup_CreateRestoreUseExisting_StartMainChain";
            }

            if (this.currentState == "Setup_CreateRestoreUseExisting_StartMainChain")
            {
                // All 3 sub-branches of this state require the mainchain and sidechain nodes to be initialized, so do that first.
                await this.registrationService.StartNodeAsync(NodeType.MainChain).ConfigureAwait(false);

                this.nextState = "Setup_CreateRestoreUseExisting_MainChainSynced";
            }

            if (this.currentState == "Setup_CreateRestoreUseExisting_MainChainSynced")
            {
                await this.registrationService.EnsureNodeIsInitializedAsync(NodeType.MainChain, this.registrationService.MainchainNetwork.DefaultAPIPort).ConfigureAwait(false);

                await this.registrationService.EnsureNodeIsSyncedAsync(NodeType.MainChain, this.registrationService.MainchainNetwork.DefaultAPIPort).ConfigureAwait(false);

                this.nextState = "Setup_CreateRestoreUseExisting_StartSideChain";
            }

            if (this.currentState == "Setup_CreateRestoreUseExisting_StartSideChain")
            {
                await this.registrationService.StartNodeAsync(NodeType.SideChain).ConfigureAwait(false);

                this.nextState = "Setup_CreateRestoreUseExisting_SideChainSynced";
            }

            if (this.currentState == "Setup_CreateRestoreUseExisting_SideChainSynced")
            {
                await this.registrationService.EnsureNodeIsInitializedAsync(NodeType.SideChain, this.registrationService.MainchainNetwork.DefaultAPIPort).ConfigureAwait(false);

                await this.registrationService.EnsureNodeIsSyncedAsync(NodeType.SideChain, this.registrationService.MainchainNetwork.DefaultAPIPort).ConfigureAwait(false);

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
            }

            if (this.currentState == "Setup_CreateRestoreUseExisting_Create")
            {
                var temp = new Mnemonic("English", WordCount.Twelve);
                this.mnemonic = string.Join(' ', temp.Words);

                var dialog = new ConfirmationDialog("Mnemonic", this.mnemonic, false);
                dialog.ShowDialog();

                this.nextState = "Setup_CreateRestoreUseExisting_Create_Passphrase";
            }

            if (this.currentState == "Setup_CreateRestoreUseExisting_Create_Passphrase")
            {
                var dialog = new ConfirmationDialog("Passphrase", "", true);
                dialog.ShowDialog();

                this.passphrase = dialog.Text2.Text;

                this.nextState = "Setup_CreateRestoreUseExisting_Create_Password";
            }

            if (this.currentState == "Setup_CreateRestoreUseExisting_Create_Password")
            {
                var dialog = new ConfirmationDialog("Password", "", true);
                dialog.ShowDialog();

                this.password = dialog.Text2.Text;

                this.collateralWalletName = "CollateralWallet";
                this.miningWalletName = "MiningWallet";

                this.nextState = "Setup_CreateRestoreUseExisting_Create_CreateCollateralWallet";
            }

            if (this.currentState == "Setup_CreateRestoreUseExisting_Create_CreateCollateralWallet")
            {
                await this.registrationService.RestoreWalletAsync(this.registrationService.MainchainNetwork.DefaultAPIPort, "", this.collateralWalletName, this.mnemonic, this.passphrase, this.password).ConfigureAwait(false);
                await this.registrationService.ResyncWalletAsync(this.registrationService.MainchainNetwork.DefaultAPIPort, this.collateralWalletName).ConfigureAwait(false);

                this.nextState = "Setup_CreateRestoreUseExisting_Create_SyncCollateralWallet";
            }

            if (this.currentState == "Setup_CreateRestoreUseExisting_Create_SyncCollateralWallet")
            {
                int percentSynced = await this.registrationService.WalletSyncProgressAsync(this.registrationService.MainchainNetwork.DefaultAPIPort, this.collateralWalletName).ConfigureAwait(false);
                this.statusBar.Text = $"Main chain collateral wallet {percentSynced}% synced";

                if (await this.registrationService.IsWalletSyncedAsync(this.registrationService.MainchainNetwork.DefaultAPIPort, this.collateralWalletName).ConfigureAwait(false))
                    this.nextState = "Setup_CreateRestoreUseExisting_Create_CreateMiningWallet";
            }

            if (this.currentState == "Setup_CreateRestoreUseExisting_Create_CreateMiningWallet")
            {
                await this.registrationService.RestoreWalletAsync(this.registrationService.SidechainNetwork.DefaultAPIPort, "", this.miningWalletName, this.mnemonic, this.passphrase, this.password).ConfigureAwait(false);
                await this.registrationService.ResyncWalletAsync(this.registrationService.SidechainNetwork.DefaultAPIPort, this.miningWalletName).ConfigureAwait(false);

                this.nextState = "Setup_CreateRestoreUseExisting_Create_SyncMiningWallet";
            }

            if (this.currentState == "Setup_CreateRestoreUseExisting_Create_SyncMiningWallet")
            {
                int percentSynced = await this.registrationService.WalletSyncProgressAsync(this.registrationService.SidechainNetwork.DefaultAPIPort, this.miningWalletName).ConfigureAwait(false);
                this.statusBar.Text = $"Side chain mining wallet {percentSynced}% synced";

                if (await this.registrationService.IsWalletSyncedAsync(this.registrationService.SidechainNetwork.DefaultAPIPort, this.miningWalletName).ConfigureAwait(false))
                    this.nextState = "Setup_CreateRestoreUseExisting_Create_AskForCollateral";
            }

            if (this.currentState == "Setup_CreateRestoreUseExisting_Create_AskForCollateral")
            {
                this.collateralAddress = await this.registrationService.GetFirstWalletAddressAsync(this.registrationService.MainchainNetwork.DefaultAPIPort, this.collateralWalletName).ConfigureAwait(false);

                MessageBox.Show($"Your collateral address is: {this.collateralAddress}", "Collateral Address", MessageBoxButton.OK);

                // The 3 sub-branches recombine after this and can share common states.
                this.nextState = "Setup_CreateRestoreUseExisting_CheckForCollateral";
            }

            if (this.currentState == "Setup_CreateRestoreUseExisting_CheckForCollateral")
            {
                if (await this.registrationService.CheckWalletBalanceAsync(this.registrationService.MainchainNetwork.DefaultAPIPort, this.collateralWalletName, RegistrationService.CollateralRequirement).ConfigureAwait(false))
                    this.nextState = "Setup_CreateRestoreUseExisting_CheckForRegistrationFee";
                else
                    this.statusBar.Text = $"Waiting for collateral wallet to have a balance of at least {RegistrationService.CollateralRequirement} STRAX";
            }

            if (this.currentState == "Setup_CreateRestoreUseExisting_CheckForRegistrationFee")
            {
                if (await this.registrationService.CheckWalletBalanceAsync(this.registrationService.SidechainNetwork.DefaultAPIPort, this.miningWalletName, RegistrationService.FeeRequirement).ConfigureAwait(false))
                    this.nextState = "Setup_CreateRestoreUseExisting_PerformRegistration";
                else
                    this.nextState = "Setup_CreateRestoreUseExisting_PerformCrossChain";
            }

            if (this.currentState == "Setup_CreateRestoreUseExisting_PerformCrossChain")
            {
                if (MessageBox.Show("Insufficient balance in the mining wallet. Perform a cross-chain transfer of 500.1 STRAX?", "Registration Fee Missing", MessageBoxButton.YesNo, MessageBoxImage.Warning, MessageBoxResult.No) == MessageBoxResult.No)
                {
                    this.nextState = null;
                    this.currentState = "Begin"; // TODO: Maybe we don't have to go all the way back to the beginning, but it is unclear what should be done if they select 'No'

                    return true;
                }

                this.cirrusAddress = await this.registrationService.GetFirstWalletAddressAsync(this.registrationService.SidechainNetwork.DefaultAPIPort, this.miningWalletName).ConfigureAwait(false);

                if (await this.registrationService.PerformCrossChainTransferAsync(this.registrationService.MainchainNetwork.DefaultAPIPort, this.collateralWalletName, this.password, "500.1", this.cirrusAddress, this.collateralAddress).ConfigureAwait(false))
                {
                    this.nextState = "Setup_CreateRestoreUseExisting_WaitForCrossChainTransfer";
                }
            }

            if (this.currentState == "Setup_CreateRestoreUseExisting_WaitForCrossChainTransfer")
            {
                this.statusBar.Text = "Waiting for registration fee to be sent via cross-chain transfer...";

                if (await this.registrationService.CheckWalletBalanceAsync(this.registrationService.SidechainNetwork.DefaultAPIPort, this.miningWalletName, RegistrationService.FeeRequirement).ConfigureAwait(false))
                {
                    this.nextState = "Setup_CreateRestoreUseExisting_PerformRegistration";
                }
            }

            if (this.currentState == "Setup_CreateRestoreUseExisting_PerformRegistration")
            {
                await this.registrationService.CallJoinFederationRequestAsync(this.collateralAddress, this.collateralWalletName, this.password, this.miningWalletName, this.password).ConfigureAwait(false);

                this.nextState = "Setup_CreateRestoreUseExisting_WaitForRegistration";
            }

            if (this.currentState == "Setup_CreateRestoreUseExisting_WaitForRegistration")
            {
                if (await this.registrationService.MonitorJoinFederationRequestAsync().ConfigureAwait(false))
                {
                    this.statusBar.Text = "Registration complete";
                    this.nextState = "Run_LaunchBrowser";
                }
            }

            if (this.currentState == "Setup_CreateRestoreUseExisting_Restore")
            {
                // The only material difference is that the user needs to supply their own mnemonic; so just retrieve it via an input dialog and then jump back into the Create sub-branch.
                do
                {
                    var inputBox = new InputBox($"Please enter your 12-word mnemonic:");

                    this.mnemonic = inputBox.ShowDialog();

                    if (!string.IsNullOrEmpty(this.mnemonic))
                    {
                        try
                        {
                            // Test the mnemonic to ensure validity.
                            var temp = new Mnemonic(this.mnemonic, Wordlist.English);

                            // If there was no exception, break out of the loop and continue.
                            break;
                        }
                        catch
                        {
                        }
                    }

                    MessageBox.Show("Please ensure that you enter a valid mnemonic", "Error", MessageBoxButton.OK);
                } while (true);

                this.nextState = "Setup_CreateRestoreUseExisting_Create_Passphrase";
            }

            if (this.currentState == "Setup_CreateRestoreUseExisting_UseExisting")
            {
                // Need to get the names of the wallets for the main chain (collateral) and side chain (mining) nodes.
                do
                {
                    var inputBox = new InputBox($"Please enter your 12-word mnemonic:");

                    this.mnemonic = inputBox.ShowDialog();

                    if (!string.IsNullOrEmpty(this.mnemonic))
                    {
                        try
                        {
                            // Test the mnemonic to ensure validity.
                            var temp = new Mnemonic(this.mnemonic, Wordlist.English);

                            // If there was no exception, break out of the loop and continue.
                            break;
                        }
                        catch
                        {
                        }
                    }

                    MessageBox.Show("Please ensure that you enter a valid mnemonic", "Error", MessageBoxButton.OK);
                } while (true);

                this.nextState = "Setup_CreateRestoreUseExisting_UseExisting_CheckMainWalletSynced";
            }

            if (this.currentState == "Setup_CreateRestoreUseExisting_UseExisting_CheckMainWalletSynced")
            {
                if (await this.registrationService.IsWalletSyncedAsync(this.registrationService.MainchainNetwork.DefaultAPIPort, this.miningWalletName).ConfigureAwait(false))
                {
                    this.nextState = "Setup_CreateRestoreUseExisting_UseExisting_CheckSideWalletSynced";
                }

                this.statusBar.Text = "Waiting for mainchain wallet to sync...";
            }

            if (this.currentState == "Setup_CreateRestoreUseExisting_UseExisting_CheckSideWalletSynced")
            {
                if (await this.registrationService.IsWalletSyncedAsync(this.registrationService.MainchainNetwork.DefaultAPIPort, this.miningWalletName).ConfigureAwait(false))
                {
                    // Now we can jump back into the same sequence as the other 2 sub-branches.
                    this.nextState = "Setup_CreateRestoreUseExisting_Create_AskForCollateral";
                }

                this.statusBar.Text = "Waiting for sidechain wallet to sync...";
            }

            return false;
        }

        private async void Button_ClickAsync(object sender, RoutedEventArgs e)
        {
            Button button = sender as Button;

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
    }
}
