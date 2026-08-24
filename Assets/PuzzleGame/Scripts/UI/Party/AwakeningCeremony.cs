using System;
using UnityEngine;
using UnityEngine.UIElements;
using PuzzleGame.Presentation.VFX;

namespace PuzzleGame.Presentation.UI.Party
{
    /// <summary>
    /// The 6★ Awakening ceremony — a headline presentation moment.
    /// Sequence: base card floats center → gathering light + escalating
    /// shake → white burst → awakened art, frame and sixth star slam in.
    /// Reads as an achievement; no duplicate messaging anywhere.
    /// </summary>
    public class AwakeningCeremony : VisualElement
    {
        readonly CharacterView _before;
        readonly Func<CharacterView> _performAwaken;
        readonly Action _onDone;
        VisualElement _card;
        VisualElement _stage;

        public AwakeningCeremony(CharacterView before, Func<CharacterView> performAwaken, Action onDone)
        {
            _before = before;
            _performAwaken = performAwaken;
            _onDone = onDone;

            style.position = Position.Absolute;
            style.left = 0; style.right = 0; style.top = 0; style.bottom = 0;
            style.backgroundColor = Theme.BgDeep.WithAlpha(0.94f);
            style.alignItems = Align.Center;
            style.justifyContent = Justify.Center;

            _stage = new VisualElement();
            _stage.style.alignItems = Align.Center;
            _stage.style.justifyContent = Justify.Center;
            Add(_stage);

            _card = UiKit.PortraitCard(_before, 200f, 248f);
            _stage.Add(_card);

            var caption = UiKit.Text("Something stirs within " + _before.DisplayName + "...", 16f, true, Theme.TextDim);
            caption.style.marginTop = 18f;
            _stage.Add(caption);

            schedule.Execute(Charge).ExecuteLater(MotionSettings.Ms(700));
        }

        void Charge()
        {
            int chargeMs = MotionSettings.Ms(1800);
            // Gathering light: rising sparks converging on the card.
            if (!MotionSettings.ReducedMotion)
            {
                for (int i = 0; i < 5; i++)
                {
                    int delay = i * (chargeMs / 5);
                    schedule.Execute(() =>
                    {
                        var center = this.WorldToLocal(_card.worldBound.center);
                        UiFx.Burst(this, center, Theme.Rarity6, 12, 90f);
                        UiFx.Shake(_card, 3f + 2f, 300);
                    }).ExecuteLater(delay);
                }
            }
            _card.experimental.animation.Start(0f, 1f, chargeMs, (e, t) =>
            {
                UiKit.Border(e, Color.Lerp(Theme.RarityColor(_before.BaseRarity), Color.white, t), 3f + t * 3f);
                e.style.scale = new Scale(Vector2.one * (1f + t * 0.06f));
            }).OnCompleted(Transform);
        }

        void Transform()
        {
            var after = _performAwaken();
            if (after == null)
            {
                RemoveFromHierarchy();
                _onDone?.Invoke();
                return;
            }
            // Blinding flash covers the swap.
            UiFx.Flash(this, Color.white, 0.95f, MotionSettings.Ms(650));
            UiFx.Shake(this, 10f, 400);
            schedule.Execute(() => Reveal(after)).ExecuteLater(MotionSettings.Ms(240));
        }

        void Reveal(CharacterView after)
        {
            _stage.Clear();
            _card = UiKit.PortraitCard(after, 220f, 272f);
            _stage.Add(_card);
            UiFx.Punch(_card, 1.25f, MotionSettings.Ms(500));
            var center = this.WorldToLocal(_card.worldBound.center);
            UiFx.Burst(this, _stage.worldBound.size == Vector2.zero ? new Vector2(0, 0) : center, Theme.Rarity6, 26, 130f);

            var title = UiKit.Title("AWAKENED!", 34f);
            title.style.color = Theme.Rarity6;
            title.style.marginTop = 14f;
            _stage.Add(title);
            UiFx.Punch(title, 1.4f, MotionSettings.Ms(400));

            var name = UiKit.Text(after.DisplayName, 20f, true);
            _stage.Add(name);
            if (!string.IsNullOrEmpty(after.Epithet))
            {
                _stage.Add(UiKit.Dim("“" + after.Epithet + "”", 14f));
            }

            // Sixth star slams in after a beat.
            var stars = UiKit.Stars(after.BaseRarity, true, 22f);
            stars.style.marginTop = 8f;
            _stage.Add(stars);
            if (stars.childCount >= 6 && !MotionSettings.ReducedMotion)
            {
                var sixth = stars[5];
                sixth.style.opacity = 0f;
                schedule.Execute(() =>
                {
                    sixth.style.opacity = 1f;
                    UiFx.Punch(sixth, 2.4f, 450);
                    UiFx.Shake(_stage, 6f, 260);
                    UiFx.Burst(this, this.WorldToLocal(sixth.worldBound.center), Theme.Rarity5, 14, 60f);
                }).ExecuteLater(500);
            }

            var done = UiKit.WarmButton("Magnificent", () =>
            {
                RemoveFromHierarchy();
                _onDone?.Invoke();
            });
            done.style.marginTop = 20f;
            done.style.opacity = 0f;
            _stage.Add(done);
            done.experimental.animation.Start(0f, 1f, MotionSettings.Ms(400), (e, v) => e.style.opacity = v);
        }
    }
}
