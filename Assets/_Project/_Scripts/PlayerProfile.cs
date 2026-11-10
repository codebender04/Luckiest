using UnityEngine;

public static class PlayerProfile
{
    private const string NameKey = "PlayerName";

    public static string Name
    {
        get => PlayerPrefs.GetString(NameKey, "");
        set
        {
            PlayerPrefs.SetString(NameKey, value);
            PlayerPrefs.Save();
        }
    }

    public static bool HasName => !string.IsNullOrEmpty(Name);
}
