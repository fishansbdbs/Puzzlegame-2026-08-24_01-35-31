using System;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Controls;

namespace PuzzleGame.Unity.Input
{
    public interface IBoardPointerSource
    {
        event Action<Vector2> PointerPressed;
        event Action<Vector2> PointerMoved;
        event Action<Vector2> PointerReleased;
    }

    /// <summary>
    /// Polls raw Input System devices and exposes a single locked primary pointer.
    /// Disabling during a drag releases once at the last observed position.
    /// </summary>
    public sealed class BoardPointerInput : MonoBehaviour, IBoardPointerSource
    {
        private enum LockedPointer
        {
            None,
            Mouse,
            Touch
        }

        private LockedPointer lockedPointer;
        private int lockedTouchId;
        private Vector2 lastPosition;

        public event Action<Vector2> PointerPressed;
        public event Action<Vector2> PointerMoved;
        public event Action<Vector2> PointerReleased;

        private void OnEnable()
        {
            ResetLock();
        }

        private void Update()
        {
            if (lockedPointer == LockedPointer.None)
            {
                TryBeginPointer();
                return;
            }

            if (lockedPointer == LockedPointer.Touch) UpdateLockedTouch();
            else UpdateLockedMouse();
        }

        private void OnDisable()
        {
            ReleaseLockedPointer(lastPosition);
        }

        private void TryBeginPointer()
        {
            TouchControl primaryTouch;
            if (TryFindNewPrimaryTouch(out primaryTouch))
            {
                lockedPointer = LockedPointer.Touch;
                lockedTouchId = primaryTouch.touchId.ReadValue();
                lastPosition = primaryTouch.position.ReadValue();
                var handler = PointerPressed;
                if (handler != null) handler(lastPosition);
                return;
            }

            var mouse = Mouse.current;
            if (mouse == null || !mouse.leftButton.wasPressedThisFrame || !mouse.leftButton.isPressed) return;
            lockedPointer = LockedPointer.Mouse;
            lastPosition = mouse.position.ReadValue();
            var mouseHandler = PointerPressed;
            if (mouseHandler != null) mouseHandler(lastPosition);
        }

        private static bool TryFindNewPrimaryTouch(out TouchControl result)
        {
            var touchscreen = Touchscreen.current;
            if (touchscreen != null)
            {
                var primary = touchscreen.primaryTouch;
                if (IsNewPress(primary))
                {
                    result = primary;
                    return true;
                }

                // InputTestFixture and some touch backends expose the first physical
                // contact before the synthetic primaryTouch control is updated.
                var touches = touchscreen.touches;
                for (var index = 0; index < touches.Count; index++)
                {
                    if (IsNewPress(touches[index]))
                    {
                        result = touches[index];
                        return true;
                    }
                }
            }

            result = null;
            return false;
        }

        private static bool IsNewPress(TouchControl touch)
        {
            return touch != null && touch.press.isPressed &&
                   (touch.press.wasPressedThisFrame || touch.phase.ReadValue() == UnityEngine.InputSystem.TouchPhase.Began);
        }

        private void UpdateLockedMouse()
        {
            var mouse = Mouse.current;
            if (mouse == null)
            {
                ReleaseLockedPointer(lastPosition);
                return;
            }

            var position = mouse.position.ReadValue();
            EmitMoveIfChanged(position);
            if (mouse.leftButton.wasReleasedThisFrame || !mouse.leftButton.isPressed)
                ReleaseLockedPointer(position);
        }

        private void UpdateLockedTouch()
        {
            TouchControl touch;
            if (!TryFindLockedTouch(out touch))
            {
                ReleaseLockedPointer(lastPosition);
                return;
            }

            var position = touch.position.ReadValue();
            EmitMoveIfChanged(position);
            if (touch.press.wasReleasedThisFrame || !touch.press.isPressed)
                ReleaseLockedPointer(position);
        }

        private bool TryFindLockedTouch(out TouchControl result)
        {
            var touchscreen = Touchscreen.current;
            if (touchscreen != null)
            {
                var touches = touchscreen.touches;
                for (var index = 0; index < touches.Count; index++)
                {
                    var touch = touches[index];
                    if (touch.touchId.ReadValue() == lockedTouchId)
                    {
                        result = touch;
                        return true;
                    }
                }
            }

            result = null;
            return false;
        }

        private void EmitMoveIfChanged(Vector2 position)
        {
            if (position == lastPosition) return;
            lastPosition = position;
            var handler = PointerMoved;
            if (handler != null) handler(position);
        }

        private void ReleaseLockedPointer(Vector2 position)
        {
            if (lockedPointer == LockedPointer.None) return;
            ResetLock();
            var handler = PointerReleased;
            if (handler != null) handler(position);
        }

        private void ResetLock()
        {
            lockedPointer = LockedPointer.None;
            lockedTouchId = 0;
            lastPosition = default(Vector2);
        }
    }
}
