using UnityEngine;
using UnityEngine.UI;
using ChungToi.Core;

namespace ChungToi.Game
{
    /// <summary>
    /// Minimal screen-space overlay: turn label, status line (orientation / AI thinking), winner
    /// banner, and a stats line at the bottom showing the most recent AI search counters.
    ///
    /// Builds itself procedurally from a single <see cref="Canvas"/> on this GameObject — no prefab
    /// assets, no TextMeshPro import required (uses legacy <see cref="Text"/> with the built-in
    /// LegacyRuntime font).
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Canvas))]
    public sealed class HudOverlay : MonoBehaviour
    {
        public GameController Controller;

        private Text _turnLabel;
        private Text _statusLabel;
        private Text _statsLabel;
        private Text _winnerLabel;

        private void Awake()
        {
            var canvas = GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            if (GetComponent<CanvasScaler>() == null) gameObject.AddComponent<CanvasScaler>();
            if (GetComponent<GraphicRaycaster>() == null) gameObject.AddComponent<GraphicRaycaster>();

            _turnLabel   = MakeLabel("Turn",   new Vector2(0f, 1f),   new Vector2( 20f, -20f),  28, TextAnchor.UpperLeft);
            _statusLabel = MakeLabel("Status", new Vector2(0f, 1f),   new Vector2( 20f, -56f),  20, TextAnchor.UpperLeft);
            _statsLabel  = MakeLabel("Stats",  new Vector2(0f, 0f),   new Vector2( 20f,  20f),  16, TextAnchor.LowerLeft);
            _winnerLabel = MakeLabel("Winner", new Vector2(0.5f,0.5f), Vector2.zero,             56, TextAnchor.MiddleCenter);
            _winnerLabel.gameObject.SetActive(false);
        }

        private void Update()
        {
            if (Controller == null || Controller.State == null) return;
            var state = Controller.State;

            // Winner banner takes over the center of the screen at game over.
            if (state.Phase == GamePhase.GameOver)
            {
                _turnLabel.text = "";
                _statusLabel.text = "Press N for new game";
                _winnerLabel.gameObject.SetActive(true);
                _winnerLabel.text = state.Winner == Player.None
                    ? "Draw"
                    : $"{state.Winner} wins!";
                _statsLabel.text = ComposeStatsLine(Controller);
                return;
            }

            _winnerLabel.gameObject.SetActive(false);
            _turnLabel.text = $"Turn: {state.ToMove}{TurnSuffix(Controller)}";
            _statusLabel.text = ComposeStatusLine(Controller);
            _statsLabel.text = ComposeStatsLine(Controller);
        }

        private static string TurnSuffix(GameController c)
        {
            if (c.CurrentAI != null) return c.CurrentAI.IsThinking ? "  (AI thinking…)" : "  (AI)";
            return "  (you)";
        }

        private static string ComposeStatusLine(GameController c)
        {
            var human = c.CurrentHuman;
            if (human == null) return ""; // AI's turn — Turn label already says so.

            var state = c.State;
            if (state.Phase == GamePhase.Placement)
                return $"Place orientation: {human.CurrentOrientation}  (R to flip)";

            if (!human.SelectedPiece.HasValue)
                return "Click your piece to select  ·  Esc to cancel  ·  R to rotate during slide";

            return $"Slide orientation: {human.SlideOrientation}  (R to flip, click own piece to rotate-in-place, Esc to deselect)";
        }

        private static string ComposeStatsLine(GameController c)
        {
            var stats = c.LastAIStats;
            if (stats == null) return "N: new game   ·   M: main menu";
            return $"Last AI [{c.LastAILabel}]: {stats}   ·   N: new game   ·   M: main menu";
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
            rt.sizeDelta = new Vector2(900f, 80f);

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
