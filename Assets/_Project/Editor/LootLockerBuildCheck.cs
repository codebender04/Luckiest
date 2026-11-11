using LootLocker;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;

/// <summary>
/// Before every build: removes the LootLocker editor admin token from the config (it would ship
/// inside the build otherwise) and warns when a development API key is about to be shipped.
/// </summary>
public class LootLockerBuildCheck : IPreprocessBuildWithReport
{
    public int callbackOrder => 0;

    public void OnPreprocessBuild(BuildReport report)
    {
        var config = Resources.Load<LootLockerConfig>("Config/LootLockerConfig");
        if (config == null)
            return;

        if (!string.IsNullOrEmpty(config.adminToken))
        {
            config.adminToken = "";
            EditorUtility.SetDirty(config);
            AssetDatabase.SaveAssets();
            Debug.Log("LootLocker admin token removed from the config before building.");
        }

        if (config.apiKey != null && config.apiKey.StartsWith("dev_"))
            Debug.LogWarning("Building with a LootLocker dev_ API key. Use the prod_ (Live) key for the itch.io release.");
    }
}
