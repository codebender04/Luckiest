using System.Collections.Generic;
using UnityEngine;

public class HistoryPanel : MonoBehaviour
{
    [SerializeField] private GameManager gameManager;
    [SerializeField] private Palette palette;
    [Tooltip("Parent with a VerticalLayoutGroup. Rows are added top to bottom.")]
    [SerializeField] private RectTransform rowContainer;
    [SerializeField] private HistoryRow rowPrefab;

    private readonly List<HistoryRow> rows = new();
    private int shown;

    private void Awake()
    {
        for (int i = 0; i < gameManager.MaxAttempts; i++)
        {
            HistoryRow row = Instantiate(rowPrefab, rowContainer);
            row.Init(i, palette);
            rows.Add(row);
        }
    }

    private void OnEnable()
    {
        gameManager.OnRunStarted += HandleRunStarted;
        gameManager.OnSuccess += HandleSuccess;
        gameManager.OnBust += HandleBust;
    }

    private void OnDisable()
    {
        gameManager.OnRunStarted -= HandleRunStarted;
        gameManager.OnSuccess -= HandleSuccess;
        gameManager.OnBust -= HandleBust;
    }

    private void HandleSuccess(float reward, int probability, int streak) => Refresh();
    private void HandleBust(float lost, int probability) => Refresh();

    private void HandleRunStarted()
    {
        shown = 0;
        for (int i = 0; i < rows.Count; i++)
            rows[i].ShowEmpty(i * 0.03f);
        PulseNext();
    }

    private void Refresh()
    {
        IReadOnlyList<Attempt> attempts = gameManager.Attempts;
        for (; shown < attempts.Count && shown < rows.Count; shown++)
            rows[shown].ShowAttempt(attempts[shown]);

        var stats = new RunStats(attempts);
        float delay = 0f;
        for (int i = 0; i < shown; i++)
        {
            if (!stats.Wiped[i] || rows[i].IsWiped)
                continue;
            rows[i].Wipe(delay);
            delay += 0.06f;
        }

        PulseNext();
    }

    private void PulseNext()
    {
        if (shown < rows.Count)
            rows[shown].Pulse();
    }
}
