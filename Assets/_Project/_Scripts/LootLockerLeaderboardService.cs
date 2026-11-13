using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using LootLocker;
using LootLocker.Requests;
using UnityEngine;

/// <summary>
/// LootLocker player leaderboard via the LootLocker SDK. Keys live in the SDK's config
/// (Edit → Project Settings → LootLocker SDK). Scores are integers there, so they are sent ×10.
/// </summary>
public class LootLockerLeaderboardService : LeaderboardService
{
    // LootLocker scores are ints. ×10 keeps one decimal and still fits scores up to ~214 million.
    private const float ScoreScale = 10f;

    [Tooltip("The leaderboard's Key from the LootLocker console.")]
    [SerializeField] private string leaderboardKey = "luckiest";

    private Task sessionTask;
    private int playerId;

    public override async Task SubmitScoreAsync(float score)
    {
        await EnsureSession();
        int value = (int)Math.Min(Math.Round((double)score * ScoreScale), int.MaxValue);
        await Request<LootLockerSubmitScoreResponse>(done =>
            LootLockerSDKManager.SubmitScore(playerId.ToString(), value, leaderboardKey, done));
    }

    public override async Task SetPlayerNameAsync(string playerName)
    {
        await EnsureSession();
        await Request<PlayerNameResponse>(done => LootLockerSDKManager.SetPlayerName(playerName, done));
    }

    public override async Task<IReadOnlyList<LeaderboardEntry>> GetTopScoresAsync(int count)
    {
        await EnsureSession();
        var response = await Request<LootLockerGetScoreListResponse>(done =>
            LootLockerSDKManager.GetScoreList(leaderboardKey, count, done));

        var entries = new List<LeaderboardEntry>();
        if (response.items != null)
            foreach (LootLockerLeaderboardMember member in response.items)
                entries.Add(ToEntry(member.rank, member.score, member.player));
        return entries;
    }

    public override async Task<LeaderboardEntry?> GetPlayerEntryAsync()
    {
        await EnsureSession();
        try
        {
            var response = await Request<LootLockerGetMemberRankResponse>(done =>
                LootLockerSDKManager.GetMemberRank(leaderboardKey, playerId.ToString(), done));
            return response.rank > 0 ? ToEntry(response.rank, response.score, response.player) : null;
        }
        catch (LeaderboardRequestException e) when (e.Status == 404)
        {
            return null;
        }
    }

    private LeaderboardEntry ToEntry(int rank, int score, LootLockerPlayer player) => new()
    {
        Rank = rank,
        Score = score / ScoreScale,
        PlayerName = DisplayName(player),
        IsLocalPlayer = player != null && player.id == playerId
    };

    private static string DisplayName(LootLockerPlayer player)
    {
        if (player == null) return "???";
        return string.IsNullOrWhiteSpace(player.name) ? $"GUEST-{player.id}" : player.name;
    }

    private Task EnsureSession() => sessionTask ??= StartSession();

    private async Task StartSession()
    {
        try
        {
            // The SDK remembers the guest identifier, so the player stays the same between visits.
            var response = await Request<LootLockerGuestSessionResponse>(done =>
                LootLockerSDKManager.StartGuestSession(done));
            playerId = response.player_id;
        }
        catch
        {
            sessionTask = null;
            throw;
        }
    }

    private static Task<T> Request<T>(Action<Action<T>> send) where T : LootLockerResponse
    {
        var tcs = new TaskCompletionSource<T>();
        send(response =>
        {
            if (response.success)
                tcs.TrySetResult(response);
            else
                tcs.TrySetException(new LeaderboardRequestException(response.statusCode,
                    $"{typeof(T).Name} failed ({response.statusCode}): {response.errorData?.message ?? response.text}"));
        });
        return tcs.Task;
    }
}

public class LeaderboardRequestException : Exception
{
    public long Status { get; }

    public LeaderboardRequestException(long status, string message) : base(message) => Status = status;
}
