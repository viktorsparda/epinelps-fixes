using EpinelPS.Data;
using EpinelPS.Database;
using EpinelPS.Utils;

namespace EpinelPS.LobbyServer.Character;

/// <summary>
/// NIKKE stores the limit-break / core-enhancement stage twice:
/// <see cref="CharacterModel.Tid"/> points at a per-stage CharacterTable row
/// (whose GradeCoreId encodes the stage) and <see cref="CharacterModel.Grade"/>
/// stores the 0-based stage index. The two must always agree:
/// <c>Grade = GradeCoreId - base</c>, where base is 1 (SSR), 101 (SR) or 201 (R).
///
/// Older code (the admin set-core-level command and the upgrade handlers) wrote
/// raw GradeCoreId values into Grade, and both upgrade handlers added
/// <c>req.Count</c> to Grade instead of deriving it from the new Tid. That left
/// the client and server disagreeing about the current star/core stage: the
/// client could show a limit-break screen for a character the server already
/// considered maxed, then request several "limit breaks" and jump straight into
/// core levels. Keep every upgrade path using these helpers.
/// </summary>
internal static class CharacterGradeHelper
{
    public static int BaseGradeCoreId(OriginalRareType rare) => rare switch
    {
        OriginalRareType.SSR => 1,
        OriginalRareType.SR => 101,
        OriginalRareType.R => 201,
        _ => 1,
    };

    public static int MaxGradeCoreId(OriginalRareType rare) => rare switch
    {
        OriginalRareType.SSR => 11,
        OriginalRareType.SR => 103,
        OriginalRareType.R => 201,
        _ => 11,
    };

    /// <summary>
    /// Last GradeCoreId reachable through limit break, before core enhancement.
    /// SSR: GradeCoreId 1-4 = 0-3 stars. SR: 101-103 = 0-2 stars (no core).
    /// R: 201 = base only.
    /// </summary>
    public static int MaxLimitBreakGradeCoreId(OriginalRareType rare) => rare switch
    {
        OriginalRareType.SSR => 4,
        OriginalRareType.SR => 103,
        OriginalRareType.R => 201,
        _ => 4,
    };

    public static int MaxGradeIndex(OriginalRareType rare) =>
        MaxGradeCoreId(rare) - BaseGradeCoreId(rare);

    public static int GradeIndex(int gradeCoreId, OriginalRareType rare) =>
        Math.Clamp(gradeCoreId - BaseGradeCoreId(rare), 0, MaxGradeIndex(rare));

    public static int GradeIndex(CharacterRecord record) =>
        GradeIndex(record.GradeCoreId, record.OriginalRare);

    public static int GradeIndex(CharacterModel character)
    {
        if (GameData.Instance != null &&
            GameData.Instance.CharacterTable.TryGetValue(character.Tid, out CharacterRecord? record))
        {
            return GradeIndex(record);
        }

        return Math.Max(character.Grade, 0);
    }

    /// <summary>
    /// Whether a duplicate should become a spare-body item instead of body
    /// labels. <paramref name="existingSpareBodies"/> is the current stack size,
    /// so a stack that could already reach the cap does not grow any further.
    /// </summary>
    public static bool CanReceiveSpareBody(CharacterModel owned, CharacterRecord pulled, int existingSpareBodies)
    {
        if (pulled.OriginalRare == OriginalRareType.R)
            return false;

        return GradeIndex(owned) + existingSpareBodies < MaxGradeIndex(pulled.OriginalRare);
    }

    /// <summary>
    /// Repairs saves written by older handlers that stored the raw GradeCoreId
    /// in <see cref="CharacterModel.Grade"/>. Safe to run on every startup.
    /// </summary>
    public static int NormalizeSavedGrades()
    {
        if (GameData.Instance == null)
            return 0;

        int changed = 0;
        foreach (User user in JsonDb.Instance.Users)
        {
            foreach (CharacterModel character in user.Characters)
            {
                if (!GameData.Instance.CharacterTable.TryGetValue(character.Tid, out CharacterRecord? record))
                    continue;

                int expected = GradeIndex(record);
                if (character.Grade == expected)
                    continue;

                Logging.WriteLine(
                    $"[GradeCheck] user={user.ID} csn={character.Csn} tid={character.Tid}: " +
                    $"grade {character.Grade} -> {expected}",
                    LogType.Warning);
                character.Grade = expected;
                changed++;
            }
        }

        if (changed > 0)
            Logging.WriteLine($"[GradeCheck] normalized {changed} character grade value(s)", LogType.Warning);

        return changed;
    }
}
