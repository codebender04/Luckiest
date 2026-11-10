using UnityEngine;

[RequireComponent(typeof(AudioSource))]
public class SfxPlayer : MonoBehaviour
{
    [SerializeField] private AudioClip click;
    [SerializeField] private AudioClip win;
    [SerializeField] private AudioClip jackpot;
    [SerializeField] private AudioClip bust;
    [SerializeField] private AudioClip newBest;
    [SerializeField] private AudioClip tick;
    [SerializeField] private AudioClip runEnd;

    [Range(0f, 1f)] [SerializeField] private float volume = 0.7f;

    private AudioSource source;

    private void Awake()
    {
        source = GetComponent<AudioSource>();
        source.playOnAwake = false;
        source.ignoreListenerPause = true;
    }

    public void Click() => Play(click, 1f);
    public void Bust() => Play(bust, 1f);
    public void NewBest() => Play(newBest, 1f);
    public void RunEnd() => Play(runEnd, 1f);
    public void Tick(float pitch = 1f) => Play(tick, pitch, 0.35f);

    public void Win(float risk, int streak, bool isJackpot)
    {
        if (isJackpot && jackpot != null)
        {
            Play(jackpot, 1f);
            return;
        }

        float pitch = Mathf.Lerp(0.9f, 1.5f, risk) + Mathf.Min(streak - 1, 6) * 0.04f;
        Play(win, pitch);
    }

    private void Play(AudioClip clip, float pitch, float volumeScale = 1f)
    {
        if (clip == null) return;
        source.pitch = pitch;
        source.PlayOneShot(clip, volume * volumeScale);
    }
}
