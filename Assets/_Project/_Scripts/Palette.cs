using UnityEngine;

public enum OddsTier { Safe, Risky, LongShot }

[CreateAssetMenu(fileName = "Palette", menuName = "Luckiest/Palette")]
public class Palette : ScriptableObject
{
    [field: Header("Colors")]
    [field: SerializeField] public Color Ink { get; private set; } = new Color32(43, 38, 51, 255);
    [field: SerializeField] public Color Green { get; private set; } = new Color32(62, 163, 77, 255);
    [field: SerializeField] public Color Gold { get; private set; } = new Color32(242, 193, 78, 255);
    [field: SerializeField] public Color Red { get; private set; } = new Color32(209, 73, 91, 255);
    [field: SerializeField] public Color Cream { get; private set; } = new Color32(250, 243, 224, 255);
    [field: Tooltip("Wins that a later bust wiped out.")]
    [field: SerializeField] public Color Wiped { get; private set; } = new Color32(150, 144, 156, 255);

    [field: Header("Odds tiers")]
    [field: Tooltip("At or below this chance a pick is a long shot, and a win is a jackpot.")]
    [field: SerializeField] public int JackpotThreshold { get; private set; } = 10;
    [field: Tooltip("Below this chance a pick is risky.")]
    [field: SerializeField] public int RiskyThreshold { get; private set; } = 50;

    public OddsTier TierFor(int probability) =>
        probability <= JackpotThreshold ? OddsTier.LongShot
        : probability < RiskyThreshold ? OddsTier.Risky
        : OddsTier.Safe;

    public bool IsJackpot(int probability) => TierFor(probability) == OddsTier.LongShot;

    public Color ForTier(OddsTier tier) => tier switch
    {
        OddsTier.LongShot => Red,
        OddsTier.Risky => Gold,
        _ => Green
    };

    public Color ForOdds(int probability) => ForTier(TierFor(probability));

    public static Color WithAlpha(Color color, float alpha)
    {
        color.a = alpha;
        return color;
    }
}
