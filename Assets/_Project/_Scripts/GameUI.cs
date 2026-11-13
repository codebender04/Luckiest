using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

public class GameUI : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private GameManager gameManager;
    [SerializeField] private Juice juice;
    [SerializeField] private SfxPlayer sfx;
    [SerializeField] private Palette palette;

    [Header("Score")]
    [SerializeField] private ScoreCounter score;
    [SerializeField] private TMP_Text streakText;
    [SerializeField] private TMP_Text resultText;
    [SerializeField] private TMP_Text personalBestText;

    [Header("Odds picker")]
    [SerializeField] private Slider probabilitySlider;
    [SerializeField] private Image sliderFill;
    [SerializeField] private TMP_Text probabilityText;
    [SerializeField] private TMP_Text rewardText;
    [SerializeField] private Button clickButton;
    [SerializeField] private float clickCooldown = 0.15f;

    [Header("Attempts")]
    [SerializeField] private TMP_Text attemptsText;
    [SerializeField] private AttemptPips pips;

    [Header("Win feedback")]
    [SerializeField] private HitFeedback safeWin = new()
    {
        label = "NICE", labelPop = 0.3f,
        flashAlpha = 0.25f, flashDuration = 0.3f, shake = 3f, shakeDuration = 0.15f,
        bloom = 1.5f, chromaticAberration = 0.2f, lensDistortion = -0.08f, postDuration = 0.3f,
        scorePop = 0.3f, particles = 8, particleSpeed = 220f, popupRise = 40f
    };
    [SerializeField] private HitFeedback riskyWin = new()
    {
        label = "LUCKY!", labelPop = 0.45f,
        flashAlpha = 0.45f, flashDuration = 0.4f, shake = 7f, shakeDuration = 0.3f,
        bloom = 3f, chromaticAberration = 0.5f, lensDistortion = -0.18f, postDuration = 0.5f,
        scorePop = 0.45f, particles = 20, particleSpeed = 300f, popupRise = 40f
    };
    [SerializeField] private HitFeedback longShotWin = new()
    {
        label = "JACKPOT!", labelPop = 0.6f,
        flashAlpha = 0.65f, flashDuration = 0.6f, shake = 14f, shakeDuration = 0.5f, hitStop = 0.12f,
        bloom = 6f, chromaticAberration = 1f, lensDistortion = -0.35f, postDuration = 0.8f,
        scorePop = 0.7f, particles = 45, particleSpeed = 380f, popupRise = 60f
    };

    private float lastClickTime = -10f;
    private OddsTier? currentTier;

    private void Awake()
    {
        probabilitySlider.wholeNumbers = true;
        probabilitySlider.minValue = 1;
        probabilitySlider.maxValue = 99;
        probabilitySlider.onValueChanged.AddListener(OnProbabilityChanged);
        clickButton.onClick.AddListener(OnClickButton);
    }

    private void OnEnable()
    {
        gameManager.OnStateChanged += UpdateUI;
        gameManager.OnRunStarted += HandleRunStarted;
        gameManager.OnSuccess += HandleSuccess;
        gameManager.OnBust += HandleBust;
        gameManager.OnRunEnded += HandleRunEnded;
    }

    private void OnDisable()
    {
        gameManager.OnStateChanged -= UpdateUI;
        gameManager.OnRunStarted -= HandleRunStarted;
        gameManager.OnSuccess -= HandleSuccess;
        gameManager.OnBust -= HandleBust;
        gameManager.OnRunEnded -= HandleRunEnded;
    }

    private void Start()
    {
        OnProbabilityChanged(probabilitySlider.value);
        UpdateUI();
    }

    private void Update()
    {
        if (gameManager.IsRunActive && SpacePressed())
            OnClickButton();
    }

    private static bool SpacePressed()
    {
#if ENABLE_INPUT_SYSTEM
        return Keyboard.current != null && Keyboard.current.spaceKey.wasPressedThisFrame;
#else
        return Input.GetKeyDown(KeyCode.Space);
#endif
    }

    private int SelectedProbability => Mathf.RoundToInt(probabilitySlider.value);

    private static float Risk(int probability) => 1f - Mathf.InverseLerp(1, 99, probability);

    private void UpdateUI()
    {
        attemptsText.text = $"{gameManager.AttemptsRemaining}/{gameManager.MaxAttempts}";
        personalBestText.text = $"BEST {GameManager.FormatScore(gameManager.PersonalBest)}";
        clickButton.interactable = gameManager.IsRunActive;

        int streak = gameManager.Streak;
        streakText.gameObject.SetActive(streak >= 2);
        streakText.text = $"{streak} STREAK {GameManager.FormatMultiplier(gameManager.StreakMultiplier)}";

        pips.SetRemaining(gameManager.AttemptsRemaining);
        UpdateRewardPreview();
    }

    private void UpdateRewardPreview() =>
        rewardText.text = $"+{GameManager.FormatScore(gameManager.GainFor(SelectedProbability))}";

    private void OnProbabilityChanged(float value)
    {
        int probability = SelectedProbability;
        probabilityText.text = $"{probability}%";
        UpdateRewardPreview();

        OddsTier tier = palette.TierFor(probability);
        if (tier == currentTier)
            return;

        bool firstSet = currentTier == null;
        currentTier = tier;

        Color color = palette.ForTier(tier);
        float fade = firstSet ? 0f : 0.2f;
        sliderFill.DOColor(color, fade);
        rewardText.DOColor(color, fade);

        if (firstSet)
            return;
        juice.Pop(rewardText.transform, 0.25f);
    }

    private void OnClickButton()
    {
        if (Time.unscaledTime - lastClickTime < clickCooldown)
            return;

        lastClickTime = Time.unscaledTime;
        sfx.Click();
        juice.Pop(clickButton.transform, 0.15f, 0.2f);
        gameManager.TryAttempt(SelectedProbability);
    }

    private void HandleRunStarted()
    {
        score.Set(0f);
        pips.Refill(gameManager.MaxAttempts);
        SetResult("GOOD LUCK", palette.Ink, 0.3f);
    }

    private void HandleSuccess(float reward, int probability, int streak)
    {
        OddsTier tier = palette.TierFor(probability);
        HitFeedback fx = tier switch
        {
            OddsTier.LongShot => longShotWin,
            OddsTier.Risky => riskyWin,
            _ => safeWin
        };
        Color accent = tier == OddsTier.Safe ? palette.Green : palette.Gold;
        RectTransform scoreRect = score.Rect;
        float risk = Risk(probability);

        sfx.Win(risk, streak, tier == OddsTier.LongShot);
        score.RollTo(gameManager.CurrentScore, Mathf.Lerp(0.3f, 0.8f, risk));
        SetResult(fx.label, accent, fx.labelPop);

        if (fx.hitStop > 0f)
            juice.HitStop(fx.hitStop);
        juice.Flash(palette.Green, fx.flashAlpha, fx.flashDuration);
        juice.Shake(fx.shake, fx.shakeDuration);
        juice.PostKick(bloomBoost: fx.bloom, chromaticAberration: fx.chromaticAberration,
            lensDistortion: fx.lensDistortion, duration: fx.postDuration);
        juice.Burst(scoreRect, fx.particles, new[] { accent, palette.Cream, palette.Green }, fx.particleSpeed);
        juice.Pop(scoreRect, fx.scorePop);
        juice.Popup(scoreRect, $"+{GameManager.FormatScore(reward)}", accent, fx.popupRise);

        float multiplier = gameManager.MultiplierFor(probability);
        if (streak >= 2 && multiplier >= 1.5f)
            juice.Popup(streakText.rectTransform, GameManager.FormatMultiplier(multiplier), palette.Gold, 30f);

        if (streak >= 2)
            juice.Pop(streakText.transform, 0.5f);
    }

    private void HandleBust(float lost, int probability)
    {
        bool lostSomething = lost > 0f;

        sfx.Bust();
        SetResult(lostSomething ? "BUST!" : "MISS", palette.Red, 0.4f);
        juice.Flash(palette.Red, lostSomething ? 0.5f : 0.25f, 0.45f);
        juice.Shake(lostSomething ? 10f : 4f, 0.3f);
        juice.PostKick(chromaticAberration: lostSomething ? 1f : 0.4f, lensDistortion: 0.2f,
            desaturate: lostSomething ? 70f : 30f, duration: 0.6f);

        if (!lostSomething)
            return;
        juice.Popup(score.Rect, $"-{GameManager.FormatScore(lost)}", palette.Red, -36f);
        score.PlayBust(palette.Red);
    }

    private void HandleRunEnded(float finalScore, bool isNewBest) => SetResult("RUN OVER", palette.Ink, 0.4f);

    private void SetResult(string text, Color color, float pop)
    {
        resultText.text = text;
        resultText.color = color;
        juice.Pop(resultText.transform, pop);
    }
}
