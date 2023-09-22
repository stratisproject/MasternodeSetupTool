namespace MasternodeSetupTool;

public class WalletCreationState
{
    public string Name;
    public string Mnemonic;
    public string Passphrase;
    public string Password;

    public bool IsValid()
    {
        return !string.IsNullOrEmpty(Name) && !string.IsNullOrEmpty(Mnemonic) && !string.IsNullOrEmpty(Password);
    }
}