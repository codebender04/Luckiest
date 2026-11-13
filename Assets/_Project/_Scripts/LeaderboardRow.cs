using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class LeaderboardRow : MonoBehaviour
{
    [SerializeField] private TMP_Text rankText;
    [SerializeField] private TMP_Text nameText;
    [SerializeField] private TMP_Text scoreText;
    [SerializeField] private Image background;
    [SerializeField] private Color normalBackground = new(0, 0, 0, 0);
    [SerializeField] private Color highlightBackground = new(0.13f, 0.13f, 0.13f, 0.12f);

    public void Set(LeaderboardEntry entry, bool highlight)
    {
        rankText.text = $"{entry.Rank}.";
        nameText.text = $"{entry.PlayerName}";
        scoreText.text = GameManager.FormatScore(entry.Score);
        if (background != null)
            background.color = highlight ? highlightBackground : normalBackground;
    }
}
