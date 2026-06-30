using System.Text.Json;
using System.Text.Json.Serialization;

namespace Wolfgang.Conflict.Core.Catalog;

/// <summary>
/// Loads <see cref="FactionDefinition"/> values from JSON content. The loader
/// is content-only and never touches the file system — host applications
/// (Blazor, MAUI, MonoGame, etc.) supply the JSON string from wherever
/// they host their content (wwwroot, embedded resources, downloaded packs).
/// </summary>
public static class FactionLoader
{
    /// <summary>
    /// JSON serialization options used by <see cref="Load"/> and <see cref="Save"/>.
    /// Exposed so hosts can reuse the same options for related catalog files.
    /// </summary>
    public static JsonSerializerOptions JsonOptions { get; } = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DictionaryKeyPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = true,
        Converters = { new JsonStringEnumConverter(JsonNamingPolicy.CamelCase) }
    };


    /// <summary>
    /// Parse a JSON faction definition and validate it.
    /// </summary>
    /// <param name="json">UTF-8 text in the faction JSON schema.</param>
    /// <returns>The parsed and validated <see cref="FactionDefinition"/>.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="json"/> is <see langword="null"/>.</exception>
    /// <exception cref="InvalidOperationException">
    /// The JSON is malformed, missing required fields, contains duplicate
    /// IDs, or references a weapon ID not declared in the file.
    /// </exception>
    public static FactionDefinition Load(string json)
    {
        ArgumentNullException.ThrowIfNull(json);

        FactionFileDto? dto;

        try
        {
            dto = JsonSerializer.Deserialize<FactionFileDto>(json, JsonOptions);
        }
        catch (JsonException ex)
        {
            throw new InvalidOperationException
            (
                $"Faction JSON could not be parsed: {ex.Message}",
                ex
            );
        }

        if (dto is null)
        {
            throw new InvalidOperationException("Faction JSON parsed to null.");
        }

        return BuildAndValidate(dto);
    }


    /// <summary>
    /// Serialize a faction back to JSON. Useful for the future map editor
    /// and for round-trip tests.
    /// </summary>
    public static string Save(FactionDefinition faction)
    {
        ArgumentNullException.ThrowIfNull(faction);

        var dto = new FactionFileDto
        (
            faction.Id,
            faction.DisplayName,
            faction.Color,
            faction.WeaponSystems.Values.ToList(),
            faction.UnitTypes.Values.ToList()
        );

        return JsonSerializer.Serialize(dto, JsonOptions);
    }


    private static FactionDefinition BuildAndValidate(FactionFileDto dto)
    {
        RequireNonEmpty(dto.Id, nameof(dto.Id));
        RequireNonEmpty(dto.DisplayName, nameof(dto.DisplayName));
        RequireNonEmpty(dto.Color, nameof(dto.Color));

        var weapons = ToDictionary
        (
            dto.Weapons ?? [],
            w => w.Id,
            "weapon system"
        );

        var units = ToDictionary
        (
            dto.Units ?? [],
            u => u.Id,
            "unit type"
        );

        foreach (var unit in units.Values)
        {
            foreach (var weaponId in unit.WeaponSystemIds)
            {
                if (!weapons.ContainsKey(weaponId))
                {
                    throw new InvalidOperationException
                    (
                        $"Unit '{unit.Id}' references unknown weapon system '{weaponId}'."
                    );
                }
            }
        }

        return new FactionDefinition
        (
            dto.Id,
            dto.DisplayName,
            dto.Color,
            weapons,
            units
        );
    }


    private static void RequireNonEmpty(string? value, string fieldName)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new InvalidOperationException
            (
                $"Faction definition is missing required field '{fieldName}'."
            );
        }
    }


    private static Dictionary<string, T> ToDictionary<T>
    (
        IReadOnlyList<T> items,
        Func<T, string> keySelector,
        string itemKind
    )
    {
        var result = new Dictionary<string, T>(StringComparer.Ordinal);

        foreach (var item in items)
        {
            var key = keySelector(item);
            RequireNonEmpty(key, $"{itemKind} id");

            if (!result.TryAdd(key, item))
            {
                throw new InvalidOperationException
                (
                    $"Duplicate {itemKind} id '{key}' in faction definition."
                );
            }
        }

        return result;
    }


    /// <summary>JSON-shaped DTO for a faction file (list-based for authoring).</summary>
    private sealed record FactionFileDto
    (
        string Id,
        string DisplayName,
        string Color,
        IReadOnlyList<WeaponSystemDefinition>? Weapons,
        IReadOnlyList<UnitTypeDefinition>? Units
    );
}
