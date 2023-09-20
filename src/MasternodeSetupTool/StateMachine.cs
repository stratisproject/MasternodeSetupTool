using System;
using System.Collections.Generic;
using System.Reflection;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Threading;
using MasternodeSetupTool;
using NBitcoin;
using static MasternodeSetupTool.RegistrationService;

namespace MasternodeSetupTool;

public class StateMachine: ILogger
{
    public enum State
    {
        Begin,
        RunMasterNode_KeyPresent,
        Run_StartMainChain,
        Run_MainChainSynced,
        Run_StartSideChain,
        Run_SideChainSynced,
        Run_LaunchBrowser,
        SetupMasterNode_Eula,
        Setup_KeyPresent,
        Setup_CreateRestoreUseExisting_StartMainChain,
        Setup_CreateKey,
        Setup_CreateRestoreUseExisting_MainChainSynced,
        Setup_CreateRestoreUseExisting_StartSideChain,
        Setup_CreateRestoreUseExisting_SideChainSynced,
        Setup_CreateRestoreUseExisting_Create_AskForCollateral,
        Setup_CreateRestoreUseExisting_CheckIsFederationMember,
        Setup_CreateRestoreUseExisting_Select,
        Setup_CreateRestoreUseExisting_Create,
        Setup_CreateRestoreUseExisting_Create_Mining,
        Setup_CreateRestoreUseExisting_CheckForCollateral,
        Setup_CreateRestoreUseExisting_Restore,
        Setup_CreateRestoreUseExisting_Restore_Mining,
        Setup_CreateRestoreUseExisting_CheckForRegistrationFee,
        Setup_CreateRestoreUseExisting_PerformRegistration,
        Setup_CreateRestoreUseExisting_WaitForBalance,
        Setup_CreateRestoreUseExisting_WaitForRegistration,
        Setup_CreateRestoreUseExisting_UseExisting,
        Setup_CreateRestoreUseExisting_UseExisting_CollateralPassword,
        Setup_CreateRestoreUseExisting_UseExisting_Mining,
        Setup_CreateRestoreUseExisting_UseExisting_MiningPassword,
        Setup_CreateRestoreUseExisting_UseExisting_CheckMainWalletSynced,
        Setup_CreateRestoreUseExisting_UseExisting_CheckSideWalletSynced
    }

    private readonly RegistrationService registrationService;
    private readonly IStateHandler stateHandler;

    private State currentState = State.Begin;
    private State? nextState = null;

    public WalletCredentials? collateralWalletCredentials;
    public WalletCredentials? miningWalletCredentials;

    public WalletCreationState? collateralWalletCreationState;
    public WalletCreationState? miningWalletCreationState;


    private bool IsEnabled = true;

    public StateMachine(NetworkType networkType, IStateHandler stateHandler)
    {
        this.stateHandler = stateHandler;
        this.registrationService = new RegistrationService(networkType, this);


        this.stateHandler.OnProgramVersionAvailable(GetInformationalVersion()).GetAwaiter().GetResult();
    }

    public void OnRunNode()
    {
        this.nextState = State.RunMasterNode_KeyPresent;
    }

    public void OnSetupNode()
    {
        this.nextState = State.SetupMasterNode_Eula;
    }

    public async Task TickAsync()
    {
        var thread = System.Threading.Thread.CurrentThread;

        if (!this.IsEnabled) return;

        this.IsEnabled = false;

        if (this.currentState == State.Begin)
        {
            await this.stateHandler.OnStart();
        }

        if (this.nextState == null)
        {
            this.IsEnabled = true;

            return;
        }

        this.currentState = (State)this.nextState;
        this.nextState = null;

        if (await RunBranchAsync())
        {
            this.IsEnabled = true;

            return;
        }

        if (await SetupBranchAsync())
        {
            this.IsEnabled = true;

            return;
        }

        this.IsEnabled = true;
    }

