using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

namespace MidnightHour
{
    /// <summary>Every key the game listens to. Order must match the tables in InputCompat.</summary>
    public enum GameKey
    {
        Tower1, Tower2, Tower3, Choice4, Cancel, Sell, StartNight, Pause, Speed,
        AimMode, AimLines, Help, Restart, PanUp, PanDown, PanLeft, PanRight
    }

    /// <summary>
    /// Works with BOTH the new Input System (Unity 6 default) and the old Input Manager,
    /// so the project runs no matter what "Active Input Handling" is set to.
    /// </summary>
    public static class InputCompat
    {
#if ENABLE_INPUT_SYSTEM
        static readonly Key[][] Table =
        {
            new[] { Key.Digit1, Key.Numpad1 },     // Tower1
            new[] { Key.Digit2, Key.Numpad2 },     // Tower2
            new[] { Key.Digit3, Key.Numpad3 },     // Tower3
            new[] { Key.Digit4, Key.Numpad4 },     // Choice4
            new[] { Key.Escape },                  // Cancel
            new[] { Key.X, Key.Delete },           // Sell
            new[] { Key.Enter, Key.NumpadEnter },  // StartNight
            new[] { Key.Space },                   // Pause
            new[] { Key.F },                       // Speed
            new[] { Key.P },                       // AimMode
            new[] { Key.L },                       // AimLines
            new[] { Key.H },                       // Help
            new[] { Key.R },                       // Restart
            new[] { Key.W, Key.UpArrow },          // PanUp
            new[] { Key.S, Key.DownArrow },        // PanDown
            new[] { Key.A, Key.LeftArrow },        // PanLeft
            new[] { Key.D, Key.RightArrow },       // PanRight
        };

        public static bool Down(GameKey k)
        {
            var kb = Keyboard.current;
            if (kb == null) return false;
            foreach (var key in Table[(int)k]) if (kb[key].wasPressedThisFrame) return true;
            return false;
        }

        public static bool Held(GameKey k)
        {
            var kb = Keyboard.current;
            if (kb == null) return false;
            foreach (var key in Table[(int)k]) if (kb[key].isPressed) return true;
            return false;
        }

        public static Vector2 MousePosition
        {
            get { return Mouse.current != null ? Mouse.current.position.ReadValue() : Vector2.zero; }
        }

        public static bool LeftClickDown
        {
            get { return Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame; }
        }

        public static bool RightClickDown
        {
            get { return Mouse.current != null && Mouse.current.rightButton.wasPressedThisFrame; }
        }

        /// <summary>Scroll wheel in "notches" (about +-1 per click).</summary>
        public static float Scroll
        {
            get
            {
                if (Mouse.current == null) return 0f;
                float y = Mouse.current.scroll.ReadValue().y;
                return Mathf.Abs(y) >= 20f ? y / 120f : Mathf.Clamp(y, -3f, 3f);
            }
        }
#else
        static readonly KeyCode[][] Table =
        {
            new[] { KeyCode.Alpha1, KeyCode.Keypad1 },
            new[] { KeyCode.Alpha2, KeyCode.Keypad2 },
            new[] { KeyCode.Alpha3, KeyCode.Keypad3 },
            new[] { KeyCode.Alpha4, KeyCode.Keypad4 },
            new[] { KeyCode.Escape },
            new[] { KeyCode.X, KeyCode.Delete },
            new[] { KeyCode.Return, KeyCode.KeypadEnter },
            new[] { KeyCode.Space },
            new[] { KeyCode.F },
            new[] { KeyCode.P },
            new[] { KeyCode.L },
            new[] { KeyCode.H },
            new[] { KeyCode.R },
            new[] { KeyCode.W, KeyCode.UpArrow },
            new[] { KeyCode.S, KeyCode.DownArrow },
            new[] { KeyCode.A, KeyCode.LeftArrow },
            new[] { KeyCode.D, KeyCode.RightArrow },
        };

        public static bool Down(GameKey k)
        {
            foreach (var key in Table[(int)k]) if (Input.GetKeyDown(key)) return true;
            return false;
        }

        public static bool Held(GameKey k)
        {
            foreach (var key in Table[(int)k]) if (Input.GetKey(key)) return true;
            return false;
        }

        public static Vector2 MousePosition { get { return Input.mousePosition; } }
        public static bool LeftClickDown { get { return Input.GetMouseButtonDown(0); } }
        public static bool RightClickDown { get { return Input.GetMouseButtonDown(1); } }
        public static float Scroll { get { return Input.mouseScrollDelta.y; } }
#endif
    }

    /// <summary>Unity 6 renamed Rigidbody.velocity to linearVelocity. This hides the difference.</summary>
    public static class RigidbodyCompat
    {
        public static Vector3 GetVelocity(this Rigidbody rb)
        {
#if UNITY_6000_0_OR_NEWER
            return rb.linearVelocity;
#else
            return rb.velocity;
#endif
        }

        public static void SetVelocity(this Rigidbody rb, Vector3 v)
        {
            if (rb.isKinematic) return;
#if UNITY_6000_0_OR_NEWER
            rb.linearVelocity = v;
#else
            rb.velocity = v;
#endif
        }
    }

    public static class FindCompat
    {
        public static T First<T>() where T : Object
        {
#if UNITY_2023_1_OR_NEWER
            return Object.FindFirstObjectByType<T>();
#else
            return Object.FindObjectOfType<T>();
#endif
        }
    }
}
