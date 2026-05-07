using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using ChungToi.Core;
using ChungToi.View;

namespace ChungToi.Game
{
    /// <summary>
    /// Translates raw mouse + keyboard input into semantic events. Stateless about the game itself —
    /// just emits "the user clicked cell X" or "the user pressed R" so the listener can decide what
    /// to do. The new Input System is used directly via <see cref="Mouse.current"/> and
    /// <see cref="Keyboard.current"/>; no .inputactions asset wiring required.
    ///
    /// Defers to Unity's <see cref="EventSystem"/> when the pointer is over a UI element so menu
    /// clicks don't bleed through to the board.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class InputController : MonoBehaviour
    {
        public Camera RaycastCamera;

        public event Action<Coord> CellLeftClicked;
        public event Action RotatePressed;
        public event Action CancelPressed;

        public Coord? HoveredCell { get; private set; }

        private const float RayLength = 200f;

        private void Update()
        {
            if (RaycastCamera == null) return;

            bool pointerOverUi = EventSystem.current != null && EventSystem.current.IsPointerOverGameObject();

            if (pointerOverUi)
                HoveredCell = null;
            else
                UpdateHover();

            var mouse = Mouse.current;
            if (!pointerOverUi && mouse != null && mouse.leftButton.wasPressedThisFrame && HoveredCell.HasValue)
                CellLeftClicked?.Invoke(HoveredCell.Value);

            var kb = Keyboard.current;
            if (kb != null)
            {
                if (kb.rKey.wasPressedThisFrame) RotatePressed?.Invoke();
                if (kb.escapeKey.wasPressedThisFrame) CancelPressed?.Invoke();
            }
        }

        private void UpdateHover()
        {
            var mouse = Mouse.current;
            if (mouse == null) { HoveredCell = null; return; }

            var screenPos = mouse.position.ReadValue();
            var ray = RaycastCamera.ScreenPointToRay(screenPos);
            if (Physics.Raycast(ray, out var hit, RayLength))
            {
                var cellRef = hit.collider.GetComponent<CellRef>();
                HoveredCell = cellRef != null ? cellRef.Coord : (Coord?)null;
            }
            else
            {
                HoveredCell = null;
            }
        }
    }
}
