using DG.Tweening;
using TMPro;
using UnityEngine;

/// <summary>Shows a one-line rules hint during the player's first run, then never again.</summary>
[RequireComponent(typeof(CanvasGroup))]
public class RuleHint : MonoBehaviour
{
    private const string SeenKey = "SeenRuleHint";

    [SerializeField] private GameManager gameManager;
    [Tooltip("Optional. If set, its text is replaced with the message below.")]
    [SerializeField] private TMP_Text text;
    [SerializeField] private string message = "LOWER CHANCE = MORE POINTS. MISS = LOSE IT ALL.\nHIGHER STREAK = HIGHER MULTIPLIER.";
    [SerializeField] private float showDelay = 0.6f;
    [Tooltip("The hint fades out after this many attempts.")]
    [SerializeField] private int hideAfterAttempts = 3;

    private CanvasGroup group;
    private Vector2 home;
    private bool showing;

    private static bool Seen
    {
        get => PlayerPrefs.GetInt(SeenKey, 0) == 1;
        set
        {
            PlayerPrefs.SetInt(SeenKey, value ? 1 : 0);
            PlayerPrefs.Save();
        }
    }

    private void Awake()
    {
        group = GetComponent<CanvasGroup>();
        group.alpha = 0f;
        group.blocksRaycasts = false;
        home = ((RectTransform)transform).anchoredPosition;
        if (text != null)
            text.text = message;
    }

    private void OnEnable()
    {
        gameManager.OnRunStarted += HandleRunStarted;
        gameManager.OnStateChanged += HandleStateChanged;
    }

    private void OnDisable()
    {
        gameManager.OnRunStarted -= HandleRunStarted;
        gameManager.OnStateChanged -= HandleStateChanged;
    }

    private void HandleRunStarted()
    {
        if (Seen || showing)
            return;

        showing = true;
        var rect = (RectTransform)transform;
        rect.anchoredPosition = home + Vector2.down * 12f;

        group.DOKill();
        rect.DOKill();
        group.DOFade(1f, 0.4f).SetDelay(showDelay);
        rect.DOAnchorPos(home, 0.5f).SetDelay(showDelay).SetEase(Ease.OutBack)
            .OnComplete(() => rect.DOAnchorPosY(home.y + 3f, 0.8f).SetLoops(-1, LoopType.Yoyo).SetEase(Ease.InOutSine));
    }

    private void HandleStateChanged()
    {
        if (!showing || gameManager.AttemptNumber < hideAfterAttempts && gameManager.IsRunActive)
            return;

        showing = false;
        Seen = true;
        transform.DOKill();
        group.DOKill();
        group.DOFade(0f, 0.4f);
    }

    [ContextMenu("Reset Seen Flag")]
    private void ResetSeen() => Seen = false;
}
