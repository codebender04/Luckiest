using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using UnityEngine;

/// <summary>Offline stand-in that keeps the top scores on this machine. Useful for testing without a network.</summary>
public class LocalLeaderboardService : LeaderboardService
{
    private const string Key = "LocalLeaderboard";

    [SerializeField] private int maxStored = 50;

    [Serializable] private class Row { public string name; public float score; public bool mine; }
    [Serializable] private class Table { public List<Row> rows = new(); }

    private Table table;

    private void Awake()
    {
        string json = PlayerPrefs.GetString(Key, "");
        table = string.IsNullOrEmpty(json) ? new Table() : JsonUtility.FromJson<Table>(json);
    }

    private static string CurrentName => PlayerProfile.HasName ? PlayerProfile.Name : "YOU";

    public override Task SubmitScoreAsync(float score)
    {
        table.rows.Add(new Row { name = CurrentName, score = score, mine = true });
        table.rows = table.rows.OrderByDescending(r => r.score).Take(maxStored).ToList();
        Save();
        return Task.CompletedTask;
    }

    public override Task SetPlayerNameAsync(string playerName)
    {
        foreach (Row row in table.rows.Where(r => r.mine))
            row.name = playerName;
        Save();
        return Task.CompletedTask;
    }

    public override Task<IReadOnlyList<LeaderboardEntry>> GetTopScoresAsync(int count)
    {
        IReadOnlyList<LeaderboardEntry> result = table.rows
            .Take(count)
            .Select((r, i) => ToEntry(r, i))
            .ToList();
        return Task.FromResult(result);
    }

    public override Task<LeaderboardEntry?> GetPlayerEntryAsync()
    {
        int index = table.rows.FindIndex(r => r.mine);
        LeaderboardEntry? entry = index < 0 ? null : ToEntry(table.rows[index], index);
        return Task.FromResult(entry);
    }

    private static LeaderboardEntry ToEntry(Row row, int index) => new()
    {
        Rank = index + 1,
        PlayerName = row.name,
        Score = row.score,
        IsLocalPlayer = row.mine
    };

    private void Save()
    {
        PlayerPrefs.SetString(Key, JsonUtility.ToJson(table));
        PlayerPrefs.Save();
    }
}
