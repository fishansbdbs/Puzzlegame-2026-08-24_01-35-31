using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

namespace PuzzleGame.Presentation.UI
{
    /// <summary>Base class for full-screen UI pages.</summary>
    public abstract class UiScreen
    {
        protected UiRouter Router { get; private set; }
        public VisualElement Root { get; private set; }

        public void Attach(UiRouter router)
        {
            Router = router;
            Root = new VisualElement();
            Root.style.position = Position.Absolute;
            Root.style.left = 0; Root.style.right = 0;
            Root.style.top = 0; Root.style.bottom = 0;
            Root.style.backgroundColor = Theme.Bg;
            Build(Root);
        }

        /// <summary>Build the screen's element tree once.</summary>
        protected abstract void Build(VisualElement root);

        /// <summary>Called each time the screen becomes visible.</summary>
        public virtual void OnEnter() { }
        public virtual void OnExit() { }
        /// <summary>Per-frame tick while the screen is on top.</summary>
        public virtual void Tick(float deltaTime) { }

        /// <summary>Standard header with back arrow, title and currency chips.</summary>
        protected VisualElement Header(string title, bool backButton = true)
        {
            var bar = UiKit.Row(10f);
            bar.style.paddingLeft = 14f;
            bar.style.paddingRight = 14f;
            bar.style.paddingTop = 8f;
            bar.style.paddingBottom = 8f;
            bar.style.backgroundColor = Theme.BgDeep.WithAlpha(0.85f);
            bar.style.flexShrink = 0f;
            if (backButton)
            {
                var back = UiKit.Button("‹ Back", () => Router.Pop());
                bar.Add(back);
            }
            bar.Add(UiKit.Title(title, 19f));
            bar.Add(UiKit.Spacer());

            var economy = PresentationServices.GetOrNull<IEconomySource>();
            if (economy != null)
            {
                var goldChip = UiKit.CurrencyChip("◆", UiKit.FormatNumber(economy.GetBalance(CurrencyId.Gold)), Theme.GoldColor);
                var gemChip = UiKit.CurrencyChip("❖", UiKit.FormatNumber(economy.GetBalance(CurrencyId.Gems)), Theme.GemColor);
                bar.Add(goldChip);
                bar.Add(gemChip);
                Action refresh = () =>
                {
                    var g = goldChip.Q<Label>("chip-value");
                    if (g != null) g.text = UiKit.FormatNumber(economy.GetBalance(CurrencyId.Gold));
                    var m = gemChip.Q<Label>("chip-value");
                    if (m != null) m.text = UiKit.FormatNumber(economy.GetBalance(CurrencyId.Gems));
                };
                economy.BalancesChanged += refresh;
                Root.RegisterCallback<DetachFromPanelEvent>(_ => economy.BalancesChanged -= refresh);
            }
            return bar;
        }
    }

    /// <summary>
    /// Stack-based navigation over one UIDocument root. Landscape-first: the
    /// router also applies device safe-area padding to every screen.
    /// </summary>
    public class UiRouter
    {
        readonly VisualElement _host;
        readonly List<UiScreen> _stack = new List<UiScreen>();

        public UiRouter(VisualElement host)
        {
            _host = host;
            _host.style.flexGrow = 1f;
            ApplySafeArea();
        }

        public UiScreen Current => _stack.Count > 0 ? _stack[_stack.Count - 1] : null;

        public void Push(UiScreen screen)
        {
            var prev = Current;
            if (prev != null)
            {
                prev.OnExit();
                prev.Root.style.display = DisplayStyle.None;
            }
            screen.Attach(this);
            _stack.Add(screen);
            _host.Add(screen.Root);
            AnimateIn(screen.Root);
            screen.OnEnter();
        }

        public void Pop()
        {
            if (_stack.Count <= 1) return;
            var top = Current;
            top.OnExit();
            _stack.RemoveAt(_stack.Count - 1);
            top.Root.RemoveFromHierarchy();
            var revealed = Current;
            if (revealed != null)
            {
                revealed.Root.style.display = DisplayStyle.Flex;
                AnimateIn(revealed.Root);
                revealed.OnEnter();
            }
        }

        /// <summary>Clear the stack and land on a single screen.</summary>
        public void Home(UiScreen screen)
        {
            foreach (var s in _stack)
            {
                s.OnExit();
                s.Root.RemoveFromHierarchy();
            }
            _stack.Clear();
            Push(screen);
        }

        public void Tick(float deltaTime)
        {
            Current?.Tick(deltaTime);
        }

        void AnimateIn(VisualElement root)
        {
            if (MotionSettings.ReducedMotion) return;
            root.style.opacity = 0f;
            root.experimental.animation.Start(0f, 1f, MotionSettings.Ms(180), (e, v) => e.style.opacity = v);
        }

        void ApplySafeArea()
        {
            _host.RegisterCallback<GeometryChangedEvent>(_ =>
            {
                var safe = Screen.safeArea;
                float scaleX = _host.resolvedStyle.width / Mathf.Max(1f, Screen.width);
                float scaleY = _host.resolvedStyle.height / Mathf.Max(1f, Screen.height);
                _host.style.paddingLeft = safe.xMin * scaleX;
                _host.style.paddingRight = (Screen.width - safe.xMax) * scaleX;
                _host.style.paddingTop = (Screen.height - safe.yMax) * scaleY;
                _host.style.paddingBottom = safe.yMin * scaleY;
            });
        }
    }
}
