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
    }
}
