using System;
using System.Collections.Generic;
using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

[Serializable]
public class RunStripPrefabs
{
    public RectTransform win;
    public RectTransform jackpot;
    public RectTransform bust;
    [Tooltip("A win whose points a later bust wiped out.")]
    public RectTransform wiped;

    public RectTransform For(Attempt attempt, bool isWiped, Palette palette) =>
        isWiped ? wiped
        : !attempt.Won ? bust
        : palette.IsJackpot(attempt.Probability) ? jackpot
        : win;
}

public class EndPanel : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private GameManager gameManager;
    [SerializeField] private Juice juice;
    [SerializeField] private SfxPlayer sfx;
    [SerializeField] private Palette palette;

    [Header("Panel")]
    [SerializeField] private CanvasGroup panel;
    [SerializeField] private RectTransform box;
    [Tooltip("Dims the game behind the panel. Its alpha in the scene is the faded-in alpha.")]
    [SerializeField] private Image overlay;
    [SerializeField] private Button playAgainButton;

    [Header("Content")]
    [SerializeField] private TMP_Text titleText;
    [SerializeField] private ScoreCounter finalScore;
    [SerializeField] private TMP_Text bestText;
    [SerializeField] private StatRow bestStreakRow;
    [SerializeField] private StatRow riskiestWinRow;
    [SerializeField] private StatRow lostToBustsRow;

    [Header("Run strip")]
    [Tooltip("Parent with a HorizontalLayoutGroup.")]
    [SerializeField] private RectTransform strip;
    [SerializeField] private RunStripPrefabs cellPrefabs;

    [Header("Timing")]
    [SerializeField] private float showDelay = 0.7f;
    [Tooltip("Time between strip cells starting to pop in. Lower = faster.")]
    [SerializeField] private float cellStagger = 0.05f;
    [SerializeField] private float cellPopDuration = 0.2f;
    [SerializeField] private float statStagger = 0.1f;

    private readonly List<RectTransform> cells = new();
    private StatRow[] statRows;
    private Vector2 boxHome;
    private float overlayAlpha;
    private Sequence sequence;

    private void Awake()
    {
        statRows = new[] { bestStreakRow, riskiestWinRow, lostToBustsRow };
        boxHome = box.anchoredPosition;
        overlayAlpha = overlay.color.a;
        overlay.gameObject.SetActive(true);
        overlay.color = Palette.WithAlpha(overlay.color, 0f);
        panel.alpha = 0f;
        SetInteractable(false);

        playAgainButton.onClick.AddListener(() =>
        {
            sfx.Click();
            gameManager.StartNewRun();
        });
    }

    private void OnEnable()
    {
        gameManager.OnRunStarted += Hide;
        gameManager.OnRunEnded += Show;
    }

    private void OnDisable()
    {
        gameManager.OnRunStarted -= Hide;
        gameManager.OnRunEnded -= Show;
    }

    private void OnDestroy() => sequence?.Kill();

    private void Hide()
    {
        sequence?.Kill();
        SetInteractable(false);
        panel.DOKill();
        overlay.DOKill();
        panel.DOFade(0f, panel.alpha > 0f ? 0.2f : 0f);
        overlay.DOFade(0f, overlay.color.a > 0f ? 0.3f : 0f);
    }

    private void Show(float score, bool isNewBest)
    {
        sequence?.Kill();
        panel.DOKill();
        overlay.DOKill();

        IReadOnlyList<Attempt> attempts = gameManager.Attempts;
        var stats = new RunStats(attempts);

        titleText.text = stats.Title(score, palette.JackpotThreshold);
        finalScore.Set(0f);
        bestText.DOKill();
        bestText.gameObject.SetActive(false);
        BuildStrip(attempts, stats);
        FillStats(stats);

        panel.alpha = 0f;
        box.anchoredPosition = boxHome + Vector2.up * 160f;
        box.localScale = Vector3.one * 0.8f;
        float rollDuration = Mathf.Clamp(score * 0.01f, 0.4f, 1.2f);

        sequence = DOTween.Sequence()
            .AppendInterval(showDelay)
            .AppendCallback(() =>
            {
                sfx.RunEnd();
                SetInteractable(true);
            })
            .Append(panel.DOFade(1f, 0.25f))
            .Join(overlay.DOFade(overlayAlpha, 0.4f))
            .Join(box.DOAnchorPos(boxHome, 0.55f).SetEase(Ease.OutElastic, 1.4f))
            .Join(box.DOScale(1f, 0.45f).SetEase(Ease.OutBack))
            .AppendCallback(() => finalScore.RollTo(score, rollDuration, Ease.OutQuad))
            .AppendInterval(rollDuration)
            .AppendCallback(() => CelebrateScore(score))
            .AppendInterval(0.2f);

        AddStripReveal(attempts);
        AddStatsReveal();

        sequence.AppendInterval(0.1f)
            .AppendCallback(() => ShowBest(score, gameManager.PersonalBest, isNewBest));
    }

    private void BuildStrip(IReadOnlyList<Attempt> attempts, RunStats stats)
    {
        foreach (RectTransform cell in cells)
        {
            cell.DOKill();
            cell.gameObject.SetActive(false);
            Destroy(cell.gameObject);
        }
        cells.Clear();

        for (int i = 0; i < attempts.Count; i++)
        {
            RectTransform cell = Instantiate(cellPrefabs.For(attempts[i], stats.Wiped[i], palette), strip);
            cell.localScale = Vector3.zero;
            cells.Add(cell);
        }
    }

    private void FillStats(RunStats stats)
    {
        bestStreakRow.Set(stats.BestStreak > 0 ? $"x{stats.BestStreak}" : "NONE", palette.Ink);

        riskiestWinRow.Set(
            stats.RiskiestWin > 0 ? $"{stats.RiskiestWin}%" : "NONE",
            stats.RiskiestWin > 0 ? palette.ForOdds(stats.RiskiestWin) : palette.Ink);

        lostToBustsRow.Set(
            stats.LostToBusts > 0f ? GameManager.FormatScore(stats.LostToBusts) : "NOTHING",
            stats.LostToBusts > 0f ? palette.Red : palette.Green);

        foreach (StatRow row in statRows)
            row.Hide();
    }

    private void AddStripReveal(IReadOnlyList<Attempt> attempts)
    {
        float start = sequence.Duration();
        for (int i = 0; i < cells.Count; i++)
        {
            RectTransform cell = cells[i];
            bool won = attempts[i].Won;
            float pitch = won ? 1f + i * 0.06f : 0.6f;
            float t = start + i * cellStagger;

            sequence.InsertCallback(t, () => sfx.Tick(pitch));
            sequence.Insert(t, cell.DOScale(1f, cellPopDuration).SetEase(Ease.OutBack, 2.5f));
            if (!won)
                sequence.Insert(t, cell.DOPunchRotation(new Vector3(0f, 0f, 20f), 0.3f, 12));
        }
    }

    private void AddStatsReveal()
    {
        float start = sequence.Duration() + 0.1f;
        for (int i = 0; i < statRows.Length; i++)
        {
            float t = start + i * statStagger;
            sequence.InsertCallback(t, () => sfx.Tick(1.4f));
            sequence.Insert(t, statRows[i].Reveal());
        }
    }

    private void CelebrateScore(float score)
    {
        if (score <= 0f)
            return;
        juice.Burst(finalScore.Rect, 30, new[] { palette.Gold, palette.Cream, palette.Green }, 300f);
        juice.Pop(finalScore.transform, 0.5f, 0.45f);
        juice.PostKick(bloomBoost: 2.5f, chromaticAberration: 0.3f, duration: 0.5f);
    }

    private void ShowBest(float score, float personalBest, bool isNewBest)
    {
        bestText.gameObject.SetActive(true);
        bestText.transform.localScale = Vector3.zero;
        bestText.transform.DOScale(1f, 0.6f).SetEase(Ease.OutElastic);

        if (isNewBest)
        {
            sfx.NewBest();
            bestText.text = "NEW BEST!";
            bestText.color = palette.Ink;
            bestText.DOFade(0.35f, 0.45f).SetDelay(0.6f).SetLoops(-1, LoopType.Yoyo).SetEase(Ease.InOutSine);
            juice.Flash(palette.Gold, 0.35f, 0.5f);
            juice.Burst(bestText.rectTransform, 20, new[] { palette.Gold, palette.Cream }, 240f);
        }
        else
        {
            bestText.text = $"{GameManager.FormatScore(personalBest - score)} SHORT OF BEST";
            bestText.color = Palette.WithAlpha(palette.Ink, 0.55f);
        }
    }

    private void SetInteractable(bool value)
    {
        panel.interactable = value;
        panel.blocksRaycasts = value;
        overlay.raycastTarget = value;
    }
}
