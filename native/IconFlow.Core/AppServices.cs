namespace IconFlow.Core;

public sealed class AppServices
{
    public DataStore Store { get; }
    public ImageService Images { get; }
    public WindowsIconService Windows { get; }
    public IntegrationService Integration { get; }

    public AppServices(string? rootOverride = null, bool reconcileIntegrations = true)
    {
        Store = new DataStore(rootOverride);
        Images = new ImageService(Store);
        Windows = new WindowsIconService(Store);
        Integration = new IntegrationService(Store);
        if (reconcileIntegrations) Integration.ReconcileSafeDefaults();
    }
}
