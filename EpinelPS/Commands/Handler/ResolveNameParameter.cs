using EpinelPS.Commands.Binding;
using EpinelPS.Commands.Core;
using EpinelPS.Data;

namespace EpinelPS.Commands.Handler;

public class ResolveNameParameter : ICommandParameters
{
    static ParameterDescriptor[] ICommandParameters.Descriptors => [
        Param.String(0, "query", "Numeric ID or (part of) a name"),
    ];

    public string Query { get; init; } = "";
}

/// <summary>
/// Name/ID lookup for the admin CLI and admin API. Uses the installed
/// local-locale tables (LocaleNameResolver) so keys like Locale_Item:...
/// resolve to English names. Numeric queries match IDs; text queries match
/// resolved names and name keys, case-insensitively.
/// </summary>
public class ResolveNameHandler(IExecutionContext context) : BaseHandler<ResolveNameParameter>(context)
{
    public override string Name => "resolve-name";
    public override string Description => "Resolve a NIKKE ID or name (characters, items, costumes, equipment, cubes)";

    protected override Task<HandleResult> ExecuteAsync(ResolveNameParameter parameters)
    {
        string query = (parameters.Query ?? "").Trim();
        if (query.Length == 0)
            return Task.FromResult(new HandleResult(false, "usage: resolve-name <id|name>"));

        bool numeric = int.TryParse(query, out int id);
        string needle = query.ToLowerInvariant();
        var hits = new List<string>();

        void Add(string kind, int rowId, string? localKey)
        {
            string name = LocaleNameResolver.Resolve(localKey, "en");
            bool idMatch = numeric && rowId == id;
            bool nameMatch = !numeric &&
                ((!string.IsNullOrEmpty(name) && name.Contains(needle, StringComparison.OrdinalIgnoreCase)) ||
                 (!string.IsNullOrEmpty(localKey) && localKey.Contains(needle, StringComparison.OrdinalIgnoreCase)));
            if ((idMatch || nameMatch) && hits.Count < 50)
                hits.Add($"{kind} {rowId} = {name} ({localKey})");
        }

        foreach (var r in GameData.Instance.CharacterTable.Values)
            Add("character", r.Id, r.NameLocalkey);
        foreach (var r in GameData.Instance.itemMaterialTable.Values)
            Add("material", r.Id, r.NameLocalkey);
        foreach (var r in GameData.Instance.ItemEquipTable.Values)
            Add("equipment", r.Id, r.NameLocalkey);
        foreach (var r in GameData.Instance.CharacterCostumeTable.Values)
            Add("costume", r.Id, r.CostumeNameLocale);
        foreach (var r in GameData.Instance.ItemHarmonyCubeTable.Values)
            Add("cube", r.Id, r.NameLocalkey);

        string msg = hits.Count == 0 ? $"no name/ID match for '{query}'"
                                     : string.Join("\n", hits);
        Console.WriteLine(msg);
        return Task.FromResult(new HandleResult(true, msg));
    }
}
