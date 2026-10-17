using System.Collections;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;

namespace MatteiGrid.Shared.Infrastructure.Logging;

/// <summary>
/// Healthcare-compliant sanitizer that redacts credentials, secrets, tokens, 
/// and sensitive medical/personal identifiers from method parameters and log traces.
/// </summary>
public static class LogSanitizer
{
    private static readonly HashSet<string> SensitiveKeywords = new(StringComparer.OrdinalIgnoreCase)
    {
        "password", "pwd", "secret", "token", "accesstoken", "refreshtoken",
        "authorization", "apikey", "key", "signingkey", "creditcard", "cardnumber",
        "cvv", "pin", "ssn", "aadhar", "pan", "securitycode", "hash"
    };

    private static readonly Regex PhoneRegex = new(@"\b(?:\+?\d{1,3}[-.\s]?)?\(?\d{3}\)?[-.\s]?\d{3}[-.\s]?(\d{4})\b", RegexOptions.Compiled);

    /// <summary>
    /// Formats method arguments into a sanitized string representation.
    /// </summary>
    public static string SanitizeArguments(MethodInfo method, object?[]? arguments)
    {
        if (arguments is null || arguments.Length == 0)
            return string.Empty;

        var parameters = method.GetParameters();
        var sb = new StringBuilder();

        for (var i = 0; i < arguments.Length; i++)
        {
            if (i > 0) sb.Append(", ");

            var paramName = i < parameters.Length ? parameters[i].Name ?? $"arg{i}" : $"arg{i}";
            var paramValue = arguments[i];

            sb.Append(paramName).Append('=');
            sb.Append(SanitizeValue(paramName, paramValue));
        }

        return sb.ToString();
    }

    /// <summary>
    /// Sanitizes an individual argument or property value based on name and content.
    /// </summary>
    public static string SanitizeValue(string name, object? value)
    {
        if (value is null)
            return "null";

        // Redact if parameter name matches sensitive keywords
        if (SensitiveKeywords.Contains(name) || SensitiveKeywords.Any(k => name.Contains(k, StringComparison.OrdinalIgnoreCase)))
            return "\"***\"";

        // Ignore CancellationToken and HttpContext instances in traces
        if (value is CancellationToken)
            return "[CancellationToken]";

        if (value is string strValue)
        {
            if (string.IsNullOrWhiteSpace(strValue))
                return "\"\"";

            // Mask phone numbers (keep only last 4 digits: e.g. ***4321)
            var sanitizedStr = PhoneRegex.Replace(strValue, "***$1");

            if (sanitizedStr.Length > 200)
                sanitizedStr = string.Concat(sanitizedStr.AsSpan(0, 197), "...");

            return $"\"{sanitizedStr}\"";
        }

        if (value is DateTime dt)
            return $"\"{dt:yyyy-MM-dd HH:mm:ss}\"";

        if (value is DateTimeOffset dto)
            return $"\"{dto:yyyy-MM-dd HH:mm:ss zzz}\"";

        if (value is DateOnly dateOnly)
            return $"\"{dateOnly:yyyy-MM-dd}\"";

        if (value is Guid guid)
            return $"\"{guid}\"";

        if (value.GetType().IsPrimitive || value is decimal)
            return value.ToString() ?? string.Empty;

        // Collection summary
        if (value is IEnumerable enumerable and not string)
        {
            int count = 0;
            foreach (var _ in enumerable) count++;
            return $"[{value.GetType().Name} Count={count}]";
        }

        // Complex DTOs: summarize type name
        return $"[{value.GetType().Name}]";
    }
}
