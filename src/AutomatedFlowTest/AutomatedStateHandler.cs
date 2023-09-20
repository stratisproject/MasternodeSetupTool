// See https://aka.ms/new-console-template for more information
using ICSharpCode.Decompiler.CSharp.Syntax;
using ICSharpCode.Decompiler.IL;
using MasternodeSetupTool;
using NBitcoin;
using Stratis.Bitcoin.Features.Wallet.Models;
using Stratis.SmartContracts;
using NodeType = MasternodeSetupTool.NodeType;

class AutomatedStateHandler : IStateHandler
{
    private Configuration configuration;

    public AutomatedStateHandler(Configuration configuration)
    {
        this.configuration = configuration;
    }

    public void Error(string message)
    {
        Console.WriteLine($"Error: {message}");
    }

    public void Error(Exception exception)
    {
        Console.WriteLine($"Error: {exception}");
    }

    public void Error(string message, Exception exception)
    {
        Console.WriteLine($"Error: {message}");
        Console.WriteLine($"Error: {exception}");
    }

    public void Info(string message, string? updateTag = null)
    {
        Console.WriteLine($"Info: {message}");
    }

    public async Task OnAlreadyMember()
    {
        Console.WriteLine("This node is already a member");
    }

    public async Task<string?> OnAskCreatePassword(NodeType nodeType)
    {
        Console.WriteLine($"Asked to create a password for {nodeType} wallet");

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
        Console.WriteLine($"Asked for EULA");
        return true;
    }

    public async Task<bool> OnAskForMnemonicConfirmation(NodeType nodeType, string mnemonic)
    {
        Console.WriteLine($"Asked for mnemonic confirmation");
        Console.WriteLine($"Mnemonic: {mnemonic}");
        return true;
    }

    public async Task<bool> OnAskForNewFederationKey()
    {
        Console.WriteLine($"Asked for new federation key, deny");
        return false;
    }

    public async Task<string?> OnAskForPassphrase(NodeType nodeType)
    {
        Console.WriteLine($"Asked to create a passphrase for {nodeType} wallet");

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
        Console.WriteLine($"Asked a mnemonic for {nodeType} wallet");

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
        Console.WriteLine($"Asked a wallet name for {nodeType} wallet");

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
        Console.WriteLine($"Asked a password for {nodeType} wallet");

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
        Console.WriteLine($"Asked a wallet source for {nodeType} wallet");

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
        Console.WriteLine($"Asked to reenter password, deny");
        return false;
    }

    public async Task<bool> OnAskToRunIfAlreadyMember()
    {
        Console.WriteLine($"Already a member, stopping");
        return false;
    }

    public async Task<string?> OnChooseAddress(List<AddressItem> addresses, NodeType nodeType)
    {
        Console.WriteLine($"Choosing address {addresses.FirstOrDefault()?.Address}");
        return addresses.FirstOrDefault()?.Address;
    }

    public async Task<string?> OnChooseWallet(List<WalletItem> wallets, NodeType nodeType)
    {
        Console.WriteLine($"Choosing wallet {wallets.FirstOrDefault()?.Name}");
        return wallets.FirstOrDefault()?.Name;
    }

    public async Task OnCreateWalletFailed(NodeType nodeType)
    {
        Console.WriteLine($"{nodeType} wallet creation failed");
    }

    public async Task OnFederationKeyMissing()
    {
        Console.WriteLine($"Missing federation key");
    }

    public async Task OnMissingRegistrationFee(string address)
    {
        Console.WriteLine($"Missing registration fee on address: {address}");
    }

    public async Task OnMnemonicExists(NodeType nodeType)
    {
        Console.WriteLine($"{nodeType} wallet mnemonic already exists");
    }

    public async Task OnMnemonicIsInvalid(NodeType nodeType)
    {
        Console.WriteLine($"{nodeType} wallet mnemonic is invalid");
    }

    public async Task OnNodeFailedToStart(NodeType nodeType, string? reason = null)
    {
        Console.WriteLine($"{nodeType} node failed to start");
        Console.WriteLine($"Reason: {reason}");
    }

    public async Task OnProgramVersionAvailable(string? version)
    {
        Console.WriteLine($"App version: {version ?? "null"}");
    }

    public async Task OnRegistrationCanceled()
    {
        Console.WriteLine($"Registration canceled");
    }

    public async Task OnRegistrationComplete()
    {
        Console.WriteLine($"Registration complete");
    }

    public async Task OnRegistrationFailed()
    {
        Console.WriteLine($"Registration failed");
    }

    public async Task OnRestoreWalletFailed(NodeType nodeType)
    {
        Console.WriteLine($"{nodeType} wallet restore failed");
    }

    public async Task OnResyncFailed(NodeType nodeType)
    {
        Console.WriteLine($"{nodeType} wallet resync failed");
    }

    public async Task OnShowNewFederationKey(string pubKey, string savePath)
    {
        Console.WriteLine($"New pubKey is: {pubKey}");
        Console.WriteLine($"New savePath is: {savePath}");
    }

    public async Task OnShowWalletAddress(NodeType nodeType, string address)
    {
        Console.WriteLine($"{nodeType} wallet address is {address}");
    }

    public async Task OnShowWalletName(NodeType nodeType, string walletName)
    {
        Console.WriteLine($"{nodeType} wallet name is {walletName}");
    }

    public async Task OnStart()
    {
        Console.WriteLine($"Started");
    }

    public async Task OnWaitingForCollateral()
    {
        Console.WriteLine($"Waiting for collateral");
    }

    public async Task OnWaitingForRegistrationFee()
    {
        Console.WriteLine($"Waiting for registration fee");
    }

    public async Task OnWalletExistsOrInvalid(NodeType nodeType)
    {
        Console.WriteLine($"{nodeType} wallet exists or invalid");
    }

    public async Task OnWalletNameExists(NodeType nodeType)
    {
        Console.WriteLine($"{nodeType} wallet name already exists");
    }

    public async Task OnWalletSynced(NodeType nodeType)
    {
        Console.WriteLine($"{nodeType} wallet synced");
    }

    public async Task OnWalletSyncing(NodeType nodeType, int progress)
    {
        Console.WriteLine($"{nodeType} wallet is syncing, {progress}%");
    }
}
