using System.Globalization;
using System.Xml.Linq;

namespace IconFlow;

public sealed record LanguageOption(string Code, string Name);

public static class Loc
{
    private static string? _activeLanguage;
    private static Lazy<IReadOnlyDictionary<string, string>> _strings = new(LoadStrings);

    public static IReadOnlyList<LanguageOption> Languages { get; } =
    [
        new("system", "System / 系统"),
        new("zh-CN", "简体中文"),
        new("zh-TW", "繁體中文"),
        new("en-US", "English"),
        new("es-ES", "Español"),
        new("fr-FR", "Français"),
        new("de-DE", "Deutsch"),
        new("pt-BR", "Português"),
        new("ja-JP", "日本語"),
        new("ko-KR", "한국어"),
        new("ru-RU", "Русский"),
        new("ar-SA", "العربية")
    ];

    public static string Get(string key, string fallback = "")
    {
        try
        {
            return _strings.Value.TryGetValue(key, out var value) && !string.IsNullOrWhiteSpace(value)
                ? value : (string.IsNullOrWhiteSpace(fallback) ? key : fallback);
        }
        catch { return string.IsNullOrWhiteSpace(fallback) ? key : fallback; }
    }

    public static string Format(string key, string fallback, params object[] values)
        => string.Format(CultureInfo.CurrentCulture, Get(key, fallback), values);

    public static void ApplyLanguage(string? language)
    {
        _activeLanguage = string.IsNullOrWhiteSpace(language) || language.Equals("system", StringComparison.OrdinalIgnoreCase)
            ? null : language;
        _strings = new(LoadStrings);
        try
        {
            if (_activeLanguage is not null)
            {
                var culture = CultureInfo.GetCultureInfo(_activeLanguage);
                CultureInfo.CurrentUICulture = culture;
                CultureInfo.DefaultThreadCurrentUICulture = culture;
            }
        }
        catch { /* Keep the system language if Windows rejects an optional override. */ }
    }

    private static IReadOnlyDictionary<string, string> LoadStrings()
    {
        var requested = _activeLanguage ?? CultureInfo.CurrentUICulture.Name;
        var locale = Languages.Select(x => x.Code).Where(x => x != "system")
            .FirstOrDefault(x => x.Equals(requested, StringComparison.OrdinalIgnoreCase));
        locale ??= requested.StartsWith("zh", StringComparison.OrdinalIgnoreCase)
            ? (requested.Contains("TW", StringComparison.OrdinalIgnoreCase) || requested.Contains("Hant", StringComparison.OrdinalIgnoreCase) ? "zh-TW" : "zh-CN")
            : Languages.Select(x => x.Code).FirstOrDefault(x => x != "system" && x.StartsWith(requested.Split('-')[0] + "-", StringComparison.OrdinalIgnoreCase));
        locale ??= "en-US";
        var path = Path.Combine(AppContext.BaseDirectory, "Strings", locale, "Resources.resw");
        if (!File.Exists(path)) return new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        return XDocument.Load(path).Root?.Elements("data")
            .Where(x => x.Attribute("name") is not null)
            .ToDictionary(x => x.Attribute("name")!.Value, x => x.Element("value")?.Value ?? string.Empty, StringComparer.OrdinalIgnoreCase)
            ?? new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
    }

    public static bool IsRightToLeft
    {
        get
        {
            var language = _activeLanguage;
            if (string.IsNullOrWhiteSpace(language)) language = CultureInfo.CurrentUICulture.Name;
            return language.StartsWith("ar", StringComparison.OrdinalIgnoreCase);
        }
    }

    public static string BuiltInIconName(string key, string fallback)
        => Get("BuiltIn_" + key, fallback);
}
