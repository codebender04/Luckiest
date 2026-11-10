using DG.Tweening;
using TMPro;
using UnityEngine;

[RequireComponent(typeof(TMP_Text))]
public class ScoreCounter : MonoBehaviour
{
    [SerializeField] private SfxPlayer sfx;
    [Tooltip("Minimum seconds between tick sounds while the number rolls.")]
    [SerializeField] private float tickInterval = 0.03f;

    private TMP_Text text;
    private Vector2 home;
    private Color baseColor;
    private float value;
    private int lastStep;
    private float lastTickTime;
    private Tween tween;

    public RectTransform Rect { get; private set; }

    private void Awake()
    {
        text = GetComponent<TMP_Text>();
        Rect = text.rectTransform;
        home = Rect.anchoredPosition;
        baseColor = text.color;
    }

    private void OnDestroy() => tween?.Kill();

    public void Set(float target)
    {
        tween?.Kill();
        lastStep = Mathf.FloorToInt(target);
        Apply(target);
    }

    public void RollTo(float target, float duration, Ease ease = Ease.OutCubic)
    {
        tween?.Kill();
        tween = DOTween.To(() => value, Apply, target, duration).SetEase(ease);
    }

    public void PlayBust(Color color)
    {
        tween?.Kill();
        Rect.DOKill();
        ResetVisuals();
        text.color = color;

        tween = DOTween.Sequence()
            .Append(Rect.DOShakeAnchorPos(0.35f, 10f, 40, 90f, false, true))
            .Append(DOTween.To(() => value, Apply, 0f, 0.5f).SetEase(Ease.InCubic))
            .Join(Rect.DOScale(0.8f, 0.5f).SetEase(Ease.InCubic))
            .Append(Rect.DOScale(1f, 0.3f).SetEase(Ease.OutBack))
            .Join(text.DOColor(baseColor, 0.3f))
            .OnKill(ResetVisuals);
    }

    private void Apply(float v)
    {
        value = v;
        text.text = GameManager.FormatScore(v);

        int step = Mathf.FloorToInt(v);
        if (step == lastStep)
            return;

        bool rising = step > lastStep;
        lastStep = step;

        if (sfx == null || Time.unscaledTime - lastTickTime < tickInterval)
            return;
        lastTickTime = Time.unscaledTime;
        sfx.Tick(rising ? 1.2f : 0.7f);
    }

    private void ResetVisuals()
    {
        Rect.anchoredPosition = home;
        Rect.localScale = Vector3.one;
        text.color = baseColor;
    }
}
