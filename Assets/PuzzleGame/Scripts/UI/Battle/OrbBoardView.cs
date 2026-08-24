using System;
using UnityEngine;
using UnityEngine.UIElements;
using PuzzleGame.Presentation.VFX;

namespace PuzzleGame.Presentation.UI.Battle
{
    /// <summary>
    /// Renders the 6x5 orb board and feeds pointer input (mouse or touch)
    /// to the battle source cell-by-cell. Pure presentation: swaps, matches
    /// and timers are decided by the core source.
    ///
    /// Orb visual states: normal, selected/dragging (lift + ring), matched
    /// (flash/pop), locked (chain icon), poisoned (skull tint), blocked
    /// (solid obstruction).
    /// </summary>
    public class OrbBoardView : VisualElement
    {
        const int Cols = 6;
        const int Rows = 5;

        readonly IBattleEventSource _source;
        readonly VisualElement[,] _cells = new VisualElement[Cols, Rows];
        readonly VisualElement[,] _orbs = new VisualElement[Cols, Rows];
        readonly Label[,] _stateIcons = new Label[Cols, Rows];
        BoardCell _pointerCell = new BoardCell(-1, -1);
        bool _tracking;

        /// <summary>Screen-level FX hook: (panel position, color) for match bursts.</summary>
        public Action<Vector2, OrbColor> MatchBurst;

        public OrbBoardView(IBattleEventSource source)
        {
            _source = source;
            style.flexGrow = 0f;
            style.backgroundColor = Theme.BgDeep.WithAlpha(0.75f);
            UiKit.Round(this, Theme.Radius);
            UiKit.Border(this, Theme.PanelLine, 1f);
            UiKit.Pad(this, 4f);

            for (int r = Rows - 1; r >= 0; r--)
            {
                var rowEl = new VisualElement();
                rowEl.style.flexDirection = FlexDirection.Row;
                rowEl.style.flexGrow = 1f;
                for (int c = 0; c < Cols; c++)
                {
                    var cell = new VisualElement();
                    cell.style.flexGrow = 1f;
                    cell.style.alignItems = Align.Center;
                    cell.style.justifyContent = Justify.Center;
                    var orb = new VisualElement();
                    orb.pickingMode = PickingMode.Ignore;
                    orb.style.width = Length.Percent(88);
                    orb.style.height = Length.Percent(88);
                    orb.style.unityBackgroundImageTintColor = Color.white;
                    cell.Add(orb);
                    var icon = UiKit.Text("", 15f, true, Color.white);
                    icon.pickingMode = PickingMode.Ignore;
                    icon.style.position = Position.Absolute;
                    cell.Add(icon);
                    rowEl.Add(cell);
                    _cells[c, r] = cell;
                    _orbs[c, r] = orb;
                    _stateIcons[c, r] = icon;
                }
                Add(rowEl);
            }

            RegisterCallback<PointerDownEvent>(OnPointerDown);
            RegisterCallback<PointerMoveEvent>(OnPointerMove);
            RegisterCallback<PointerUpEvent>(OnPointerUp);
            RegisterCallback<PointerCaptureOutEvent>(_ => StopTracking());

            _source.BoardChanged += Refresh;
            _source.MatchResolved += OnMatch;
            RegisterCallback<DetachFromPanelEvent>(_ =>
            {
                _source.BoardChanged -= Refresh;
                _source.MatchResolved -= OnMatch;
            });
            RegisterCallback<GeometryChangedEvent>(_ => Refresh());
            Refresh();
        }

        // ------------------------------ Input --------------------------------

        BoardCell CellAt(Vector2 localPos)
        {
            float w = resolvedStyle.width - 8f;
            float h = resolvedStyle.height - 8f;
            if (w <= 0 || h <= 0) return new BoardCell(-1, -1);
            int col = Mathf.FloorToInt((localPos.x - 4f) / (w / Cols));
            int rowFromTop = Mathf.FloorToInt((localPos.y - 4f) / (h / Rows));
            int row = Rows - 1 - rowFromTop;
            if (col < 0 || col >= Cols || row < 0 || row >= Rows) return new BoardCell(-1, -1);
            return new BoardCell(col, row);
        }

