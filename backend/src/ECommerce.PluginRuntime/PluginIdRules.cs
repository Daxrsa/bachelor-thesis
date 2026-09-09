using System.Text.RegularExpressions;

namespace ECommerce.PluginRuntime;

public static partial class PluginIdRules
{
    public static bool IsValid(string? id) =>
        !string.IsNullOrWhiteSpace(id) && PluginIdRegex().IsMatch(id);

    [GeneratedRegex(@"^[a-z0-9]+(?:-[a-z0-9]+)*-plugin$", RegexOptions.CultureInvariant)]
    private static partial Regex PluginIdRegex();
}
