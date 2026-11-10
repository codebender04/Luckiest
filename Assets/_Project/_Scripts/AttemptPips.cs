using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;

public class AttemptPips : MonoBehaviour
{
    [SerializeField] private Image pipPrefab;
    [SerializeField] private Sprite full;
    [SerializeField] private Sprite empty;
    [SerializeField] private Juice juice;

    private readonly List<Image> pips = new();

    public void Refill(int count)
    {
        while (pips.Count < count)
        {
            Image pip = Instantiate(pipPrefab, transform);
            pip.sprite = full;
            pip.SetNativeSize();
            pips.Add(pip);
        }

        for (int i = 0; i < pips.Count; i++)
        {
            Image pip = pips[i];
            pip.gameObject.SetActive(i < count);
            pip.sprite = full;
            pip.transform.DOKill();
            pip.transform.localRotation = Quaternion.identity;
            pip.transform.localScale = Vector3.zero;
            pip.transform.DOScale(1f, 0.35f).SetDelay(i * 0.04f).SetEase(Ease.OutBack, 2.5f);
        }
    }

    public void SetRemaining(int remaining)
    {
        for (int i = 0; i < pips.Count; i++)
        {
            bool isFull = i < remaining;
            if (pips[i].sprite == full && !isFull)
                Spend(pips[i]);
            pips[i].sprite = isFull ? full : empty;
        }
    }

    private void Spend(Image pip)
    {
        juice.Pop(pip.transform, 0.6f, 0.4f);
        pip.transform.DOPunchRotation(new Vector3(0f, 0f, 25f), 0.4f, 10);
    }
}
