using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using ChungToi.View;
using ChungToi.Game;

namespace ChungToi.View.Editor
{
    /// <summary>
    /// Editor convenience for one-click scene wiring. Two menu items:
    ///
    ///   <b>Tools → Chung Toi → Set up scene</b>      — playable scene: GameController + HUD
    ///   <b>Tools → Chung Toi → Set up preview only</b> — render-only: BoardSetup with a test position
    ///
    /// Both are idempotent — running them twice won't duplicate the setup.
    /// </summary>
    public static class ChungToiSceneSetup
    {
        private const string GameObjectName    = "ChungToi Game";
        private const string HudObjectName     = "ChungToi HUD";
        private const string PreviewObjectName = "ChungToi Setup";

        // ---- Playable scene ----

        [MenuItem("Tools/Chung Toi/Set up scene")]
        public static void SetUpScene()
        {
            var scene = SceneManager.GetActiveScene();
            if (!scene.IsValid())
            {
                Debug.LogWarning("No active scene to set up.");
                return;
            }

            var controllerGo = FindOrCreate(GameObjectName);
            if (controllerGo.GetComponent<GameController>() == null)
                controllerGo.AddComponent<GameController>();

            var hudGo = FindOrCreate(HudObjectName);
            if (hudGo.GetComponent<Canvas>() == null)
                hudGo.AddComponent<Canvas>();
            if (hudGo.GetComponent<HudOverlay>() == null)
                hudGo.AddComponent<HudOverlay>();

            // Wire HUD → controller (best-effort; works whether or not the controller already had Awake run).
            var hud = hudGo.GetComponent<HudOverlay>();
            var controller = controllerGo.GetComponent<GameController>();
            hud.Controller = controller;

            EnsureMainCamera();

            EditorSceneManager.MarkSceneDirty(scene);
            Selection.activeObject = controllerGo;
            EditorGUIUtility.PingObject(controllerGo);
            Debug.Log($"Set up '{GameObjectName}' + '{HudObjectName}'. Press Play to start a placement-phase game.");
        }

        // ---- Render-only preview (no input) ----

        [MenuItem("Tools/Chung Toi/Set up preview only")]
        public static void SetUpPreview()
        {
            var scene = SceneManager.GetActiveScene();
            if (!scene.IsValid())
            {
                Debug.LogWarning("No active scene to set up.");
                return;
            }

            var go = FindOrCreate(PreviewObjectName);
            if (go.GetComponent<BoardSetup>() == null)
                go.AddComponent<BoardSetup>();

            EnsureMainCamera();

            EditorSceneManager.MarkSceneDirty(scene);
            Selection.activeObject = go;
            EditorGUIUtility.PingObject(go);
            Debug.Log($"Set up '{PreviewObjectName}'. Press Play to render the test position.");
        }

        // ---- helpers ----

        private static GameObject FindOrCreate(string name)
        {
            var existing = GameObject.Find(name);
            return existing != null ? existing : new GameObject(name);
        }

        private static void EnsureMainCamera()
        {
            if (Camera.main != null) return;
            var cam = new GameObject("Main Camera").AddComponent<Camera>();
            cam.tag = "MainCamera";
            cam.gameObject.AddComponent<AudioListener>();
        }
    }
}
