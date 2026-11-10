using System;
using System.Collections.Generic;
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

    private readonly List<Attempt> attempts = new();

    public IReadOnlyList<Attempt> Attempts => attempts;

    public float CurrentScore { get; private set; }
    public int Streak { get; private set; }
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
    public static string FormatScore(float value) => value.ToString("0.##");

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
            float reward = RewardFor(probability);
            CurrentScore += reward;
            Streak++;
            attempts.Add(new Attempt(probability, true, reward));
            OnSuccess?.Invoke(reward, probability, Streak);
        }
        else
        {
            float lost = CurrentScore;
            CurrentScore = 0;
            Streak = 0;
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
