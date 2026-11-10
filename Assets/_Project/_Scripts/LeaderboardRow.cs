using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class LeaderboardRow : MonoBehaviour
{
    [Tooltip("Shows the rank and name together, e.g. \"1. KHOA\".")]
    [SerializeField] private TMP_Text nameText;
    [SerializeField] private TMP_Text scoreText;
    [SerializeField] private Image background;
    [SerializeField] private Color normalBackground = new(0, 0, 0, 0);
    [SerializeField] private Color highlightBackground = new(0.13f, 0.13f, 0.13f, 0.12f);

    public void Set(LeaderboardEntry entry, bool highlight)
    {
        nameText.text = $"{entry.Rank}. {entry.PlayerName}";
        scoreText.text = GameManager.FormatScore(entry.Score);
        if (background != null)
            background.color = highlight ? highlightBackground : normalBackground;
    }
}
