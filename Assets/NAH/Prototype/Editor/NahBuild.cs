using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;

// Builds a Windows development player of the gray network arena for two-window tests on one PC.
// Development builds keep the demo driver and show Unity's "Development Build" mark. Not a release build.
public static class NahBuild
{
    const string ScenePath = "Assets/NAH/Prototype/NetworkArena.unity";
    const string DefaultOutput = "Builds/Windows-Dev/NAH.exe";

    [MenuItem("NAH/Build Windows Development Player")]
    public static void BuildWindowsDevelopment()
    {
        string output = ReadArg("-nahBuildOutput") ?? DefaultOutput;
        var options = new BuildPlayerOptions
        {
            scenes = new[] { ScenePath },
            locationPathName = output,
            target = BuildTarget.StandaloneWindows64,
            options = BuildOptions.Development
        };

        BuildReport report = BuildPipeline.BuildPlayer(options);
        BuildSummary summary = report.summary;
        Debug.Log("NAH build " + summary.result + ": " + summary.totalErrors + " errors, " + summary.totalWarnings +
            " warnings, " + summary.totalSize + " bytes, " + summary.totalTime + ", " + summary.outputPath);

        if (Application.isBatchMode)
            EditorApplication.Exit(summary.result == BuildResult.Succeeded ? 0 : 1);
    }

    static string ReadArg(string name)
    {
        string[] args = System.Environment.GetCommandLineArgs();
        for (int i = 0; i < args.Length - 1; i++)
        {
            if (args[i] == name)
                return args[i + 1];
        }
        return null;
    }
}
