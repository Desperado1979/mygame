using UnityEngine;

/// <summary>Week2: canonical item ids to avoid typo divergence.</summary>
public static class GameItemIdsSimple
{
    public const string HpPotion = "potion";
    public const string MpPotion = "mana";
    public const string GenericDrop = "drop";
    public const string Shard = "shard";

    public static string Normalize(string raw)
    {
        if (string.IsNullOrWhiteSpace(raw))
            return GenericDrop;
        string v = raw.Trim().ToLowerInvariant();
        if (v == "hp_potion" || v == "hppotion")
            return HpPotion;
        if (v == "mp_potion" || v == "mppotion")
            return MpPotion;
        return v;
    }
}
