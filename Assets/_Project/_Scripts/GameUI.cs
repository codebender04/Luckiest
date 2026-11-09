
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class GameUI : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private GameManager gameManager;
    [SerializeField] private ProbabilityController probabilityController;

    [Header("UI Elements")]
    [SerializeField] private Slider probabilitySlider;
    [SerializeField] private TMP_Text scoreText;
    [SerializeField] private TMP_Text probabilityText;
    [SerializeField] private TMP_Text attemptsText;
    [SerializeField] private TMP_Text rewardText;
    [SerializeField] private TMP_Text resultText;
    [SerializeField] private TMP_Text recentResultsText;
    [SerializeField] private TMP_Text personalBestText;

    [Header("Buttons")]
    [SerializeField] private Button clickButton;
    [SerializeField] private Button playAgainButton;

    private readonly List<string> recentResults = new();

    private void Start()
    {
        gameManager.OnStateChanged += UpdateUI;
        gameManager.OnRunStarted += HandleRunStarted;
        gameManager.OnAttemptResolved += HandleAttemptResolved;
        gameManager.OnRunEnded += HandleRunEnded;

        probabilitySlider.onValueChanged.AddListener(OnProbabilityChanged);
        clickButton.onClick.AddListener(OnClickButton);
        playAgainButton.onClick.AddListener(OnPlayAgainButton);

        playAgainButton.gameObject.SetActive(false);

        UpdateUI();
        OnProbabilityChanged(probabilitySlider.value);
    }

    private void OnDestroy()
    {
        if (gameManager == null)
            return;

        gameManager.OnStateChanged -= UpdateUI;
        gameManager.OnRunStarted -= HandleRunStarted;
        gameManager.OnAttemptResolved -= HandleAttemptResolved;
        gameManager.OnRunEnded -= HandleRunEnded;
    }

    private void UpdateUI()
    {
        scoreText.text = gameManager.CurrentScore.ToString();
        attemptsText.text = $"Attempts: {gameManager.AttemptsRemaining}";
        personalBestText.text = $"Personal Best: {gameManager.PersonalBest}";

        clickButton.interactable = gameManager.IsRunActive;
    }

    private void OnProbabilityChanged(float value)
    {
        int probability = probabilityController.GetProbability(value);
        probabilityText.text = $"Probability: {probability}%";
        if (probability == 0)
        {
            rewardText.text = "Potential reward: --";
            return;
        }

        float reward = probabilityController.GetReward(probability);

        rewardText.text = $"Potential reward: +{reward}";
    }

    private void OnClickButton()
    {
        int probability = probabilityController.GetProbability(probabilitySlider.value);

        if (probability == 0)
            return;

        gameManager.TryAttempt(probability);
    }

    private void HandleAttemptResolved(bool success, float reward)
    {
        if (success)
        {
            resultText.text = $"LUCKY! +{reward}";
            recentResults.Insert(0, $"SUCCESS +{reward}");
        }
        else
        {
            resultText.text = "UNLUCKY!";
            recentResults.Insert(0, "FAILED");
        }

        recentResultsText.text = string.Join("\n", recentResults);
    }

    private void HandleRunStarted()
    {
        recentResults.Clear();

        recentResultsText.text = "";
        resultText.text = "GOOD LUCK!";
        playAgainButton.gameObject.SetActive(false);
    }

    private void HandleRunEnded()
    {
        resultText.text = "RUN COMPLETE!";
        playAgainButton.gameObject.SetActive(true);
    }

    private void OnPlayAgainButton()
    {
        gameManager.StartNewRun();
    }
}
