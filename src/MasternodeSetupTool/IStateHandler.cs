using System.Collections.Generic;
using System.Threading.Tasks;
using MasternodeSetupTool;

namespace MasternodeSetupTool;

public interface IStateHandler
{
    public Task OnStart();

    public Task OnProgramVersionAvailable(string? version);

    public Task OnFederationKeyMissing();

    public Task OnNodeFailedToStart(NodeType nodeType, string? reason = null);

    public Task<bool> OnAskForEULA();

    public Task<bool> OnAskForNewFederationKey();

    public Task OnShowNewFederationKey(string pubKey, string savePath);

    public Task<bool> OnAskToRunIfAlreadyMember();

    public Task OnAlreadyMember();

    public Task<WalletSource?> OnAskForWalletSource(NodeType nodeType);

    public Task<string?> OnChooseWallet(List<WalletItem> wallets, NodeType nodeType);

    public Task<string?> OnChooseAddress(List<AddressItem> wallets, NodeType nodeType);

    public Task OnWaitingForCollateral();

    public Task OnWaitingForRegistrationFee();

    public Task OnMissingRegistrationFee(string address);

    public Task OnWaitForRegistration();

    public Task OnRegistrationCanceled();

    public Task OnRegistrationComplete();
    public Task OnRegistrationFailed();

    public Task<bool> OnAskForMnemonicConfirmation(NodeType nodeType, string mnemonic);

    public Task<string?> OnAskForUserMnemonic(NodeType nodeType);

    public Task<string?> OnAskForWalletName(NodeType nodeType, bool newWallet);

    public Task<string?> OnAskForPassphrase(NodeType nodeType);

    public Task<string?> OnAskForWalletPassword(NodeType nodeType);

    public Task<string?> OnAskCreatePassword(NodeType nodeType);

    public Task<bool> OnAskReenterPassword(NodeType nodeType);

    public Task OnWalletNameExists();

    public Task OnMnemonicIsInvalid();

    public Task OnMnemonicExists();
    
    public Task OnWalletExistsOrInvalid(NodeType nodeType);

    public Task OnWalletSyncing(NodeType nodeType, int progress);

    public Task OnWalletSynced(NodeType nodeType);

    public Task OnShowWalletName(NodeType nodeType, string walletName);

    public Task OnShowWalletAddress(NodeType nodeType, string address);

    public Task OnRestoreWalletFailed(NodeType nodeType);
    public Task OnCreateWalletFailed(NodeType nodeType);
    public Task OnResyncFailed(NodeType nodeType);
}

public enum WalletSource
{
    NewWallet, RestoreWallet, UseExistingWallet
}