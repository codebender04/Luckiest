using System.Collections.Generic;
using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.UI;

public class Juice : MonoBehaviour
{
    private const string PopId = "pop";

    [Header("Layers")]
    [SerializeField] private RectTransform shakeRoot;
    [SerializeField] private RectTransform fxLayer;
    [SerializeField] private Image flashOverlay;

    [Header("Prefabs")]
    [SerializeField] private Image particlePrefab;
    [SerializeField] private TMP_Text popupPrefab;
    [SerializeField] private Sprite[] particleSprites;

    [Header("Post-processing")]
    [Tooltip("Falls back to the first Volume in the scene.")]
    [SerializeField] private Volume volume;

    private readonly Stack<Image> particlePool = new();
    private Vector2 shakeRootHome;
    private Tween hitStop;
    private Sequence postKick;

    private Bloom bloom;
    private ChromaticAberration chroma;
    private LensDistortion lens;
    private ColorAdjustments colorAdjust;
    private float bloomIntensityBase, bloomThresholdBase, lensBase, saturationBase;

    private void Awake()
    {
        DOTween.Init().SetCapacity(500, 200);

        if (shakeRoot != null)
            shakeRootHome = shakeRoot.anchoredPosition;

        if (flashOverlay != null)
        {
            flashOverlay.raycastTarget = false;
            flashOverlay.color = Color.clear;
        }

        SetupPostFx();
    }

    private void OnDestroy()
    {
        Time.timeScale = 1f;
        hitStop?.Kill();
        postKick?.Kill();
    }

    public void Shake(float strength, float duration)
    {
        if (shakeRoot == null) return;
        shakeRoot.DOKill();
        shakeRoot.anchoredPosition = shakeRootHome;
        shakeRoot.DOShakeAnchorPos(duration, strength, 30, 90f, false, true);
    }

    public void HitStop(float duration)
    {
        hitStop?.Kill();
        Time.timeScale = 0f;
        hitStop = DOVirtual.DelayedCall(duration, () => Time.timeScale = 1f, ignoreTimeScale: true);
    }

    public void Flash(Color color, float peakAlpha = 0.5f, float duration = 0.35f)
    {
        if (flashOverlay == null) return;
        flashOverlay.DOKill();
        color.a = peakAlpha;
        flashOverlay.color = color;
        flashOverlay.DOFade(0f, duration).SetEase(Ease.OutQuad);
    }

    public void Pop(Transform target, float amount = 0.3f, float duration = 0.35f)
    {
        DOTween.Kill(target, PopId);
        target.localScale = Vector3.one;
        target.DOPunchScale(new Vector3(-amount * 0.4f, amount, 0f), duration, 8, 0.6f).SetId(PopId);
    }

    public void PostKick(float bloomBoost = 0f, float chromaticAberration = 0f, float lensDistortion = 0f,
        float desaturate = 0f, float duration = 0.4f)
    {
        postKick?.Kill();
        postKick = DOTween.Sequence();

        if (bloom != null)
        {
            postKick.Join(Kick(bloom.intensity, bloomIntensityBase, bloomBoost, duration));
            postKick.Join(Kick(bloom.threshold, bloomThresholdBase, bloomBoost > 0f ? -bloomThresholdBase * 0.45f : 0f, duration));
        }
        if (chroma != null)
            postKick.Join(Kick(chroma.intensity, 0f, chromaticAberration, duration));
        if (lens != null)
            postKick.Join(Kick(lens.intensity, lensBase, lensDistortion, duration));
        if (colorAdjust != null)
            postKick.Join(Kick(colorAdjust.saturation, saturationBase, -desaturate, duration * 1.5f));
    }

    public void Burst(RectTransform from, int count, Color[] colors, float speed = 260f)
    {
        Vector2 origin = ToFxSpace(from);
        for (int i = 0; i < count; i++)
            SpawnParticle(origin, colors[Random.Range(0, colors.Length)], speed);
    }

