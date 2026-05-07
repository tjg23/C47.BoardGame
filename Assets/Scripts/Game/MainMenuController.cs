using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using UnityEngine.InputSystem.UI;
using ChungToi.Core;

namespace ChungToi.Game
{
    /// <summary>
    /// Pre-game main menu. Builds its UI procedurally on Awake — no prefab assets, no TextMeshPro
    /// dependency. Shown when the scene loads; hidden by <see cref="OnStartClicked"/>; re-shown
    /// when <see cref="GameController.MenuRequested"/> fires (M key during play).
    ///
    /// Owns the live <see cref="GameConfig"/> and writes selections into it as the user clicks.
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Canvas))]
    public sealed class MainMenuController : MonoBehaviour
    {
        public GameController Controller;

        private readonly GameConfig _config = GameConfig.Default;

        private Canvas _canvas;
        private GameObject _mainPanel;
        private GameObject _rulesPanel;

        // Radio-group buttons; index lookup matches the enum value layout.
        private Button[] _sizeButtons;     // [0]=3x3, [1]=4x4
        private Button[] _xButtons;        // [0]=Human, [1]=AI
        private Button[] _oButtons;        // [0]=Human, [1]=AI
        private Button[] _difficultyButtons; // [0..4] = Beginner..Expert
        private Text _difficultyLabel;

        // Style constants. All in one place so a future polish pass can re-skin without hunting.
        private static readonly Color BgColor       = new Color(0.10f, 0.10f, 0.12f, 1f);
        private static readonly Color PanelColor    = new Color(0.16f, 0.16f, 0.20f, 1f);
        private static readonly Color BtnIdle       = new Color(0.30f, 0.30f, 0.35f, 1f);
        private static readonly Color BtnSelected   = new Color(0.20f, 0.55f, 0.95f, 1f);
        private static readonly Color BtnHover      = new Color(0.40f, 0.40f, 0.50f, 1f);
        private static readonly Color BtnPressed    = new Color(0.15f, 0.40f, 0.75f, 1f);
        private static readonly Color StartIdle     = new Color(0.20f, 0.65f, 0.30f, 1f);
        private static readonly Color StartHover    = new Color(0.30f, 0.80f, 0.40f, 1f);
        private static readonly Color TextColor     = Color.white;
        private static readonly Color SubduedText   = new Color(0.75f, 0.75f, 0.80f, 1f);

        private static readonly string[] DifficultyNames =
            { "Beginner", "Easy", "Medium", "Hard", "Expert" };

        private static readonly string RulesText =
@"CHUNG TOI

A two-player abstract game on a 3x3 or 4x4 board. Each player has 3 (or 5 on 4x4) octagonal pieces with a 'facing' — Cardinal (aligned with the board sides) or Diagonal (rotated 45°).

PLACEMENT PHASE
Players alternate placing one piece per turn on an empty cell. When you place, you choose its orientation (R toggles between Cardinal and Diagonal). On the 3x3 board, the very first move cannot be the center cell. The phase ends when both players have placed all their pieces.

MOVEMENT PHASE
On your turn, click your piece to select it. Then either:
  • Click the same piece again to ROTATE it in place (toggles orientation).
  • Click a highlighted destination to SLIDE there.

Cardinal pieces slide N/S/E/W; Diagonal pieces slide NE/NW/SE/SW. Slides cannot pass through any piece, and cannot land on an occupied cell. While sliding, you may keep or flip orientation — press R to toggle the preview before clicking.

WIN
Form a complete line of your color in any row, column, or main diagonal.
  • 3x3: any 3-in-a-row.
  • 4x4: any 4-in-a-row (full edge-to-edge or full diagonal only).
Orientation does not affect winning — only ownership. If a player has no legal move on their turn, they lose.

CONTROLS
  • Click          — place / select / move
  • R              — toggle orientation (placement) or preview-rotate (slide)
  • Esc            — deselect
  • N              — new game with current settings
  • M              — return to this menu";

        // ---- lifecycle ----

        private void Awake()
        {
            EnsureEventSystem();

            _canvas = GetComponent<Canvas>();
            _canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            _canvas.sortingOrder = 10; // above the in-game HUD
            if (GetComponent<CanvasScaler>() == null) gameObject.AddComponent<CanvasScaler>();
            if (GetComponent<GraphicRaycaster>() == null) gameObject.AddComponent<GraphicRaycaster>();

            BuildBackground();
            BuildMainPanel();
            BuildRulesPanel();
            ApplyConfigToButtons();

            if (Controller != null)
            {
                Controller.AutoStart = false;
                Controller.MenuRequested += Show;
            }
            // Always start at the menu.
            Show();
        }

        private void OnDestroy()
        {
            if (Controller != null) Controller.MenuRequested -= Show;
        }

        // ---- public API ----

        public void Show()
        {
            _canvas.gameObject.SetActive(true);
            _mainPanel.SetActive(true);
            _rulesPanel.SetActive(false);
            ApplyConfigToButtons();
        }

        public void Hide() => _canvas.gameObject.SetActive(false);

        // ---- UI construction ----

        private void BuildBackground()
        {
            var bg = new GameObject("Background");
            bg.transform.SetParent(transform, false);
            var rt = bg.AddComponent<RectTransform>();
            StretchToParent(rt);
            var img = bg.AddComponent<Image>();
            img.color = BgColor;
        }

        private void BuildMainPanel()
        {
            _mainPanel = new GameObject("MainPanel");
            _mainPanel.transform.SetParent(transform, false);
            var rt = _mainPanel.AddComponent<RectTransform>();
            rt.anchorMin = new Vector2(0.5f, 0.5f);
            rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.sizeDelta = new Vector2(620f, 600f);
            rt.anchoredPosition = Vector2.zero;
            var img = _mainPanel.AddComponent<Image>();
            img.color = PanelColor;

            float y = 260f;
            MakeText(_mainPanel.transform, "Title",   "Chung Toi",                                   new Vector2(0f, y),       40, TextAnchor.MiddleCenter);
            y -= 50f;
            MakeText(_mainPanel.transform, "Sub",     "A board game with minimax AI",                 new Vector2(0f, y),       16, TextAnchor.MiddleCenter, SubduedText);
            y -= 50f;

            // Board size
            MakeText(_mainPanel.transform, "BoardLabel", "Board",                                     new Vector2(-260f, y),    20, TextAnchor.MiddleLeft);
            _sizeButtons = new[]
            {
                MakeRadioButton(_mainPanel.transform, "3x3", new Vector2(-90f, y), 100f, () => SetSize(BoardSize.ThreeByThree)),
                MakeRadioButton(_mainPanel.transform, "4x4", new Vector2( 30f, y), 100f, () => SetSize(BoardSize.FourByFour)),
            };
            y -= 60f;

            // X player
            MakeText(_mainPanel.transform, "XLabel",  "Player X",                                     new Vector2(-260f, y),    20, TextAnchor.MiddleLeft);
            _xButtons = new[]
            {
                MakeRadioButton(_mainPanel.transform, "Human", new Vector2(-90f, y), 100f, () => SetX(GameController.SideController.Human)),
                MakeRadioButton(_mainPanel.transform, "AI",    new Vector2( 30f, y), 100f, () => SetX(GameController.SideController.AI)),
            };
            y -= 60f;

            // O player
            MakeText(_mainPanel.transform, "OLabel",  "Player O",                                     new Vector2(-260f, y),    20, TextAnchor.MiddleLeft);
            _oButtons = new[]
            {
                MakeRadioButton(_mainPanel.transform, "Human", new Vector2(-90f, y), 100f, () => SetO(GameController.SideController.Human)),
                MakeRadioButton(_mainPanel.transform, "AI",    new Vector2( 30f, y), 100f, () => SetO(GameController.SideController.AI)),
            };
            y -= 60f;

            // AI difficulty (5 levels)
            MakeText(_mainPanel.transform, "DiffLabel", "AI difficulty",                              new Vector2(-260f, y),    20, TextAnchor.MiddleLeft);
            _difficultyButtons = new Button[5];
            for (int i = 0; i < 5; i++)
            {
                int captured = i; // closure
                _difficultyButtons[i] = MakeRadioButton(_mainPanel.transform, (i + 1).ToString(),
                    new Vector2(-110f + i * 60f, y), 50f,
                    () => SetDifficulty((AIDifficulty)(captured + 1)));
            }
            y -= 32f;
            _difficultyLabel = MakeText(_mainPanel.transform, "DiffName", "Medium",                   new Vector2(0f, y),       16, TextAnchor.MiddleCenter, SubduedText);
            y -= 60f;

            // Action row: Rules + Start
            MakeButton(_mainPanel.transform, "RulesBtn", "Show rules", new Vector2(-150f, y), 200f, OnRulesClicked, BtnIdle);
            MakeButton(_mainPanel.transform, "StartBtn", "Start game", new Vector2( 110f, y), 240f, OnStartClicked, StartIdle, hover: StartHover);
        }

        private void BuildRulesPanel()
        {
            _rulesPanel = new GameObject("RulesPanel");
            _rulesPanel.transform.SetParent(transform, false);
            var rt = _rulesPanel.AddComponent<RectTransform>();
            rt.anchorMin = new Vector2(0.5f, 0.5f);
            rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.sizeDelta = new Vector2(720f, 640f);
            rt.anchoredPosition = Vector2.zero;
            var img = _rulesPanel.AddComponent<Image>();
            img.color = PanelColor;

            MakeText(_rulesPanel.transform, "RulesTitle", "Rules", new Vector2(0f, 280f), 32, TextAnchor.MiddleCenter);

            // Body — single big text block. No scrolling for v1; the panel is sized to fit.
            var body = new GameObject("RulesBody");
            body.transform.SetParent(_rulesPanel.transform, false);
            var brt = body.AddComponent<RectTransform>();
            brt.anchorMin = new Vector2(0.5f, 0.5f);
            brt.anchorMax = new Vector2(0.5f, 0.5f);
            brt.pivot = new Vector2(0.5f, 0.5f);
            brt.sizeDelta = new Vector2(660f, 500f);
            brt.anchoredPosition = new Vector2(0f, -10f);
            var bt = body.AddComponent<Text>();
            bt.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            bt.text = RulesText;
            bt.fontSize = 14;
            bt.color = TextColor;
            bt.alignment = TextAnchor.UpperLeft;
            bt.horizontalOverflow = HorizontalWrapMode.Wrap;
            bt.verticalOverflow = VerticalWrapMode.Truncate;

            MakeButton(_rulesPanel.transform, "BackBtn", "Back", new Vector2(0f, -290f), 200f, OnRulesBackClicked, BtnIdle);

            _rulesPanel.SetActive(false);
        }

        // ---- handlers ----

        private void SetSize(BoardSize s)         { _config.Size = s;       ApplyConfigToButtons(); }
        private void SetX(GameController.SideController c)  { _config.XControl = c; ApplyConfigToButtons(); }
        private void SetO(GameController.SideController c)  { _config.OControl = c; ApplyConfigToButtons(); }

        private void SetDifficulty(AIDifficulty d)
        {
            _config.Difficulty = d;
            ApplyConfigToButtons();
        }

        private void ApplyConfigToButtons()
        {
            SetRadio(_sizeButtons, _config.Size == BoardSize.ThreeByThree ? 0 : 1);
            SetRadio(_xButtons,    _config.XControl == GameController.SideController.Human ? 0 : 1);
            SetRadio(_oButtons,    _config.OControl == GameController.SideController.Human ? 0 : 1);
            SetRadio(_difficultyButtons, (int)_config.Difficulty - 1);
            if (_difficultyLabel != null)
                _difficultyLabel.text = DifficultyNames[(int)_config.Difficulty - 1];
        }

        private void OnRulesClicked()
        {
            _mainPanel.SetActive(false);
            _rulesPanel.SetActive(true);
        }

        private void OnRulesBackClicked()
        {
            _rulesPanel.SetActive(false);
            _mainPanel.SetActive(true);
        }

        private void OnStartClicked()
        {
            if (Controller == null)
            {
                Debug.LogError("MainMenuController has no GameController to start.");
                return;
            }
            Hide();
            Controller.StartWithConfig(_config.Clone());
        }

        // ---- helpers ----

        private static void StretchToParent(RectTransform rt)
        {
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
        }

        private static Text MakeText(Transform parent, string name, string text, Vector2 pos, int size, TextAnchor align, Color? color = null)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            var rt = go.AddComponent<RectTransform>();
            rt.anchorMin = new Vector2(0.5f, 0.5f);
            rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = pos;
            rt.sizeDelta = new Vector2(560f, 50f);
            var t = go.AddComponent<Text>();
            t.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            t.text = text;
            t.fontSize = size;
            t.color = color ?? TextColor;
            t.alignment = align;
            t.horizontalOverflow = HorizontalWrapMode.Overflow;
            return t;
        }

        private static Button MakeButton(Transform parent, string name, string label, Vector2 pos, float width, Action onClick, Color idle, Color? hover = null)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            var rt = go.AddComponent<RectTransform>();
            rt.anchorMin = new Vector2(0.5f, 0.5f);
            rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = pos;
            rt.sizeDelta = new Vector2(width, 44f);

            var img = go.AddComponent<Image>();
            img.color = idle;

            var btn = go.AddComponent<Button>();
            var colors = btn.colors;
            colors.normalColor      = idle;
            colors.highlightedColor = hover ?? BtnHover;
            colors.pressedColor     = BtnPressed;
            colors.selectedColor    = idle;
            colors.disabledColor    = idle * 0.5f;
            btn.colors = colors;
            btn.onClick.AddListener(() => onClick());

            // Child label
            var labelGo = new GameObject("Label");
            labelGo.transform.SetParent(go.transform, false);
            var lrt = labelGo.AddComponent<RectTransform>();
            StretchToParent(lrt);
            var t = labelGo.AddComponent<Text>();
            t.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            t.text = label;
            t.fontSize = 18;
            t.color = TextColor;
            t.alignment = TextAnchor.MiddleCenter;
            t.horizontalOverflow = HorizontalWrapMode.Overflow;

            return btn;
        }

        private static Button MakeRadioButton(Transform parent, string label, Vector2 pos, float width, Action onClick)
        {
            return MakeButton(parent, $"Radio_{label}", label, pos, width, onClick, BtnIdle);
        }

        private static void SetRadio(Button[] buttons, int activeIndex)
        {
            if (buttons == null) return;
            for (int i = 0; i < buttons.Length; i++)
            {
                if (buttons[i] == null) continue;
                var img = buttons[i].GetComponent<Image>();
                var c = buttons[i].colors;
                if (i == activeIndex)
                {
                    img.color = BtnSelected;
                    c.normalColor = BtnSelected;
                    c.selectedColor = BtnSelected;
                }
                else
                {
                    img.color = BtnIdle;
                    c.normalColor = BtnIdle;
                    c.selectedColor = BtnIdle;
                }
                buttons[i].colors = c;
            }
        }

        private static void EnsureEventSystem()
        {
            if (EventSystem.current != null) return;
            var go = new GameObject("EventSystem");
            go.AddComponent<EventSystem>();
            // New Input System requires its own UI module; the legacy StandaloneInputModule
            // will throw warnings under the new system.
            go.AddComponent<InputSystemUIInputModule>();
        }
    }
}
