using UnityEngine;
using UnityEngine.UI;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

/// <summary>Toggles all game audio. Also bound to the M key. The choice is remembered.</summary>
[RequireComponent(typeof(Button))]
public class MuteButton : MonoBehaviour
{
    private const string MutedKey = "Muted";

    [SerializeField] private Image icon;
    [SerializeField] private Sprite soundOn;
    [SerializeField] private Sprite soundOff;
    [Tooltip("Optional, pops the button when toggled.")]
    [SerializeField] private Juice juice;

    private static bool Muted
    {
        get => PlayerPrefs.GetInt(MutedKey, 0) == 1;
        set
        {
            PlayerPrefs.SetInt(MutedKey, value ? 1 : 0);
            PlayerPrefs.Save();
        }
    }

    private void Awake()
    {
        GetComponent<Button>().onClick.AddListener(Toggle);
        Apply();
    }

    private void Update()
    {
#if ENABLE_INPUT_SYSTEM
        if (Keyboard.current != null && Keyboard.current.mKey.wasPressedThisFrame)
#else
        if (Input.GetKeyDown(KeyCode.M))
#endif
            Toggle();
    }

    public void Toggle()
    {
        Muted = !Muted;
        Apply();
        if (juice != null)
            juice.Pop(transform, 0.3f);
    }

    private void Apply()
    {
        bool muted = Muted;
        AudioListener.volume = muted ? 0f : 1f;
        if (icon != null)
            icon.sprite = muted ? soundOff : soundOn;
    }
}
