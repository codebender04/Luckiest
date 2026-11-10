using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

[RequireComponent(typeof(CanvasGroup))]
public class MainMenu : MonoBehaviour, IPointerDownHandler
{
    [SerializeField] private GameManager gameManager;

    [Header("Texts")]
    [SerializeField] private TMP_Text titleText;
    [SerializeField] private TMP_Text theText;
    [SerializeField] private TMP_Text tapText;
    [Tooltip("Optional.")]
    [SerializeField] private SfxPlayer sfx;

    [Header("Title pop")]
    [SerializeField] private float startDelay = 0.2f;
    [SerializeField] private float popDuration = 0.45f;

    [Header("\"The\" jump")]
    [Tooltip("Where \"The\" starts, relative to its resting position.")]
    [SerializeField] private Vector2 jumpFrom = new(-260f, 180f);
    [SerializeField] private float jumpHeight = 70f;
    [SerializeField] private float jumpDuration = 0.6f;
    [SerializeField] private float spin = 360f;

    [Header("Landing bounce")]
    [SerializeField] private float bounceDistance = 8f;
    [SerializeField] private float bounceDuration = 0.4f;

    [Header("Tap to start")]
    [SerializeField] private float blinkDuration = 0.9f;
    [SerializeField] private float blinkMinAlpha = 0.15f;
    [Tooltip("Taps before this many seconds are ignored, so the intro isn't skipped by accident.")]
    [SerializeField] private float tapLockout = 0.4f;
    [SerializeField] private float exitDuration = 0.35f;

    private CanvasGroup group;
    private RectTransform title;
    private RectTransform the;
    private Vector2 titleHome;
    private Vector2 theHome;
    private Sequence intro;
    private Sequence exit;
    private float shownAt;

    private void Awake()
    {
        group = GetComponent<CanvasGroup>();
        title = titleText.rectTransform;
        the = theText.rectTransform;
        titleHome = title.anchoredPosition;
        theHome = the.anchoredPosition;
    }

    private void OnEnable() => PlayIntro();

    private void OnDisable()
    {
        intro?.Kill();
        tapText.DOKill();
    }

    private void OnDestroy() => exit?.Kill();

    private void Update()
    {
        if (AnyKeyPressed())
            Dismiss();
    }

    public void OnPointerDown(PointerEventData eventData) => Dismiss();

    private static bool AnyKeyPressed()
    {
#if ENABLE_INPUT_SYSTEM
        return Keyboard.current != null && Keyboard.current.anyKey.wasPressedThisFrame;
#else
        return Input.anyKeyDown && !Input.GetMouseButtonDown(0);
#endif
    }

    private void Dismiss()
    {
        if (exit != null || Time.unscaledTime - shownAt < tapLockout)
            return;

        intro?.Kill();
        tapText.DOKill();
        group.blocksRaycasts = false;
        sfx?.Click();

        exit = DOTween.Sequence()
            .Append(group.DOFade(0f, exitDuration).SetEase(Ease.InQuad))
            .Join(title.DOScale(1.3f, exitDuration).SetEase(Ease.InBack))
            .Join(the.DOAnchorPos(theHome + Vector2.up * 40f, exitDuration).SetEase(Ease.InBack))
            .OnComplete(() =>
            {
                exit = null;
                gameObject.SetActive(false);
                gameManager.StartNewRun();
            });
    }

    public void PlayIntro()
    {
        intro?.Kill();
        exit?.Kill();
        exit = null;
        tapText.DOKill();
        ResetPose();

        shownAt = Time.unscaledTime;
        group.alpha = 1f;
        group.blocksRaycasts = true;

        intro = DOTween.Sequence()
            .AppendInterval(startDelay)
            .AppendCallback(() => sfx?.Tick(0.9f))
            .Append(title.DOScale(1f, popDuration).SetEase(Ease.OutBack, 3f))
            .Join(titleText.DOFade(1f, popDuration * 0.5f))
            .AppendCallback(() => sfx?.Tick(1.3f))
            .Append(the.DOJumpAnchorPos(theHome, jumpHeight, 1, jumpDuration).SetEase(Ease.InQuad))
            .Join(the.DORotate(Vector3.zero, jumpDuration, RotateMode.FastBeyond360).SetEase(Ease.OutQuad))
            .Join(theText.DOFade(1f, jumpDuration * 0.3f))
            .AppendCallback(() => sfx?.Click())
            .Append(Bounce(the, theText.transform, 0.3f))
            .Join(Bounce(title, titleText.transform, 0.12f))
            .Append(tapText.DOFade(1f, 0.4f))
            .AppendCallback(StartBlink);
    }

    private Sequence Bounce(RectTransform rect, Transform target, float squash) => DOTween.Sequence()
        .Join(rect.DOPunchAnchorPos(Vector2.down * bounceDistance, bounceDuration, 6, 0f))
        .Join(target.DOPunchScale(new Vector3(squash, -squash, 0f), bounceDuration, 6, 0.5f));

    private void StartBlink() =>
        tapText.DOFade(blinkMinAlpha, blinkDuration).SetLoops(-1, LoopType.Yoyo).SetEase(Ease.InOutSine);

    private void ResetPose()
    {
        title.anchoredPosition = titleHome;
        title.localScale = Vector3.zero;
        titleText.alpha = 0f;

        the.anchoredPosition = theHome + jumpFrom;
        the.localScale = Vector3.one;
        the.localRotation = Quaternion.Euler(0f, 0f, spin);
        theText.alpha = 0f;

        tapText.alpha = 0f;
    }
}
