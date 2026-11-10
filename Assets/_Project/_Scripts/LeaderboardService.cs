using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;

[Serializable]
public struct LeaderboardEntry
{
    public int Rank;
    public string PlayerName;
    public float Score;
    public bool IsLocalPlayer;
}

public abstract class LeaderboardService : MonoBehaviour
{
    public abstract Task SubmitScoreAsync(float score);
    public abstract Task SetPlayerNameAsync(string playerName);
    public abstract Task<IReadOnlyList<LeaderboardEntry>> GetTopScoresAsync(int count);

    /// <summary>The local player's entry, or null if they haven't submitted a score yet.</summary>
    public abstract Task<LeaderboardEntry?> GetPlayerEntryAsync();
}
