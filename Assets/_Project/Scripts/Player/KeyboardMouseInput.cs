using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Controls;

namespace YazLab.Player
{
    /// <summary>Input System üzerinden klavye ve fare girdisi.</summary>
    public class KeyboardMouseInput : MonoBehaviour, IPlayerInput
    {
        [SerializeField] private float mouseSensitivity = 0.12f;

        public bool Enabled { get; set; } = true;

        public Vector2 Move
        {
            get
            {
                var keyboard = Keyboard.current;
                if (!Enabled || keyboard == null) return Vector2.zero;

                var move = new Vector2(
                    Axis(keyboard.dKey, keyboard.aKey) + Axis(keyboard.rightArrowKey, keyboard.leftArrowKey),
                    Axis(keyboard.wKey, keyboard.sKey) + Axis(keyboard.upArrowKey, keyboard.downArrowKey));
                return Vector2.ClampMagnitude(move, 1f);
            }
        }

        public Vector2 Look
        {
            get
            {
                var mouse = Mouse.current;
                if (!Enabled || mouse == null || Cursor.lockState != CursorLockMode.Locked) return Vector2.zero;
                return mouse.delta.ReadValue() * mouseSensitivity;
            }
        }

        public bool SprintHeld => Enabled && Keyboard.current != null && Keyboard.current.leftShiftKey.isPressed;
        public bool CommandPressed => Pressed(Keyboard.current?.fKey);
        public bool FollowTogglePressed => Pressed(Keyboard.current?.gKey);
        public bool InteractPressed => Pressed(Keyboard.current?.eKey);

        private bool Pressed(KeyControl key) => Enabled && key != null && key.wasPressedThisFrame;

        private static float Axis(KeyControl positive, KeyControl negative)
        {
            return (positive.isPressed ? 1f : 0f) - (negative.isPressed ? 1f : 0f);
        }
    }
}
