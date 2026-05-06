using UnityEngine;
using UnityEngine.UI;
using ChungToi.Core;

namespace ChungToi.Game
{
    /// <summary>
    /// Minimal screen-space overlay: turn label, orientation label, winner banner. Builds itself
    /// procedurally from a single <see cref="Canvas"/> on this GameObject — no prefab assets, no
    /// TextMeshPro import required (uses legacy <see cref="Text"/> with the built-in
    /// LegacyRuntime font).
    ///
    /// Polls <see cref="GameController.State"/> and <see cref="GameController.CurrentHuman"/> every
    /// frame; never modifies the controller. If you'd rather drive updates with events, swap the
    /// Update body for explicit Refresh() calls — the API is small.
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Canvas))]
    public sealed class HudOverlay : MonoBehaviour
    {
        public GameController Controller;

        private Text _turnLabel;
        private Text _orientLabel;
        private Text _winnerLabel;

        private void Awake()
        {
            var canvas = GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            if (GetComponent<CanvasScaler>() == null) gameObject.AddComponent<CanvasScaler>();
            if (GetComponent<GraphicRaycaster>() == null) gameObject.AddComponent<GraphicRaycaster>();

            _turnLabel    = MakeLabel("Turn",    new Vector2(0f, 1f), new Vector2(20f, -20f), 28, TextAnchor.UpperLeft);
            _orientLabel  = MakeLabel("Orient",  new Vector2(0f, 1f), new Vector2(20f, -56f), 22, TextAnchor.UpperLeft);
            _winnerLabel  = MakeLabel("Winner",  new Vector2(0.5f, 0.5f), Vector2.zero,        56, TextAnchor.MiddleCenter);
            _winnerLabel.gameObject.SetActive(false);
        }

        private void Update()
        {
            if (Controller == null || Controller.State == null) return;
            var state = Controller.State;

            if (state.Phase == GamePhase.GameOver)
            {
                _turnLabel.text = "";
                _orientLabel.text = "";
                _winnerLabel.gameObject.SetActive(true);
                _winnerLabel.text = state.Winner == Player.None
                    ? "Draw"
                    : $"{state.Winner} wins!";
                return;
            }

            _winnerLabel.gameObject.SetActive(false);
            _turnLabel.text = $"Turn: {state.ToMove}";

            // Orientation indicator: human only, placement only (Step 4 scope).
            var human = Controller.CurrentHuman;
            if (human != null && state.Phase == GamePhase.Placement)
                _orientLabel.text = $"Place orientation: {human.CurrentOrientation}  (R to flip)";
            else
                _orientLabel.text = "";
        }

        // ---- helpers ----

        private Text MakeLabel(string name, Vector2 anchor, Vector2 anchoredPos, int fontSize, TextAnchor alignment)
        {
            var go = new GameObject(name + "Label");
            go.transform.SetParent(transform, false);

            var rt = go.AddComponent<RectTransform>();
            rt.anchorMin = anchor;
            rt.anchorMax = anchor;
            rt.pivot = anchor;
            rt.anchoredPosition = anchoredPos;
            rt.sizeDelta = new Vector2(600f, 80f);

            var text = go.AddComponent<Text>();
            text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            text.fontSize = fontSize;
            text.color = Color.white;
            text.alignment = alignment;
            text.horizontalOverflow = HorizontalWrapMode.Overflow;
            text.verticalOverflow = VerticalWrapMode.Overflow;
            return text;
        }
    }
}
