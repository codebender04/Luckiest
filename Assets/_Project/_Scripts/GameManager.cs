
using System;
using UnityEngine;

public class GameManager : MonoBehaviour
{
    [Header("Game Settings")]
    [SerializeField] private int maxAttempts = 20;

    private float currentScore;
    private int attemptsRemaining;
    private float personalBest;
    private bool isRunActive;

    public float CurrentScore => currentScore;
    public int AttemptsRemaining => attemptsRemaining;
    public float PersonalBest => personalBest;
    public bool IsRunActive => isRunActive;

    public event Action OnStateChanged;
    public event Action OnRunStarted;
    public event Action<bool, float> OnAttemptResolved;
    public event Action OnRunEnded;

    private const string PersonalBestKey = "PersonalBest";

    private void Awake()
    {
        personalBest = PlayerPrefs.GetInt(PersonalBestKey, 0);
        StartNewRun();
    }

    public void StartNewRun()
    {
        currentScore = 0;
        attemptsRemaining = maxAttempts;
        isRunActive = true;

        OnRunStarted?.Invoke();
        OnStateChanged?.Invoke();
    }

    public void TryAttempt(int probability)
    {
        if (!isRunActive)
            return;

        if (probability < 1 || probability > 100)
            return;

        // Calculate reward
        float reward = 100f / probability;

        // Generate random number from 1 to 100
        int roll = UnityEngine.Random.Range(1, 101);

        // Check success
        bool success = roll <= probability;

        if (success)
        {
            currentScore += reward;
        }

        attemptsRemaining--;

        OnAttemptResolved?.Invoke(success, success ? reward : 0);

        // Update personal best
        if (currentScore > personalBest)
        {
            personalBest = currentScore;
            PlayerPrefs.SetFloat(PersonalBestKey, personalBest);
            PlayerPrefs.Save();
        }

        // Check if run has ended
        if (attemptsRemaining <= 0)
        {
            isRunActive = false;
            OnRunEnded?.Invoke();
        }

        OnStateChanged?.Invoke();
    }
}
