using System.Collections.Frozen;
using System.Reflection;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using MotelLease.Application.Common.Abstractions;
using MotelLease.Application.Common.Security;

namespace MotelLease.Infrastructure.Localization;

/// <summary>
/// Resolves message keys from JSON resources embedded in this assembly. Chosen over .resx
/// because the keys are dotted and hierarchical, which resx names cannot express, and because
/// a JSON file is reviewable in a diff.
/// </summary>
public sealed class JsonLocalizer : ILocalizer
{
    private readonly FrozenDictionary<string, FrozenDictionary<string, string>> _catalogues;
    private readonly ILogger<JsonLocalizer> _logger;

    public JsonLocalizer(ILogger<JsonLocalizer> logger)
    {
        _logger = logger;

        var assembly = typeof(JsonLocalizer).Assembly;

        _catalogues = SupportedLanguages.All
            .ToFrozenDictionary(
                language => language,
                language => Load(assembly, language),
                StringComparer.OrdinalIgnoreCase);
    }

    public string Get(string key, string language, params object[] arguments)
    {
        var catalogue = _catalogues.TryGetValue(language, out var requested)
            ? requested
            : _catalogues[SupportedLanguages.Default];

        if (!catalogue.TryGetValue(key, out var template)
            && !_catalogues[SupportedLanguages.Default].TryGetValue(key, out template))
        {
            // The key itself is returned rather than throwing: a missing translation must not
            // turn a working endpoint into a 500.
            _logger.LogWarning("Missing message key {Key} for language {Language}.", key, language);

            return key;
        }

        return arguments.Length == 0 ? template : string.Format(template, arguments);
    }

    private static FrozenDictionary<string, string> Load(Assembly assembly, string language)
    {
        var resourceName =
            $"{typeof(JsonLocalizer).Namespace}.Resources.messages.{language}.json";

        using var stream = assembly.GetManifestResourceStream(resourceName)
            ?? throw new InvalidOperationException(
                $"Embedded resource '{resourceName}' is missing. Every language in " +
                $"{nameof(SupportedLanguages)}.{nameof(SupportedLanguages.All)} needs one.");

        var entries = JsonSerializer.Deserialize<Dictionary<string, string>>(stream)
            ?? throw new InvalidOperationException($"Resource '{resourceName}' is not a JSON object.");

        return entries.ToFrozenDictionary(StringComparer.Ordinal);
    }
}
