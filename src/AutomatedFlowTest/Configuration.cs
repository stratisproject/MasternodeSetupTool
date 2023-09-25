using MasternodeSetupTool;
using NBitcoin;

public class Configuration
{
    public FlowType flowType = FlowType.SetupNode;

    public bool writeConsoleLog = true;
    public string? logFilePath = null;

    public NetworkType networkType = NetworkType.Testnet;

    public WalletSource collateralWalletSource = WalletSource.UseExistingWallet;
    public WalletSource miningWalletSource = WalletSource.UseExistingWallet;

    public bool confirmEULA = true;
    public bool confirmNewFederationKey = true;
    public bool confirmRunIfAlreadyMember = true;
    public bool confirmMnemonic = true;
    public bool confirmReenterPassword = true;

    public string? collateralWalletName = "TestWallet";
    public string? miningWalletName = "TestWallet";
    public string collateralWalletPassword = "12345";
    public string miningWalletPassword = "12345";
    public string collateralWalletPassphrase = "";
    public string miningWalletPassphrase = "";
    public string collateralWalletMnemonic = "";
    public string miningWalletMnemonic = "";
    public string? collateralWalletAddress = null;
    public string? miningWalletAddress = null;
}


public enum FlowType
{
    RunNode, SetupNode
}
