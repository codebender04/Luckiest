using System;

[Serializable]
public struct HitFeedback
{
    public string label;
    public float labelPop;

    [UnityEngine.Header("Screen")]
    public float flashAlpha;
    public float flashDuration;
    public float shake;
    public float shakeDuration;
    public float hitStop;

    [UnityEngine.Header("Post-processing")]
    public float bloom;
    public float chromaticAberration;
    public float lensDistortion;
    public float postDuration;

    [UnityEngine.Header("Score")]
    public float scorePop;
    public int particles;
    public float particleSpeed;
    public float popupRise;
}
