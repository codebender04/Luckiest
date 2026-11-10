using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(CanvasGroup))]
public class HistoryRow : MonoBehaviour
{
    [SerializeField] private TMP_Text indexText;
    [SerializeField] private TMP_Text amountText;
    [SerializeField] private TMP_Text oddsText;
    [Tooltip("Optional background, tinted gold on jackpots and red on busts.")]
    [SerializeField] private Image highlight;
    [Tooltip("Line across the row, shown when a bust wipes this win. Set its pivot X to 0 so it draws in from the left.")]
    [SerializeField] private RectTransform strike;

    [SerializeField] private float emptyAlpha = 0.25f;
    [SerializeField] private float highlightAlpha = 0.25f;

    private CanvasGroup group;
    private Palette palette;

    public bool IsWiped { get; private set; }

    private void Awake() => group = GetComponent<CanvasGroup>();

    public void Init(int index, Palette palette)
    {
        this.palette = palette;
        indexText.text = $"#{index + 1}";
    }

    public void ShowEmpty(float delay)
    {
        KillTweens();
        IsWiped = false;
        SetText("--", "--", palette.Ink, palette.Ink, palette.Ink);
        SetHighlight(null);
        strike.gameObject.SetActive(false);

        group.alpha = 0f;
        group.DOFade(emptyAlpha, 0.25f).SetDelay(delay);
    }

    public void Pulse()
    {
        group.DOKill();
        group.alpha = emptyAlpha;
        group.DOFade(0.6f, 0.5f).SetLoops(-1, LoopType.Yoyo).SetEase(Ease.InOutSine);
    }

    public void ShowAttempt(Attempt attempt)
    {
        KillTweens();
        group.alpha = 1f;

        bool jackpot = attempt.Won && palette.IsJackpot(attempt.Probability);
        string odds = $"{attempt.Probability}%";

        if (attempt.Won)
        {
            SetText($"+{GameManager.FormatScore(attempt.Delta)}", odds,
                palette.Green, palette.Green, palette.ForOdds(attempt.Probability));
            SetHighlight(jackpot ? palette.Gold : null);
        }
        else
        {
            string amount = attempt.Delta < 0f ? GameManager.FormatScore(attempt.Delta) : "MISS";
            SetText(amount, odds, palette.Red, palette.Red, palette.Red);
            SetHighlight(palette.Red);
        }

        transform.localScale = new Vector3(0f, 1f, 1f);
        transform.DOScaleX(1f, 0.3f).SetEase(Ease.OutBack, 2f);
        if (jackpot)
            transform.DOPunchScale(Vector3.one * 0.3f, 0.45f, 8).SetDelay(0.3f);
    }

    public void Wipe(float delay)
    {
        IsWiped = true;
        SetText(amountText.text, oddsText.text, palette.Wiped, palette.Wiped, palette.Wiped);
        SetHighlight(null);

        strike.gameObject.SetActive(true);
        strike.localScale = new Vector3(0f, 1f, 1f);
        strike.DOScaleX(1f, 0.2f).SetDelay(delay).SetEase(Ease.OutCubic);
        transform.DOPunchRotation(new Vector3(0f, 0f, 8f), 0.35f, 10).SetDelay(delay);
    }

    private void SetText(string amount, string odds, Color indexColor, Color amountColor, Color oddsColor)
    {
        amountText.text = amount;
        oddsText.text = odds;
        indexText.color = indexColor;
        amountText.color = amountColor;
        oddsText.color = oddsColor;
    }

    private void SetHighlight(Color? color)
    {
        if (highlight == null)
            return;
        highlight.enabled = color.HasValue;
        if (color.HasValue)
            highlight.color = Palette.WithAlpha(color.Value, highlightAlpha);
    }

    private void KillTweens()
    {
        group.DOKill();
        transform.DOKill();
        strike.DOKill();
        transform.localScale = Vector3.one;
        transform.localRotation = Quaternion.identity;
    }
}
