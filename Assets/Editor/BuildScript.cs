using UnityEditor;

public static class BuildScript
{
    public static void BuildLinux()
    {
        BuildPlayerOptions options = new BuildPlayerOptions
        {
            scenes = new[]
            {
                "Assets/1_FeedTheRealm/Scenes/Testing/NetworkRefactorSampleScene.unity",
            },
            locationPathName = "Build/Client/client.x86_64",
            target = BuildTarget.StandaloneLinux64,
            options = BuildOptions.None,
        };

        BuildPipeline.BuildPlayer(options);
    }
}
