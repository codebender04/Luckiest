using System;
using System.Collections.Generic;
using System.Globalization;
using UnityEngine;

public readonly struct Attempt
{
    public readonly int Probability;
    public readonly bool Won;
    public readonly float Delta;   // reward on a win, minus the wiped score on a bust

    public Attempt(int probability, bool won, float delta)
    {
        Probability = probability;
        Won = won;
        Delta = delta;
    }
}

public class GameManager : MonoBehaviour
{
    private const string PersonalBestKey = "PersonalBest";

    [SerializeField] private int maxAttempts = 10;
    [Tooltip("Off when the main menu starts the first run.")]
    [SerializeField] private bool startRunOnLoad;
    [Tooltip("How much a win multiplies the score you already have. 0 = plain adding, " +
             "1 = a win at p% multiplies your score by 100/p (every choice is worth the same on average).")]
    [Range(0f, 1f)]
    [SerializeField] private float streakStrength = 0.6f;

    private readonly List<Attempt> attempts = new();

    public IReadOnlyList<Attempt> Attempts => attempts;

    public float CurrentScore { get; private set; }
    public int Streak { get; private set; }
    /// <summary>Combined score multiplier of the current streak.</summary>
    public float StreakMultiplier { get; private set; } = 1f;
    public int AttemptsRemaining { get; private set; }
    public float PersonalBest { get; private set; }
    public bool IsRunActive { get; private set; }
    public int MaxAttempts => maxAttempts;
    public int AttemptNumber => maxAttempts - AttemptsRemaining;

    public event Action OnStateChanged;
    public event Action OnRunStarted;
    public event Action<float, int, int> OnSuccess;   // reward, probability, streak
    public event Action<float, int> OnBust;           // score lost, probability
    public event Action<float, bool> OnRunEnded;      // final score, beat personal best

    public static float RewardFor(int probability) => 100f / probability;

    /// <summary>What a win at this chance multiplies the current score by.</summary>
    public float MultiplierFor(int probability) => 1f + streakStrength * (RewardFor(probability) - 1f);

    /// <summary>Points a win at this chance would add right now.</summary>
    public float GainFor(int probability) =>
        CurrentScore * (MultiplierFor(probability) - 1f) + RewardFor(probability);

    public static string FormatScore(float value)
    {
        float abs = Mathf.Abs(value);
        if (abs < 1000f) return value.ToString("0.##", CultureInfo.InvariantCulture);
        if (abs < 1e6f) return value.ToString("#,0", CultureInfo.InvariantCulture);
        if (abs < 1e9f) return (value / 1e6f).ToString("0.##", CultureInfo.InvariantCulture) + "M";
        return (value / 1e9f).ToString("0.##", CultureInfo.InvariantCulture) + "B";
    }

    public static string FormatMultiplier(float value) =>
        "x" + (value < 100f ? value.ToString("0.#", CultureInfo.InvariantCulture) : FormatScore(value));

    private void Awake()
    {
        PersonalBest = PlayerPrefs.GetFloat(PersonalBestKey, 0f);
    }

    private void Start()
    {
        if (startRunOnLoad)
            StartNewRun();
    }

    public void StartNewRun()
    {
        CurrentScore = 0;
        Streak = 0;
        StreakMultiplier = 1f;
        attempts.Clear();
        AttemptsRemaining = maxAttempts;
        IsRunActive = true;

        OnRunStarted?.Invoke();
        OnStateChanged?.Invoke();
    }

    public void TryAttempt(int probability)
    {
        if (!IsRunActive || probability < 1 || probability > 100)
            return;

        AttemptsRemaining--;

        if (UnityEngine.Random.Range(1, 101) <= probability)
        {
            float reward = GainFor(probability);
            CurrentScore += reward;
            Streak++;
            StreakMultiplier *= MultiplierFor(probability);
            attempts.Add(new Attempt(probability, true, reward));
            OnSuccess?.Invoke(reward, probability, Streak);
        }
        else
        {
            float lost = CurrentScore;
            CurrentScore = 0;
            Streak = 0;
            StreakMultiplier = 1f;
            attempts.Add(new Attempt(probability, false, -lost));
            OnBust?.Invoke(lost, probability);
        }

        if (AttemptsRemaining <= 0)
            EndRun();

        OnStateChanged?.Invoke();
    }

    private void EndRun()
    {
        IsRunActive = false;

        bool beatBest = CurrentScore > PersonalBest;
        if (beatBest)
        {
            PersonalBest = CurrentScore;
            PlayerPrefs.SetFloat(PersonalBestKey, PersonalBest);
            PlayerPrefs.Save();
        }

        OnRunEnded?.Invoke(CurrentScore, beatBest);
    }
}
