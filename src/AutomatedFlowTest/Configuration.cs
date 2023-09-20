using MasternodeSetupTool;
using NBitcoin;

public class Configuration
{
    public FlowType flowType = FlowType.SetupNode;

    public NetworkType networkType = NetworkType.Testnet;

    public WalletSource collateralWalletSource = WalletSource.UseExistingWallet;
    public WalletSource miningWalletSource = WalletSource.UseExistingWallet;

    public string collateralWalletName = "TestWallet";
    public string miningWalletName = "TestWallet";

    public string collateralWalletPassword = "12345";
    public string miningWalletPassword = "12345";

    public string collateralWalletPassphrase = "";
    public string miningWalletPassphrase = "";

    public string collateralWalletMnemonic = "";
    public string miningWalletMnemonic = "";
}

public enum FlowType
{
    RunNode, SetupNode
}
