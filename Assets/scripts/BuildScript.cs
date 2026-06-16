// using UnityEditor;
// using System.IO;

// public static class BuildScript
// {
//     [MenuItem("Builds/Perform Windows Build")]
//     public static void PerformWindowsBuild()
//     {
//         // Define the build output folder
//         string buildPath = "Builds/Windows";

//         // Ensure the output directory exists
//         if (!Directory.Exists(buildPath))
//             Directory.CreateDirectory(buildPath);

//         // Define the build options
//         BuildPlayerOptions buildPlayerOptions = new BuildPlayerOptions();
//         buildPlayerOptions.scenes = new[] { "Assets/Scenes/MAIN.unity", "Assets/Scenes/plutoDiagnostics.unity", "Assets/Scenes/AAN.unity", "Assets/Scenes/CHMECH.unity", "Assets/Scenes/CALIB.unity", "Assets/Scenes/CHGAME.unity", "Assets/Scenes/ASSESS.unity",  "Assets/Scenes/ASSISTPROFILE.unity","Assets/Games/HAT-CV/_Scenes/HATCV.unity","Assets/Games/Ping Pong/Scenes/PONGMENU.unity", "Assets/Games/Ping Pong/Scenes/PONGGAME.unity", 
//         "Assets/Games/Hatrick/_Scenes/HAT.unity", "Assets/Games/FlappyBirdStyleAssets/Scenes/TUK.unity", "Assets/Games/FruitBasket/scenes/FRUITBASKETS.unity","Assets/Games/RNR/Assets/Scenes/RNRMENU.unity","Assets/Games/RNR/Assets/Scenes/RNR.unity", "Assets/Scenes/SUMM.unity", "Assets/Scenes/DATAUPLOAD.unity", "Assets/Scenes/CONFIG.unity"}; // Change to your scene(s)
//         buildPlayerOptions.locationPathName = Path.Combine(buildPath, "PLUTO.exe"); // Output file name
//         buildPlayerOptions.target = BuildTarget.StandaloneWindows64;
//         buildPlayerOptions.options = BuildOptions.None;

//         // Perform the build
//         BuildPipeline.BuildPlayer(buildPlayerOptions);
//     }
// }