    private async Task<bool> RunBranchAsync()
    {
        // The 'Run' branch

        var thread = System.Threading.Thread.CurrentThread;

        if (this.currentState == State.RunMasterNode_KeyPresent)
        {
            if (!this.registrationService.CheckFederationKeyExists())
            {
                await this.stateHandler.OnFederationKeyMissing();

                ResetState();

                return true;
            }

            this.nextState = State.Run_StartMainChain;
        }

        if (this.currentState == State.Run_StartMainChain)
        {
            if (!await this.registrationService.StartNodeAsync(NodeType.MainChain, this.registrationService.MainchainNetwork.DefaultAPIPort).ConfigureAwait(true))
            {
                await this.stateHandler.OnNodeFailedToStart(NodeType.MainChain);
                return false;
            }

            this.nextState = State.Run_MainChainSynced;
        }

        if (this.currentState == State.Run_MainChainSynced)
        {
            await this.registrationService.EnsureNodeIsInitializedAsync(NodeType.MainChain, this.registrationService.MainchainNetwork.DefaultAPIPort).ConfigureAwait(true);

            await this.registrationService.EnsureMainChainNodeAddressIndexerIsSyncedAsync().ConfigureAwait(true);

            await this.registrationService.EnsureBlockstoreIsSyncedAsync(NodeType.MainChain, this.registrationService.MainchainNetwork.DefaultAPIPort).ConfigureAwait(true);

            this.nextState = State.Run_StartSideChain;
        }

        if (this.currentState == State.Run_StartSideChain)
        {
            if (!await this.registrationService.StartNodeAsync(NodeType.SideChain, this.registrationService.SidechainNetwork.DefaultAPIPort).ConfigureAwait(true))
            {
                await this.stateHandler.OnNodeFailedToStart(NodeType.SideChain);

                return false;
            }

            this.nextState = State.Run_SideChainSynced;
        }

        if (this.currentState == State.Run_SideChainSynced)
        {
            await this.registrationService.EnsureNodeIsInitializedAsync(NodeType.SideChain, this.registrationService.SidechainNetwork.DefaultAPIPort).ConfigureAwait(true);

            await this.registrationService.EnsureNodeIsSyncedAsync(NodeType.SideChain, this.registrationService.SidechainNetwork.DefaultAPIPort).ConfigureAwait(true);

            await this.registrationService.EnsureBlockstoreIsSyncedAsync(NodeType.SideChain, this.registrationService.SidechainNetwork.DefaultAPIPort).ConfigureAwait(true);

            this.nextState = State.Run_LaunchBrowser;
        }

        if (this.currentState == State.Run_LaunchBrowser)
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
        if (this.currentState == State.SetupMasterNode_Eula)
        {
            if (!await this.stateHandler.OnAskForEULA())
            {
                ResetState();
                return true;
            }

            this.nextState = State.Setup_KeyPresent;
        }

        if (this.currentState == State.Setup_KeyPresent)
        {
            if (this.registrationService.CheckFederationKeyExists())
            {
                if (!await this.stateHandler.OnAskForNewFederationKey())
                {
                    this.nextState = State.Setup_CreateRestoreUseExisting_StartMainChain;
                    return true;
                }

                this.registrationService.DeleteFederationKey();
            }

            this.nextState = State.Setup_CreateKey;
        }

        if (this.currentState == State.Setup_CreateKey)
        {
            string savePath = this.registrationService.CreateFederationKey();

            await this.stateHandler.OnShowNewFederationKey(this.registrationService.PubKey, savePath);

            this.nextState = State.Setup_CreateRestoreUseExisting_StartMainChain;
        }

        if (this.currentState == State.Setup_CreateRestoreUseExisting_StartMainChain)
        {
            // All 3 sub-branches of this state require the mainchain and sidechain nodes to be initialized, so do that first.
            if (!await this.registrationService.StartNodeAsync(NodeType.MainChain, this.registrationService.MainchainNetwork.DefaultAPIPort).ConfigureAwait(true))
            {
                await this.stateHandler.OnNodeFailedToStart(NodeType.MainChain);
                ResetState();

                return true;
            }

            this.nextState = State.Setup_CreateRestoreUseExisting_MainChainSynced;
        }

        if (this.currentState == State.Setup_CreateRestoreUseExisting_MainChainSynced)
        {
            await this.registrationService.EnsureNodeIsInitializedAsync(NodeType.MainChain, this.registrationService.MainchainNetwork.DefaultAPIPort).ConfigureAwait(true);

            await this.registrationService.EnsureMainChainNodeAddressIndexerIsSyncedAsync().ConfigureAwait(true);

            await this.registrationService.EnsureBlockstoreIsSyncedAsync(NodeType.MainChain, this.registrationService.MainchainNetwork.DefaultAPIPort).ConfigureAwait(true);

            this.nextState = State.Setup_CreateRestoreUseExisting_StartSideChain;
        }

        if (this.currentState == State.Setup_CreateRestoreUseExisting_StartSideChain)
        {
            if (!await this.registrationService.StartNodeAsync(NodeType.SideChain, this.registrationService.SidechainNetwork.DefaultAPIPort).ConfigureAwait(true))
            {
                await this.stateHandler.OnNodeFailedToStart(NodeType.SideChain);
                ResetState();

                return true;
            }

            this.nextState = State.Setup_CreateRestoreUseExisting_SideChainSynced;
        }

        if (this.currentState == State.Setup_CreateRestoreUseExisting_SideChainSynced)
        {
            await this.registrationService.EnsureNodeIsInitializedAsync(NodeType.SideChain, this.registrationService.SidechainNetwork.DefaultAPIPort).ConfigureAwait(true);

            await this.registrationService.EnsureNodeIsSyncedAsync(NodeType.SideChain, this.registrationService.SidechainNetwork.DefaultAPIPort).ConfigureAwait(true);

            await this.registrationService.EnsureBlockstoreIsSyncedAsync(NodeType.SideChain, this.registrationService.SidechainNetwork.DefaultAPIPort).ConfigureAwait(true);

            this.nextState = State.Setup_CreateRestoreUseExisting_CheckIsFederationMember;
        }

        if (this.currentState == State.Setup_CreateRestoreUseExisting_CheckIsFederationMember)
        {
            if (await this.registrationService.CheckIsFederationMemberAsync().ConfigureAwait(true))
            {
                if (await this.stateHandler.OnAskToRunIfAlreadyMember())
                {
                    this.nextState = State.Run_LaunchBrowser;
                    return true;
                }
                else
                {
                    await this.stateHandler.OnAlreadyMember();
                    ResetState();
                    return true;
                }
            }

            this.nextState = State.Setup_CreateRestoreUseExisting_Select;
        }

        if (this.currentState == State.Setup_CreateRestoreUseExisting_Select)
        {
            //TODO: Probably we need to show picker for collateral and mining wallets independently
            switch (await this.stateHandler.OnAskForWalletSource(NodeType.MainChain))
            {
                case WalletSource.NewWallet:
                    this.nextState = State.Setup_CreateRestoreUseExisting_Create;
                    break;
                case WalletSource.RestoreWallet:
                    this.nextState = State.Setup_CreateRestoreUseExisting_Restore;
                    break;
                case WalletSource.UseExistingWallet:
                    this.nextState = State.Setup_CreateRestoreUseExisting_UseExisting;
                    break;
                default:
                    await this.stateHandler.OnRegistrationCanceled();
                    ResetState();
                    return true;
            }
        }

        if (this.currentState == State.Setup_CreateRestoreUseExisting_Create)
        {

            if (!await HandleCreateWalletsAsync(NodeType.MainChain, createNewMnemonic: true))
            {
                this.nextState = State.Setup_CreateRestoreUseExisting_Select;
                return true;
            }

            this.nextState = State.Setup_CreateRestoreUseExisting_Create_Mining;
        }

        if (this.currentState == State.Setup_CreateRestoreUseExisting_Create_Mining)
        {

            if (!await HandleCreateWalletsAsync(NodeType.SideChain, createNewMnemonic: true))
            {
                this.nextState = State.Setup_CreateRestoreUseExisting_Select;
                return true;
            }

            this.nextState = State.Setup_CreateRestoreUseExisting_Create_AskForCollateral;
        }

        if (this.currentState == State.Setup_CreateRestoreUseExisting_Create_AskForCollateral)
        {
            this.collateralWalletCredentials.ChoosenAddress = await HandleAddressSelectionAsync(NodeType.MainChain, this.collateralWalletCredentials.Name);

            if (this.collateralWalletCredentials.ChoosenAddress == null)
            {
                this.collateralWalletCredentials.ChoosenAddress = await this.registrationService.GetFirstWalletAddressAsync(this.registrationService.MainchainNetwork.DefaultAPIPort, this.collateralWalletCredentials.Name).ConfigureAwait(true);
            }

            // The 3 sub-branches recombine after this and can share common states.
            this.nextState = State.Setup_CreateRestoreUseExisting_CheckForCollateral;
        }

        if (this.currentState == State.Setup_CreateRestoreUseExisting_CheckForCollateral)
        {
            if (await this.registrationService.CheckWalletBalanceAsync(this.registrationService.MainchainNetwork.DefaultAPIPort, this.collateralWalletCredentials.Name, RegistrationService.CollateralRequirement).ConfigureAwait(true))
            {
                this.nextState = State.Setup_CreateRestoreUseExisting_CheckForRegistrationFee;
            }
            else
            {
                await this.stateHandler.OnWaitingForCollateral();
                this.nextState = State.Setup_CreateRestoreUseExisting_CheckForCollateral;
            }
        }

        if (this.currentState == State.Setup_CreateRestoreUseExisting_CheckForRegistrationFee)
        {
            if (await this.registrationService.CheckWalletBalanceAsync(this.registrationService.SidechainNetwork.DefaultAPIPort, this.miningWalletCredentials.Name, RegistrationService.FeeRequirement).ConfigureAwait(true))
            {
                this.nextState = State.Setup_CreateRestoreUseExisting_PerformRegistration;
            }
            else
            {
                string? miningAddress = await this.registrationService.GetFirstWalletAddressAsync(this.registrationService.SidechainNetwork.DefaultAPIPort, this.miningWalletCredentials.Name).ConfigureAwait(true);
                this.miningWalletCredentials.ChoosenAddress = miningAddress;
                await this.stateHandler.OnMissingRegistrationFee(miningAddress);
                this.nextState = State.Setup_CreateRestoreUseExisting_WaitForBalance;
            }
        }

        if (this.currentState == State.Setup_CreateRestoreUseExisting_WaitForBalance)
        {

            if (await this.registrationService.CheckWalletBalanceAsync(this.registrationService.SidechainNetwork.DefaultAPIPort, this.miningWalletCredentials.Name, RegistrationService.FeeRequirement).ConfigureAwait(true))
            {
                this.nextState = State.Setup_CreateRestoreUseExisting_PerformRegistration;
            }
            else
            {
                await this.stateHandler.OnWaitingForRegistrationFee();
                this.nextState = State.Setup_CreateRestoreUseExisting_WaitForBalance;
            }
        }

        if (this.currentState == State.Setup_CreateRestoreUseExisting_PerformRegistration)
        {
            bool registeredSuccessfully = await this.registrationService.CallJoinFederationRequestAsync(this.collateralWalletCredentials, this.miningWalletCredentials).ConfigureAwait(true);
            if (!registeredSuccessfully)
            {
                await this.stateHandler.OnRegistrationFailed();
                ResetState();
                return true;
            }

            this.nextState = State.Setup_CreateRestoreUseExisting_WaitForRegistration;
        }

        if (this.currentState == State.Setup_CreateRestoreUseExisting_WaitForRegistration)
        {
            if (await this.registrationService.MonitorJoinFederationRequestAsync().ConfigureAwait(true))
            {
                await this.stateHandler.OnRegistrationComplete();
                this.nextState = State.Run_LaunchBrowser;
            }
        }

        if (this.currentState == State.Setup_CreateRestoreUseExisting_Restore)
        {
            if (!await HandleCreateWalletsAsync(NodeType.MainChain, createNewMnemonic: false))
            {
                this.nextState = State.Setup_CreateRestoreUseExisting_Select;
                return true;
            }

            this.nextState = State.Setup_CreateRestoreUseExisting_Restore_Mining;
        }

        if (this.currentState == State.Setup_CreateRestoreUseExisting_Restore_Mining)
        {
            if (!await HandleCreateWalletsAsync(NodeType.SideChain, createNewMnemonic: false))
            {
                this.nextState = State.Setup_CreateRestoreUseExisting_Select;
                return true;
            }

            this.nextState = State.Setup_CreateRestoreUseExisting_Create_AskForCollateral;
        }

        if (this.currentState == State.Setup_CreateRestoreUseExisting_UseExisting)
        {
            this.collateralWalletCredentials = new WalletCredentials();
            if (!await HandleExistingWalletNameAsync(NodeType.MainChain, this.collateralWalletCredentials))
            {
                this.nextState = State.Setup_CreateRestoreUseExisting_Select;
                return true;
            }

            this.nextState = State.Setup_CreateRestoreUseExisting_UseExisting_CollateralPassword;
        }

        if (this.currentState == State.Setup_CreateRestoreUseExisting_UseExisting_CollateralPassword)
        {
            if (!await HandleExistingPasswordAsync(NodeType.MainChain, collateralWalletCredentials))
            {
                this.nextState = State.Setup_CreateRestoreUseExisting_Select;
                return true;
            }

            this.nextState = State.Setup_CreateRestoreUseExisting_UseExisting_Mining;
        }

        if (this.currentState == State.Setup_CreateRestoreUseExisting_UseExisting_Mining)
        {
            this.miningWalletCredentials = new WalletCredentials();
            if (!await HandleExistingWalletNameAsync(NodeType.SideChain,this.miningWalletCredentials))
            {
                this.nextState = State.Setup_CreateRestoreUseExisting_Select;
                return true;
            }

            this.nextState = State.Setup_CreateRestoreUseExisting_UseExisting_MiningPassword;
        }

        if (this.currentState == State.Setup_CreateRestoreUseExisting_UseExisting_MiningPassword)
        {
            if (!await HandleExistingPasswordAsync(NodeType.SideChain, miningWalletCredentials))
            {
                this.nextState = State.Setup_CreateRestoreUseExisting_Select;
                return true;
            }

            this.nextState = State.Setup_CreateRestoreUseExisting_UseExisting_CheckMainWalletSynced;
        }

        if (this.currentState == State.Setup_CreateRestoreUseExisting_UseExisting_CheckMainWalletSynced)
        {
            if (await HandleWalletSyncAsync(NodeType.MainChain))
            {
                this.nextState = State.Setup_CreateRestoreUseExisting_UseExisting_CheckSideWalletSynced;
            }
            else
            {
                return true;
            }
        }

        if (this.currentState == State.Setup_CreateRestoreUseExisting_UseExisting_CheckSideWalletSynced)
        {
            if (await HandleWalletSyncAsync(NodeType.SideChain))
            {
                // Now we can jump back into the same sequence as the other 2 sub-branches.
                this.nextState = State.Setup_CreateRestoreUseExisting_Create_AskForCollateral;
            }
            else
            {
                return true;
            }
        }

        return false;
    }

