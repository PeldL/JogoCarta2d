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
        File.WriteAllText(Path.Combine(folder, "LEIA-ME.txt"),
            "DETETIVE: CASO ZERO\n\nAbra CasoZero.exe. Mantenha todas as pastas junto do executável.\n" +
            "Novo jogo: escolha um slot. Tab abre/fecha o documento; clique nas pistas para investigar.\n" +
            "Conclua cada caso para liberar o próximo. Os novos casos têm quatro suspeitos.\n" +
            "Save automático e manual em JSON; os saves desta build são separados dos saves do Editor.\n" +
            "Os cenários novos são rascunhos para receber a arte da equipe.\n");
        Debug.Log("[Caso Zero] Build Windows concluída em " + folder);
    }
}
