using System;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Controls;
using UnityEngine.InputSystem.LowLevel;

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
        private bool rawEventsSubscribed;
        private bool bufferedMousePress;
        private bool bufferedMouseRelease;
        private Vector2 bufferedMousePressPosition;
        private Vector2 bufferedMouseReleasePosition;
        private bool bufferedTouchPress;
        private bool bufferedTouchRelease;
        private int bufferedTouchId;
        private Vector2 bufferedTouchPressPosition;
        private Vector2 bufferedTouchReleasePosition;

        public event Action<Vector2> PointerPressed;
        public event Action<Vector2> PointerMoved;
        public event Action<Vector2> PointerReleased;

        private void OnEnable()
        {
            ResetLock();
            ClearBufferedTransitions();
            if (!rawEventsSubscribed)
            {
                InputSystem.onEvent += OnRawInputEvent;
                rawEventsSubscribed = true;
            }
        }

        private void Update()
        {
            try
            {
                if (lockedPointer == LockedPointer.None)
                {
                    if (!TryBeginBufferedPointer()) TryBeginPointer();
                    return;
                }

                if (lockedPointer == LockedPointer.Touch) UpdateLockedTouch();
                else UpdateLockedMouse();
            }
            finally
            {
                ClearBufferedTransitions();
            }
        }

        private void OnDisable()
        {
            if (rawEventsSubscribed)
            {
                InputSystem.onEvent -= OnRawInputEvent;
                rawEventsSubscribed = false;
            }
            ReleaseLockedPointer(lastPosition);
            ClearBufferedTransitions();
        }

        private void OnRawInputEvent(InputEventPtr eventPtr, InputDevice device)
        {
            var mouse = device as Mouse;
            if (mouse != null)
            {
                float pressed;
                if (!mouse.leftButton.ReadValueFromEvent(eventPtr, out pressed)) return;
                Vector2 position;
                if (!mouse.position.ReadValueFromEvent(eventPtr, out position)) position = mouse.position.ReadValue();
                if (pressed > 0f)
                {
                    if (!bufferedMousePress)
                    {
                        bufferedMousePress = true;
                        bufferedMousePressPosition = position;
                    }
                }
                else if (bufferedMousePress)
                {
                    bufferedMouseRelease = true;
                    bufferedMouseReleasePosition = position;
                }
                return;
            }

            var touchscreen = device as Touchscreen;
            if (touchscreen == null) return;

            // Touchscreen consumes platform TouchState events through a special
            // remapping path, so its child controls cannot be read directly from
            // those events. Capture the original contact before that remapping.
            if (eventPtr.IsA<StateEvent>())
            {
                try
                {
                    var state = StateEvent.GetState<TouchState>(eventPtr);
                    BufferTouchTransition(state.touchId, state.phase, state.position);
                    return;
                }
                catch (InvalidOperationException)
                {
                    // A full TouchscreenState event can still be read through its
                    // child controls below.
                }
            }

            var touches = touchscreen.touches;
            for (var index = 0; index < touches.Count; index++)
            {
                var touch = touches[index];
                int touchId;
                float pressed;
                if (!touch.touchId.ReadValueFromEvent(eventPtr, out touchId) || touchId == 0 ||
                    !touch.press.ReadValueFromEvent(eventPtr, out pressed)) continue;
                Vector2 position;
                if (!touch.position.ReadValueFromEvent(eventPtr, out position)) position = touch.position.ReadValue();
                if (pressed > 0f)
                {
                    if (!bufferedTouchPress)
                    {
                        bufferedTouchPress = true;
                        bufferedTouchId = touchId;
                        bufferedTouchPressPosition = position;
                    }
                }
                else if (bufferedTouchPress && bufferedTouchId == touchId)
                {
                    bufferedTouchRelease = true;
                    bufferedTouchReleasePosition = position;
                }
            }
        }

        private void BufferTouchTransition(int touchId, UnityEngine.InputSystem.TouchPhase phase, Vector2 position)
        {
            if (touchId == 0) return;
            if (phase == UnityEngine.InputSystem.TouchPhase.Began)
            {
                if (bufferedTouchPress) return;
                bufferedTouchPress = true;
                bufferedTouchId = touchId;
                bufferedTouchPressPosition = position;
                return;
            }

            if ((phase == UnityEngine.InputSystem.TouchPhase.Ended ||
                 phase == UnityEngine.InputSystem.TouchPhase.Canceled) &&
                bufferedTouchPress && bufferedTouchId == touchId)
            {
                bufferedTouchRelease = true;
                bufferedTouchReleasePosition = position;
            }
        }

        private bool TryBeginBufferedPointer()
        {
            if (bufferedTouchPress)
            {
                lockedPointer = LockedPointer.Touch;
                lockedTouchId = bufferedTouchId;
                lastPosition = bufferedTouchPressPosition;
                var touchPressed = PointerPressed;
                if (touchPressed != null) touchPressed(bufferedTouchPressPosition);
                if (bufferedTouchRelease) ReleaseLockedPointer(bufferedTouchReleasePosition);
                return true;
            }

            if (!bufferedMousePress || !bufferedMouseRelease) return false;
            lockedPointer = LockedPointer.Mouse;
            lastPosition = bufferedMousePressPosition;
            var mousePressed = PointerPressed;
            if (mousePressed != null) mousePressed(bufferedMousePressPosition);
            ReleaseLockedPointer(bufferedMouseReleasePosition);
            return true;
        }

        private void ClearBufferedTransitions()
        {
            bufferedMousePress = false;
            bufferedMouseRelease = false;
            bufferedMousePressPosition = default(Vector2);
            bufferedMouseReleasePosition = default(Vector2);
            bufferedTouchPress = false;
            bufferedTouchRelease = false;
            bufferedTouchId = 0;
            bufferedTouchPressPosition = default(Vector2);
            bufferedTouchReleasePosition = default(Vector2);
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
                if (primaryTouch.press.wasReleasedThisFrame || !primaryTouch.press.isPressed)
                    ReleaseLockedPointer(primaryTouch.position.ReadValue());
                return;
            }

            var mouse = Mouse.current;
            if (mouse == null || !mouse.leftButton.wasPressedThisFrame) return;
            lockedPointer = LockedPointer.Mouse;
            lastPosition = mouse.position.ReadValue();
            var mouseHandler = PointerPressed;
            if (mouseHandler != null) mouseHandler(lastPosition);
            if (mouse.leftButton.wasReleasedThisFrame || !mouse.leftButton.isPressed)
                ReleaseLockedPointer(mouse.position.ReadValue());
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
            return touch != null &&
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
