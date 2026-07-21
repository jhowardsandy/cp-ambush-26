using System;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace TacticalStrategyGame.Editor
{
    public static class MovementSandboxSceneGenerator
    {
        private const string ScenePath = "Assets/Scenes/MovementSandbox.unity";

        [MenuItem("CP Ambush/Create or Open Movement Sandbox")]
        public static void CreateOrOpen()
        {
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            var root = new GameObject("Movement Sandbox");
            var controllerType = Type.GetType("TacticalStrategyGame.Presentation.Unity.MovementSandboxController, Game.Presentation.Unity");
            if (controllerType == null)
                throw new InvalidOperationException("Movement sandbox presentation assembly is unavailable.");

            root.AddComponent(controllerType);
            System.IO.Directory.CreateDirectory("Assets/Scenes");
            EditorSceneManager.SaveScene(scene, ScenePath);
            EditorBuildSettings.scenes = new[] { new EditorBuildSettingsScene(ScenePath, true) };
            EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
        }

        [MenuItem("CP Ambush/Create or Open Graybox PvE 4v4")]
        public static void CreateOrOpenGrayboxPve()
        {
            const string path = "Assets/Scenes/GrayboxPve4v4.unity";
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            var root = new GameObject("Graybox PvE 4v4");
            var controllerType = Type.GetType("TacticalStrategyGame.Presentation.Unity.GrayboxPveController, Game.Presentation.Unity");
            if (controllerType == null) throw new InvalidOperationException("Graybox PvE presentation assembly is unavailable.");
            root.AddComponent(controllerType);
            System.IO.Directory.CreateDirectory("Assets/Scenes");
            EditorSceneManager.SaveScene(scene, path);
            EditorBuildSettings.scenes = new[] { new EditorBuildSettingsScene(path, true) };
            EditorSceneManager.OpenScene(path, OpenSceneMode.Single);
        }
    }
}