        void OnPointerDown(PointerDownEvent evt)
        {
            var cell = CellAt(evt.localPosition);
            if (cell.Col < 0) return;
            _tracking = true;
            _pointerCell = cell;
            this.CapturePointer(evt.pointerId);
            _source.BeginMove(cell);
            Refresh();
        }

        void OnPointerMove(PointerMoveEvent evt)
        {
            if (!_tracking) return;
            var cell = CellAt(evt.localPosition);
            if (cell.Col < 0) return;
            // Step cell-by-cell so fast pointer sweeps still swap every orb crossed.
            int guard = 0;
            while ((cell.Col != _pointerCell.Col || cell.Row != _pointerCell.Row) && guard++ < 12)
            {
                var step = _pointerCell;
                if (cell.Col != step.Col) step.Col += Math.Sign(cell.Col - step.Col);
                else step.Row += Math.Sign(cell.Row - step.Row);
                _source.DragTo(step);
                _pointerCell = step;
            }
        }

        void OnPointerUp(PointerUpEvent evt)
        {
            this.ReleasePointer(evt.pointerId);
            StopTracking();
        }

        void StopTracking()
        {
            if (!_tracking) return;
            _tracking = false;
            _source.EndMove();
            Refresh();
        }

        // ----------------------------- Rendering -----------------------------

        public void Refresh()
        {
            var board = _source.Board;
            var states = _source.BoardStates;
            for (int c = 0; c < Cols; c++)
            {
                for (int r = 0; r < Rows; r++)
                {
                    var orb = _orbs[c, r];
                    var icon = _stateIcons[c, r];
                    var cell = _cells[c, r];
                    var color = board[c, r];
                    var flags = states[c, r];

                    if ((int)color < 0)
                    {
                        orb.style.visibility = Visibility.Hidden;
                        icon.text = "";
                        continue;
                    }
                    orb.style.visibility = Visibility.Visible;
                    orb.style.backgroundImage = new StyleBackground(PlaceholderArt.Orb(color));

                    bool held = flags.HasFlag(OrbStateFlags.Selected) || flags.HasFlag(OrbStateFlags.Dragging);
                    orb.style.scale = new Scale(Vector2.one * (held ? 1.18f : 1f));
                    orb.style.opacity = flags.HasFlag(OrbStateFlags.Dragging) ? 0.92f : 1f;

                    if (flags.HasFlag(OrbStateFlags.Blocked))
                    {
                        orb.style.unityBackgroundImageTintColor = new Color(0.25f, 0.25f, 0.3f);
                        icon.text = "▦";
                        icon.style.color = Theme.TextDim;
                    }
                    else if (flags.HasFlag(OrbStateFlags.Locked))
                    {
                        orb.style.unityBackgroundImageTintColor = new Color(0.65f, 0.65f, 0.7f);
                        icon.text = "🔒";
                        icon.style.color = Color.white;
                    }
                    else if (flags.HasFlag(OrbStateFlags.Poisoned))
                    {
                        orb.style.unityBackgroundImageTintColor = new Color(0.8f, 0.55f, 1f);
                        icon.text = "☠";
                        icon.style.color = Theme.Dark;
                    }
                    else
                    {
                        orb.style.unityBackgroundImageTintColor = Color.white;
                        icon.text = "";
                    }

                    cell.style.backgroundColor = held
                        ? Color.white.WithAlpha(0.10f)
                        : Color.clear;
                }
            }
        }

        void OnMatch(MatchEvent ev)
        {
            foreach (var cell in ev.Cells)
            {
                var orb = _orbs[cell.Col, cell.Row];
                UiFx.Punch(orb, 1.3f, 200);
                orb.experimental.animation.Start(1f, 0f, MotionSettings.Ms(240), (e, v) =>
                {
                    e.style.opacity = v;
                }).OnCompleted(() =>
                {
                    orb.style.opacity = 1f;
                    Refresh();
                });
                if (MatchBurst != null)
                {
                    var cellEl = _cells[cell.Col, cell.Row];
                    var center = cellEl.worldBound.center;
                    MatchBurst(center, ev.Color);
                }
            }
        }
    }
}
