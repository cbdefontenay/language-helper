namespace LanguageHelper.Services;

public class CultureService
{
    private const string CultureKey = "selectedCulture";

    public async Task SetCultureAsync(string culture)
    {
        await SecureStorage.SetAsync(CultureKey, culture);

        CultureInfo.DefaultThreadCurrentCulture = new CultureInfo(culture);
        CultureInfo.DefaultThreadCurrentUICulture = new CultureInfo(culture);
    }

    public async Task<string> GetStoredCultureAsync()
    {
        var culture = await SecureStorage.GetAsync(CultureKey);
        return culture ?? "fr";
    }

    public void ApplySavedCulture()
    {
        var culture = SecureStorage.GetAsync(CultureKey).Result ?? "fr";
        CultureInfo.DefaultThreadCurrentCulture = new CultureInfo(culture);
        CultureInfo.DefaultThreadCurrentUICulture = new CultureInfo(culture);
    }
}