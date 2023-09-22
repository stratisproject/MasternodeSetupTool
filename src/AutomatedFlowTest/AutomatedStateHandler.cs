using MasternodeSetupTool;

using NodeType = MasternodeSetupTool.NodeType;

public class AutomatedStateHandler : IStateHandler
{
    private Configuration configuration;
    private ILogger logger;

    public AutomatedStateHandler(Configuration configuration, ILogger logger)
    {
        this.configuration = configuration;
        this.logger = logger;
    }

    public void Error(string message)
    {
        this.logger.Log($"Error: {message}");
    }

    public void Error(Exception exception)
    {
        this.logger.Log($"Error: {exception}");
    }

    public void Error(string message, Exception exception)
    {
        this.logger.Log($"Error: {message}");
        this.logger.Log($"Error: {exception}");
    }

    public void Info(string message, string? updateTag = null)
    {
        this.logger.Log($"Info: {message}");
    }

    public async Task OnAlreadyMember()
    {
        this.logger.Log("This node is already a member");
    }

    public async Task<string?> OnAskCreatePassword(NodeType nodeType)
    {
        this.logger.Log($"Asked to create a password for {nodeType} wallet");

        if (nodeType == NodeType.MainChain)
        {
            return this.configuration.collateralWalletPassword;
        }
        else
        {
            return this.configuration.miningWalletPassword;
        }
    }

    public async Task<bool> OnAskForEULA()
    {
        this.logger.Log($"Asked for EULA");
        return true;
    }

    public async Task<bool> OnAskForMnemonicConfirmation(NodeType nodeType, string mnemonic)
    {
        this.logger.Log($"Asked for mnemonic confirmation");
        this.logger.Log($"Mnemonic: {mnemonic}");
        return true;
    }

    public async Task<bool> OnAskForNewFederationKey()
    {
        this.logger.Log($"Asked for new federation key, deny");
        return false;
    }

    public async Task<string?> OnAskForPassphrase(NodeType nodeType)
    {
        this.logger.Log($"Asked to create a passphrase for {nodeType} wallet");

        if (nodeType == NodeType.MainChain)
        {
            return this.configuration.collateralWalletPassphrase;
        }
        else
        {
            return this.configuration.miningWalletPassphrase;
        }
    }

    public async Task<string?> OnAskForUserMnemonic(NodeType nodeType)
    {
        this.logger.Log($"Asked a mnemonic for {nodeType} wallet");

        if (nodeType == NodeType.MainChain)
        {
            return this.configuration.collateralWalletMnemonic;
        }
        else
        {
            return this.configuration.miningWalletMnemonic;
        }
    }

    public async Task<string?> OnAskForWalletName(NodeType nodeType, bool newWallet)
    {
        this.logger.Log($"Asked a wallet name for {nodeType} wallet");

        if (nodeType == NodeType.MainChain)
        {
            return this.configuration.collateralWalletName;
        }
        else
        {
            return this.configuration.miningWalletName;
        }
    }

    public async Task<string?> OnAskForWalletPassword(NodeType nodeType)
    {
        this.logger.Log($"Asked a password for {nodeType} wallet");

        if (nodeType == NodeType.MainChain)
        {
            return this.configuration.collateralWalletPassword;
        }
        else
        {
            return this.configuration.miningWalletPassword;
        }
    }

    public async Task<WalletSource?> OnAskForWalletSource(NodeType nodeType)
    {
        this.logger.Log($"Asked a wallet source for {nodeType} wallet");

        if (nodeType == NodeType.MainChain)
        {
            return this.configuration.collateralWalletSource;
        }
        else
        {
            return this.configuration.miningWalletSource;
        }
    }

    public async Task<bool> OnAskReenterPassword(NodeType nodeType)
    {
        this.logger.Log($"Asked to reenter password, deny");
        return false;
    }

    public async Task<bool> OnAskToRunIfAlreadyMember()
    {
        this.logger.Log($"Already a member, stopping");
        return false;
    }

    public async Task<string?> OnChooseAddress(List<AddressItem> addresses, NodeType nodeType)
    {
        this.logger.Log($"Choosing address {addresses.FirstOrDefault()?.Address}");
        return addresses.FirstOrDefault()?.Address;
    }

    public async Task<string?> OnChooseWallet(List<WalletItem> wallets, NodeType nodeType)
    {
        this.logger.Log($"Choosing wallet {wallets.FirstOrDefault()?.Name}");
        return wallets.FirstOrDefault()?.Name;
    }

    public async Task OnCreateWalletFailed(NodeType nodeType)
    {
        this.logger.Log($"{nodeType} wallet creation failed");
    }

    public async Task OnFederationKeyMissing()
    {
        this.logger.Log($"Missing federation key");
    }

    public async Task OnMissingRegistrationFee(string address)
    {
        this.logger.Log($"Missing registration fee on address: {address}");
    }

    public async Task OnMnemonicExists(NodeType nodeType)
    {
        this.logger.Log($"{nodeType} wallet mnemonic already exists");
    }

    public async Task OnMnemonicIsInvalid(NodeType nodeType)
    {
        this.logger.Log($"{nodeType} wallet mnemonic is invalid");
    }

    public async Task OnNodeFailedToStart(NodeType nodeType, string? reason = null)
    {
        this.logger.Log($"{nodeType} node failed to start");
        this.logger.Log($"Reason: {reason}");
    }

    public async Task OnProgramVersionAvailable(string? version)
    {
        this.logger.Log($"App version: {version ?? "null"}");
    }

    public async Task OnRegistrationCanceled()
    {
        this.logger.Log($"Registration canceled");
    }

    public async Task OnRegistrationComplete()
    {
        this.logger.Log($"Registration complete");
    }

    public async Task OnRegistrationFailed()
    {
        this.logger.Log($"Registration failed");
    }

    public async Task OnRestoreWalletFailed(NodeType nodeType)
    {
        this.logger.Log($"{nodeType} wallet restore failed");
    }

    public async Task OnResyncFailed(NodeType nodeType)
    {
        this.logger.Log($"{nodeType} wallet resync failed");
    }

    public async Task OnShowNewFederationKey(string pubKey, string savePath)
    {
        this.logger.Log($"New pubKey is: {pubKey}");
        this.logger.Log($"New savePath is: {savePath}");
    }

    public async Task OnShowWalletAddress(NodeType nodeType, string address)
    {
        this.logger.Log($"{nodeType} wallet address is {address}");
    }

    public async Task OnShowWalletName(NodeType nodeType, string walletName)
    {
        this.logger.Log($"{nodeType} wallet name is {walletName}");
    }

    public async Task OnStart()
    {
        this.logger.Log($"Started");
    }

    public async Task OnWaitingForCollateral()
    {
        this.logger.Log($"Waiting for collateral");
    }

    public async Task OnWaitingForRegistrationFee()
    {
        this.logger.Log($"Waiting for registration fee");
    }

    public async Task OnWalletExistsOrInvalid(NodeType nodeType)
    {
        this.logger.Log($"{nodeType} wallet exists or invalid");
    }

    public async Task OnWalletNameExists(NodeType nodeType)
    {
        this.logger.Log($"{nodeType} wallet name already exists");
    }

    public async Task OnWalletSynced(NodeType nodeType)
    {
        this.logger.Log($"{nodeType} wallet synced");
    }

    public async Task OnWalletSyncing(NodeType nodeType, int progress)
    {
        this.logger.Log($"{nodeType} wallet is syncing, {progress}%");
    }

    public interface ILogger
    {
        void Log(string message);
    }
}
