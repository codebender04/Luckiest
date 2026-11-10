using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class LeaderboardUI : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private GameManager gameManager;
    [SerializeField] private LeaderboardService service;

    [Header("UI")]
    [Tooltip("Parent with a VerticalLayoutGroup.")]
    [SerializeField] private RectTransform rowContainer;
    [SerializeField] private LeaderboardRow rowPrefab;
    [Tooltip("Pinned under the list when the player is outside the top rows.")]
    [SerializeField] private LeaderboardRow playerRow;
    [SerializeField] private TMP_Text statusText;
    [SerializeField] private int rowsToShow = 10;

    private readonly List<LeaderboardRow> rows = new();
    private int refreshId;

    private void Start() => Refresh();

    private void OnEnable() => gameManager.OnRunEnded += HandleRunEnded;
    private void OnDisable() => gameManager.OnRunEnded -= HandleRunEnded;

    private async void HandleRunEnded(float finalScore, bool isNewBest)
    {
        if (finalScore <= 0f)
            return;

        SetStatus("SUBMITTING...");
        try
        {
            await service.SubmitScoreAsync(finalScore);
        }
        catch (Exception e)
        {
            Debug.LogWarning($"Leaderboard submit failed: {e.Message}");
            if (this != null) SetStatus("COULDN'T SUBMIT");
            return;
        }

        if (this != null)
            Refresh();
    }

    public async void Refresh()
    {
        int id = ++refreshId;
        SetStatus("LOADING...");

        try
        {
            IReadOnlyList<LeaderboardEntry> top = await service.GetTopScoresAsync(rowsToShow);
            LeaderboardEntry? mine = await service.GetPlayerEntryAsync();
            if (this == null || id != refreshId)
                return;

            Render(top, mine);
            SetStatus(top.Count == 0 ? "NO SCORES YET" : "");
        }
        catch (Exception e)
        {
            Debug.LogWarning($"Leaderboard fetch failed: {e.Message}");
            if (this != null && id == refreshId)
                SetStatus("OFFLINE");
        }
    }

    private void Render(IReadOnlyList<LeaderboardEntry> top, LeaderboardEntry? mine)
    {
        foreach (LeaderboardRow row in rows)
            Destroy(row.gameObject);
        rows.Clear();

        foreach (LeaderboardEntry entry in top)
        {
            LeaderboardRow row = Instantiate(rowPrefab, rowContainer);
            row.Set(entry, entry.IsLocalPlayer);
            rows.Add(row);
        }

        bool showPinned = mine.HasValue && mine.Value.Rank > top.Count;
        playerRow.gameObject.SetActive(showPinned);
        if (showPinned)
            playerRow.Set(mine.Value, true);
    }

    private void SetStatus(string message)
    {
        statusText.text = message;
        statusText.gameObject.SetActive(!string.IsNullOrEmpty(message));
    }
}
