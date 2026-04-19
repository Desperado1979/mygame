using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;

/// <summary>P1-A-5：一键输出 Windows 单机包（Build Settings 中已勾选的场景），便于清单验收。</summary>
public static class EpochOfDawnBuildP1A5
{
    const string BuildExeRelative = "Build/P1-A5-Windows/EpochOfDawn.exe";

    [MenuItem("EpochOfDawn/Build Windows Player (P1-A-5)")]
    public static void BuildWindowsPlayer()
    {
        string projectRoot = Directory.GetParent(Application.dataPath).FullName;
        string exePath = Path.Combine(projectRoot, BuildExeRelative);
        Directory.CreateDirectory(Path.GetDirectoryName(exePath) ?? projectRoot);

        var paths = new List<string>();
        foreach (var s in EditorBuildSettings.scenes)
        {
            if (s.enabled)
                paths.Add(s.path);
        }

        if (paths.Count == 0)
        {
            Debug.LogError("EpochOfDawnBuildP1A5: EditorBuildSettings 中无已启用场景。");
            return;
        }

        BuildReport report = BuildPipeline.BuildPlayer(
            paths.ToArray(),
            exePath,
            BuildTarget.StandaloneWindows64,
            BuildOptions.None);

        if (report.summary.result == BuildResult.Succeeded)
            Debug.Log($"EpochOfDawnBuildP1A5: 成功 → {exePath}");
        else
            Debug.LogError($"EpochOfDawnBuildP1A5: 失败 → {report.summary.result}（见 Console）");
    }
}
