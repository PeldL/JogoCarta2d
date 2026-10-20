using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;

public static class CaseZeroWindowsBuild
{
    [MenuItem("Tools/Caso Zero/Gerar build Windows no HD")]
    public static void Build()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode) return;
        var folder = Path.Combine(@"D:\Builds\CasoZero", DateTime.Now.ToString("yyyy-MM-dd_HHmmss"));
        Directory.CreateDirectory(folder);
        var result = BuildPipeline.BuildPlayer(new BuildPlayerOptions {
            scenes = EditorBuildSettings.scenes.Where(s => s.enabled).Select(s => s.path).ToArray(),
            locationPathName = Path.Combine(folder, "CasoZero.exe"),
            target = BuildTarget.StandaloneWindows64,
            options = BuildOptions.None
        });
        var report = result.summary.result + "\n" + folder + "\nErrors: " + result.summary.totalErrors + "\nWarnings: " + result.summary.totalWarnings;
        File.WriteAllText(Path.Combine(Path.GetTempPath(), "CasoZeroWindowsBuild.txt"), report);
        if (result.summary.result != BuildResult.Succeeded) throw new Exception("Build Windows falhou: " + report);
        File.WriteAllText(Path.Combine(folder, "README.txt"),
            "DETECTIVE: CASE ZERO\n\nOpen CasoZero.exe. Keep all folders beside the executable.\n" +
            "New game: choose a slot. Tab opens/closes the case file; click clues to investigate.\n" +
            "Solve each case to unlock the next. Later cases feature four suspects.\n" +
            "Automatic and manual saves use JSON; build saves are separate from Editor saves.\n" +
            "New locations are drafts awaiting the team's artwork.\n");
        Debug.Log("[Caso Zero] Build Windows concluída em " + folder);
    }
}
