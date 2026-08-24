using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using PuzzleGame.Unity.Input;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.TestTools;

namespace PuzzleGame.Tests.PlayMode.Input
{
    public sealed class BoardPointerInputTests : InputTestFixture
    {
        private GameObject root;
        private BoardPointerInput input;

        public override void Setup()
        {
            base.Setup();
            root = new GameObject("BoardPointerInputTests");
            input = root.AddComponent<BoardPointerInput>();
        }

        public override void TearDown()
        {
            if (root != null) Object.DestroyImmediate(root);
            base.TearDown();
        }

        [UnityTest]
        public IEnumerator Mouse_press_move_and_release_emit_one_locked_drag()
        {
            var mouse = InputSystem.AddDevice<Mouse>();
            var pressed = new List<Vector2>();
            var moved = new List<Vector2>();
            var released = new List<Vector2>();
            input.PointerPressed += pressed.Add;
            input.PointerMoved += moved.Add;
            input.PointerReleased += released.Add;

            Move(mouse.position, new Vector2(10f, 20f));
            Press(mouse.leftButton);
            yield return null;
            Move(mouse.position, new Vector2(30f, 40f));
            yield return null;
            Release(mouse.leftButton);
            yield return null;

            CollectionAssert.AreEqual(new[] { new Vector2(10f, 20f) }, pressed);
            CollectionAssert.AreEqual(new[] { new Vector2(30f, 40f) }, moved);
            CollectionAssert.AreEqual(new[] { new Vector2(30f, 40f) }, released);
        }

        [UnityTest]
        public IEnumerator Primary_touch_is_preferred_when_mouse_and_touch_press_together()
        {
            var mouse = InputSystem.AddDevice<Mouse>();
            InputSystem.AddDevice<Touchscreen>();
            var pressed = new List<Vector2>();
            input.PointerPressed += pressed.Add;

            Move(mouse.position, new Vector2(25f, 35f));
            Press(mouse.leftButton);
            BeginTouch(1, new Vector2(125f, 135f));
            yield return null;

            CollectionAssert.AreEqual(new[] { new Vector2(125f, 135f) }, pressed);
        }

        [UnityTest]
        public IEnumerator Secondary_touch_and_mouse_are_ignored_until_locked_touch_releases()
        {
            var mouse = InputSystem.AddDevice<Mouse>();
            InputSystem.AddDevice<Touchscreen>();
            var pressed = new List<Vector2>();
            var moved = new List<Vector2>();
            var released = new List<Vector2>();
            input.PointerPressed += pressed.Add;
            input.PointerMoved += moved.Add;
            input.PointerReleased += released.Add;

            BeginTouch(11, new Vector2(100f, 100f));
            yield return null;
            BeginTouch(22, new Vector2(200f, 200f));
            Move(mouse.position, new Vector2(300f, 300f));
            Press(mouse.leftButton);
            yield return null;
            MoveTouch(22, new Vector2(250f, 250f));
            Move(mouse.position, new Vector2(350f, 350f));
            yield return null;
            MoveTouch(11, new Vector2(110f, 120f));
            yield return null;
            EndTouch(11, new Vector2(115f, 125f));
            yield return null;

            CollectionAssert.AreEqual(new[] { new Vector2(100f, 100f) }, pressed);
            CollectionAssert.DoesNotContain(moved, new Vector2(250f, 250f));
            CollectionAssert.DoesNotContain(moved, new Vector2(350f, 350f));
            CollectionAssert.Contains(moved, new Vector2(110f, 120f));
            CollectionAssert.AreEqual(new[] { new Vector2(115f, 125f) }, released);
        }

        [UnityTest]
        public IEnumerator Disable_releases_once_and_reenable_accepts_a_fresh_press()
        {
            var mouse = InputSystem.AddDevice<Mouse>();
            var pressCount = 0;
            var releaseCount = 0;
            input.PointerPressed += delegate { pressCount++; };
            input.PointerReleased += delegate { releaseCount++; };

            Move(mouse.position, new Vector2(10f, 10f));
            Press(mouse.leftButton);
            yield return null;
            root.SetActive(false);
            root.SetActive(true);
            Release(mouse.leftButton);
            yield return null;
            Press(mouse.leftButton);
            yield return null;
            Release(mouse.leftButton);
            yield return null;

            Assert.That(pressCount, Is.EqualTo(2));
            Assert.That(releaseCount, Is.EqualTo(2));
        }
    }
}
