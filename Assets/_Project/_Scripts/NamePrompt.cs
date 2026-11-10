using System;
using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>Asks for a leaderboard name after the first finished run, then remembers it.</summary>
[RequireComponent(typeof(CanvasGroup))]
public class NamePrompt : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private GameManager gameManager;
    [SerializeField] private LeaderboardService service;
    [SerializeField] private LeaderboardUI leaderboard;
    [SerializeField] private Juice juice;
    [SerializeField] private SfxPlayer sfx;

    [Header("UI")]
    [SerializeField] private RectTransform box;
    [SerializeField] private TMP_InputField input;
    [SerializeField] private Button confirmButton;
    [Tooltip("Optional, shows errors like a failed save.")]
    [SerializeField] private TMP_Text errorText;

    [Header("Settings")]
    [Tooltip("Seconds after the run ends, so the end panel can play first.")]
    [SerializeField] private float showDelay = 3f;
    [SerializeField] private int maxLength = 12;

    private CanvasGroup group;
    private Tween pending;
    private bool saving;

    private void Awake()
    {
        group = GetComponent<CanvasGroup>();
        group.alpha = 0f;
        SetVisible(false);

        input.characterLimit = maxLength;
        input.onValidateInput = ValidateChar;
        input.onSubmit.AddListener(_ => Confirm());
        confirmButton.onClick.AddListener(Confirm);
    }

    private void OnEnable()
    {
        gameManager.OnRunEnded += HandleRunEnded;
        gameManager.OnRunStarted += HandleRunStarted;
    }

    private void OnDisable()
    {
        gameManager.OnRunEnded -= HandleRunEnded;
        gameManager.OnRunStarted -= HandleRunStarted;
        pending?.Kill();
    }

    private void HandleRunEnded(float finalScore, bool isNewBest)
    {
        if (PlayerProfile.HasName)
            return;
        pending?.Kill();
        pending = DOVirtual.DelayedCall(showDelay, Show);
    }

    private void HandleRunStarted()
    {
        pending?.Kill();
        if (group.alpha > 0f)
            Hide();
    }

    private void Show()
    {
        input.text = "";
        SetError("");
        SetVisible(true);
        group.alpha = 0f;
        group.DOFade(1f, 0.25f);
        box.localScale = Vector3.one * 0.8f;
        box.DOScale(1f, 0.4f).SetEase(Ease.OutBack);
        input.ActivateInputField();
    }

    private void Hide()
    {
        SetVisible(false);
        group.DOKill();
        group.DOFade(0f, 0.2f);
    }

    private async void Confirm()
    {
        if (saving)
            return;

        string playerName = input.text.Trim();
        if (playerName.Length == 0)
        {
            juice.Pop(box, 0.15f);
            input.ActivateInputField();
            return;
        }

        saving = true;
        confirmButton.interactable = false;
        SetError("");
        sfx.Click();

        try
        {
            await service.SetPlayerNameAsync(playerName);
            if (this == null) return;

            PlayerProfile.Name = playerName;
            Hide();
            leaderboard.Refresh();
        }
        catch (Exception e)
        {
            Debug.LogWarning($"Setting player name failed: {e.Message}");
            if (this == null) return;
            SetError("COULDN'T SAVE, TRY AGAIN");
            juice.Pop(box, 0.15f);
        }
        finally
        {
            saving = false;
            if (this != null)
                confirmButton.interactable = true;
        }
    }

    private static char ValidateChar(string text, int index, char c)
    {
        c = char.ToUpperInvariant(c);
        return char.IsLetterOrDigit(c) || c == '-' || c == '_' || c == ' ' ? c : '\0';
    }

    private void SetVisible(bool visible)
    {
        group.interactable = visible;
        group.blocksRaycasts = visible;
    }

    private void SetError(string message)
    {
        if (errorText == null) return;
        errorText.text = message;
        errorText.gameObject.SetActive(message.Length > 0);
    }
}