    public void Popup(RectTransform from, string text, Color color, float rise = 40f)
    {
        TMP_Text label = Instantiate(popupPrefab, fxLayer);
        label.raycastTarget = false;
        label.text = text;
        label.color = color;

        RectTransform rt = label.rectTransform;
        Vector2 origin = ToFxSpace(from);
        rt.anchoredPosition = origin;
        rt.localScale = Vector3.zero;

        DOTween.Sequence()
            .Append(rt.DOScale(1f, 0.3f).SetEase(Ease.OutBack, 3f))
            .Join(rt.DOAnchorPosY(origin.y + rise, 0.9f).SetEase(Ease.OutCubic))
            .Insert(0.55f, label.DOFade(0f, 0.35f))
            .SetLink(label.gameObject)
            .OnComplete(() => Destroy(label.gameObject));
    }

    private void SpawnParticle(Vector2 origin, Color color, float speed)
    {
        Image p = GetParticle();
        if (particleSprites != null && particleSprites.Length > 0)
            p.sprite = particleSprites[Random.Range(0, particleSprites.Length)];
        p.SetNativeSize();
        p.color = color;

        RectTransform rt = p.rectTransform;
        rt.anchoredPosition = origin;
        rt.localRotation = Quaternion.identity;
        rt.localScale = Vector3.one * Random.Range(0.8f, 1.6f);

        float angle = Random.Range(20f, 160f) * Mathf.Deg2Rad;
        float power = speed * Random.Range(0.4f, 1.1f);
        float life = Random.Range(0.55f, 0.95f);
        Vector2 end = origin + new Vector2(Mathf.Cos(angle) * power * life, -Random.Range(40f, 140f));
        float height = Mathf.Sin(angle) * power * 0.4f;

        DOTween.Sequence()
            .Join(rt.DOJumpAnchorPos(end, height, 1, life))
            .Join(rt.DORotate(new Vector3(0f, 0f, Random.Range(-540f, 540f)), life, RotateMode.FastBeyond360).SetEase(Ease.OutQuad))
            .Insert(life * 0.6f, rt.DOScale(0f, life * 0.4f).SetEase(Ease.InBack))
            .OnComplete(() => ReleaseParticle(p));
    }

    private Image GetParticle()
    {
        Image p = particlePool.Count > 0 ? particlePool.Pop() : Instantiate(particlePrefab, fxLayer);
        p.raycastTarget = false;
        p.gameObject.SetActive(true);
        return p;
    }

    private void ReleaseParticle(Image p)
    {
        if (p == null) return;
        p.gameObject.SetActive(false);
        particlePool.Push(p);
    }

    private Vector2 ToFxSpace(RectTransform from) =>
        fxLayer.InverseTransformPoint(from.TransformPoint(from.rect.center));

    private void SetupPostFx()
    {
        if (volume == null)
            volume = FindAnyObjectByType<Volume>();
        if (volume == null) return;

        VolumeProfile profile = volume.profile; // runtime copy, the asset stays untouched

        bloom = Enable<Bloom>(profile);
        bloom.intensity.overrideState = true;
        bloom.threshold.overrideState = true;
        bloomIntensityBase = bloom.intensity.value;
        bloomThresholdBase = bloom.threshold.value;

        chroma = Enable<ChromaticAberration>(profile);
        chroma.intensity.overrideState = true;
        chroma.intensity.value = 0f;

        lens = Enable<LensDistortion>(profile);
        lens.intensity.overrideState = true;
        lensBase = lens.intensity.value;

        colorAdjust = Enable<ColorAdjustments>(profile);
        colorAdjust.saturation.overrideState = true;
        saturationBase = colorAdjust.saturation.value;
    }

    private static T Enable<T>(VolumeProfile profile) where T : VolumeComponent
    {
        if (!profile.TryGet(out T component))
            component = profile.Add<T>();
        component.active = true;
        return component;
    }

    private static Tween Kick(FloatParameter parameter, float baseValue, float amount, float duration)
    {
        parameter.value = baseValue + amount;
        return DOTween.To(() => parameter.value, v => parameter.value = v, baseValue, duration).SetEase(Ease.OutCubic);
    }
}
