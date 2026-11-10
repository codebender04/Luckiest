using DG.Tweening;
using TMPro;
using UnityEngine;

[RequireComponent(typeof(CanvasGroup))]
public class StatRow : MonoBehaviour
{
    [SerializeField] private TMP_Text valueText;

    private CanvasGroup group;

    private void Awake() => group = GetComponent<CanvasGroup>();

    public void Set(string value, Color color)
    {
        valueText.text = value;
        valueText.color = color;
    }

    public void Hide()
    {
        group.DOKill();
        transform.DOKill();
        group.alpha = 0f;
        transform.localScale = new Vector3(0f, 1f, 1f);
    }

    public Sequence Reveal() => DOTween.Sequence()
        .Join(group.DOFade(1f, 0.2f))
        .Join(transform.DOScaleX(1f, 0.25f).SetEase(Ease.OutBack));
}