    private async Task<string?> HandleAddressSelectionAsync(NodeType nodeType, string walletName)
    {
        Network network = nodeType == NodeType.MainChain
            ? this.registrationService.MainchainNetwork
            : this.registrationService.SidechainNetwork;

        List<AddressItem>? addressesWithBalance = await this.registrationService.GetWalletAddressesAsync(walletName, network.DefaultAPIPort);

        if (addressesWithBalance != null)
        {
            return await this.stateHandler.OnChooseAddress(addressesWithBalance, nodeType);
        }

        return null;
    }

    private async Task<bool> HandleExistingWalletNameAsync(NodeType nodeType, WalletCredentials walletCredentials)
    {
        Network network = nodeType == NodeType.MainChain
            ? this.registrationService.MainchainNetwork
            : this.registrationService.SidechainNetwork;

        List<WalletItem>? walletsWithBalance = await this.registrationService.GetWalletsWithBalanceAsync(network.DefaultAPIPort);

        if (walletsWithBalance != null)
        {
            string? walletName = await this.stateHandler.OnChooseWallet(walletsWithBalance, nodeType);

            if (walletName == null)
                return false;

            walletCredentials.Name = walletName;

            return true;
        }

        return false;
    }

    private async Task<bool> HandleNewWalletNameAsync(NodeType nodeType, WalletCreationState creationState)
    {
        do
        {
            string? walletName = await this.stateHandler.OnAskForWalletName(nodeType, true);

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
                        creationState.Name = walletName;
                        break;
                    }
                    else
                    {
                        await this.stateHandler.OnWalletNameExists(nodeType);
                    }
                }
                catch
                {
                }
            }

            await this.stateHandler.OnWalletExistsOrInvalid(nodeType);
        } while (true);

        return true;
    }

    private async Task<bool> HandleNewMnemonicAsync(NodeType nodeType, WalletCreationState creationState, bool canChangeMnemonic = false)
    {
        var mnemonic = string.Join(' ', new Mnemonic("English", WordCount.Twelve).Words);

        if (await this.stateHandler.OnAskForMnemonicConfirmation(nodeType, mnemonic))
        {
            return false;
        }

        creationState.Mnemonic = mnemonic;

        return true;
    }

    private async Task<bool> HandleUserMnemonic(NodeType nodeType, WalletCreationState creationState)
    {
        string? mnemonic;

        do
        {
            mnemonic = await this.stateHandler.OnAskForUserMnemonic(nodeType);

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

            await this.stateHandler.OnMnemonicIsInvalid(nodeType);
        } while (true);

        creationState.Mnemonic = mnemonic;

        return true;
    }

    private async Task<bool> HandlePassphraseAsync(NodeType nodeType, WalletCreationState creationState)
    {
        string result = await this.stateHandler.OnAskForPassphrase(nodeType);

        creationState.Passphrase = result;

        return true;
    }

    private async Task<bool> HandleExistingPasswordAsync(NodeType nodeType, WalletCredentials credentials)
    {
        while (true)
        {
            string password = await this.stateHandler.OnAskForWalletPassword(nodeType);

            if (await this.registrationService.CheckWalletPasswordAsync(NodeApiPort(nodeType), credentials.Name, password) == false)
            {
                if (!await this.stateHandler.OnAskReenterPassword(nodeType))
                {
                    return false;
                }

                continue;
            }

            credentials.Password = password;

            return true;
        }
    }

    private async Task<bool> HandleNewPasswordAsync(NodeType nodeType, WalletCreationState creationState)
    {
        string password = await this.stateHandler.OnAskForWalletPassword(nodeType);

        if (password == null)
        {
            return false;
        }

        creationState.Password = password;
        return true;
    }

    private async Task<bool> HandleWalletCreationAsync(NodeType nodeType, WalletCreationState walletCreationState, bool createNewWallet)
    {
        Network network = nodeType == NodeType.MainChain
            ? this.registrationService.MainchainNetwork
            : this.registrationService.SidechainNetwork;

        if (walletCreationState == null ||!walletCreationState.IsValid())
        {
            return false;
        }

        while (true)
        {
            try
            {
                if (!await this.registrationService.RestoreWalletAsync(network.DefaultAPIPort, nodeType, walletCreationState.Name, walletCreationState.Mnemonic, walletCreationState.Passphrase, walletCreationState.Password, createNewWallet).ConfigureAwait(true))
                {
                    if (createNewWallet)
                        await this.stateHandler.OnCreateWalletFailed(nodeType);
                    else
                        await this.stateHandler.OnRestoreWalletFailed(nodeType);

                    return false;
                }
                break;
            }
            catch (WalletCollisionException)
            {
                await this.stateHandler.OnMnemonicExists(nodeType);

                if (!await HandleNewMnemonicAsync(nodeType, walletCreationState, canChangeMnemonic: true))
                {
                    await this.stateHandler.OnRegistrationCanceled();
                    return false;
                }
            }
        }

        if (!await this.registrationService.ResyncWalletAsync(network.DefaultAPIPort, walletCreationState.Name).ConfigureAwait(true))
        {
            await this.stateHandler.OnResyncFailed(nodeType);
            return false;
        }

        WalletCredentials newCredentials = new WalletCredentials
        {
            Name = walletCreationState.Name,
            Password = walletCreationState.Password,
        };

        if (nodeType == NodeType.MainChain)
        {
            this.collateralWalletCredentials = newCredentials;
        }
        else if (nodeType == NodeType.SideChain)
        {
            this.miningWalletCredentials = newCredentials;
        }

        return true;
    }

    private async Task<bool> HandleWalletSyncAsync(NodeType nodeType)
    {
        Network network = nodeType == NodeType.MainChain
            ? this.registrationService.MainchainNetwork
            : this.registrationService.SidechainNetwork;

        string? walletName = nodeType == NodeType.MainChain
            ? this.collateralWalletCredentials.Name
            : this.miningWalletCredentials.Name;

        if (walletName == null)
        {
            throw new ArgumentException("Wallet name can not be null.");
        }

        int percentSynced = await this.registrationService.WalletSyncProgressAsync(network.DefaultAPIPort, walletName).ConfigureAwait(true);
        this.stateHandler.OnWalletSyncing(nodeType, percentSynced);
        if (await this.registrationService.IsWalletSyncedAsync(network.DefaultAPIPort, walletName).ConfigureAwait(true))
        {
            this.stateHandler.OnWalletSynced(nodeType);
            return true;
        }
        else
        {
            return false;
        }
    }

    private async Task<bool> HandleCreateWalletsAsync(NodeType nodeType, bool createNewMnemonic)
    {
        WalletCreationState walletCreationState = new WalletCreationState();

        if (createNewMnemonic)
        {
            if (!await HandleNewMnemonicAsync(nodeType, walletCreationState))
            {
                return false;
            }
        }
        else
        {
            if (!await HandleUserMnemonic(nodeType, walletCreationState))
            {
                return false;
            }
        }

        if (!await HandleNewWalletNameAsync(nodeType, walletCreationState))
        {
            return false;
        }

        if (!await HandlePassphraseAsync(nodeType, walletCreationState))
        {
            return false;
        }

        if (!await HandleNewPasswordAsync(nodeType, walletCreationState))
        {
            return false;
        }

        if (!await HandleWalletCreationAsync(nodeType, walletCreationState, createNewMnemonic))
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

    private void ResetState()
    {
        this.nextState = null;
        this.currentState = State.Begin;
    }

    public static string? GetInformationalVersion() =>
        Assembly
            .GetExecutingAssembly()
            ?.GetCustomAttribute<AssemblyInformationalVersionAttribute>()
            ?.InformationalVersion;

    private string WalletTypeName(NodeType nodeType)
    {
        return nodeType == NodeType.MainChain ? "collateral" : "mining";
    }

    private WalletCredentials? GetWalletCredentials(NodeType nodeType)
    {
        if (nodeType == NodeType.MainChain)
        {
            return collateralWalletCredentials;
        }
        else
        {
            return miningWalletCredentials;
        }
    }

    private int NodeApiPort(NodeType nodeType)
    {
        Network network = nodeType == NodeType.MainChain ? this.registrationService.MainchainNetwork : this.registrationService.SidechainNetwork;
        return network.DefaultAPIPort;
    }

    public void Info(string message, string? updateTag = null)
    {
        this.stateHandler.Info(message, updateTag);
    }

    public void Error(string message)
    {
        this.stateHandler.Error(message);
    }

    public void Error(Exception exception)
    {
        this.stateHandler.Error(exception);
    }

    public void Error(string message, Exception exception)
    {
        this.stateHandler.Error(message, exception);
    }
}