using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using ChungToi.View;

namespace ChungToi.View.Editor
{
	/// <summary>
	/// Editor convenience: <b>Tools → Chung Toi → Set up scene</b>.
	/// Adds (or finds) a "ChungToi Setup" GameObject in the active scene with a
	/// <see cref="BoardSetup"/> component, then marks the scene dirty so you can save.
	/// Idempotent — running it twice won't duplicate the setup.
	/// </summary>
	public static class ChungToiSceneSetup
	{
		private const string SetupObjectName = "ChungToi Setup";

		[MenuItem("Tools/Chung Toi/Set up scene")]
		public static void SetUpScene()
		{
			var scene = SceneManager.GetActiveScene();
			if (!scene.IsValid())
			{
				Debug.LogWarning("No active scene to set up.");
				return;
			}

			var existing = GameObject.Find(SetupObjectName);
			if (existing != null && existing.GetComponent<BoardSetup>() != null)
			{
				Selection.activeObject = existing;
				EditorGUIUtility.PingObject(existing);
				Debug.Log($"'{SetupObjectName}' already in scene; selected it for you.");
				return;
			}

			var go = existing != null ? existing : new GameObject(SetupObjectName);
			if (go.GetComponent<BoardSetup>() == null)
				go.AddComponent<BoardSetup>();

			// Make sure something we render is visible: ensure a MainCamera exists.
			if (Camera.main == null)
			{
				var cam = new GameObject("Main Camera").AddComponent<Camera>();
				cam.tag = "MainCamera";
				cam.gameObject.AddComponent<AudioListener>();
			}

			EditorSceneManager.MarkSceneDirty(scene);
			Selection.activeObject = go;
			EditorGUIUtility.PingObject(go);
			Debug.Log($"Added '{SetupObjectName}' with BoardSetup. Press Play to render the test position.");
		}
	}
}
