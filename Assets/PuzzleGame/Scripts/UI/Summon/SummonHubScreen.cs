using System;
using UnityEngine;
using UnityEngine.UIElements;

namespace PuzzleGame.Presentation.UI.Summon
{
    /// <summary>
    /// Summon hub listing all currently-active banners from the scheduler:
    /// Standard, Featured, Gather-In and Step-Up, with timers, featured
    /// units, costs and step status at a glance.
    /// </summary>
    public class SummonHubScreen : UiScreen
    {
        ScrollView _list;

        protected override void Build(VisualElement root)
        {
            root.Add(Header("Summon"));
            _list = new ScrollView(ScrollViewMode.Vertical);
            _list.style.flexGrow = 1f;
            _list.style.paddingLeft = 16f;
            _list.style.paddingRight = 16f;
            _list.style.paddingTop = 8f;
            root.Add(_list);
        }

        public override void OnEnter()
        {
            Rebuild();
        }

        void Rebuild()
        {
            _list.Clear();
            var summons = PresentationServices.Get<ISummonSource>();
            var schedule = PresentationServices.Get<IScheduleSource>();
            foreach (var banner in summons.GetBanners())
            {
                _list.Add(BannerCard(banner, schedule.NowUtc));
            }
        }

        VisualElement BannerCard(BannerVm banner, DateTime nowUtc)
        {
            var card = UiKit.Panel(true);
            card.style.marginBottom = 12f;
            var row = UiKit.Row(14f);
            card.Add(row);

            // Pack art thumbnail.
            var pack = new VisualElement();
            pack.style.width = 74f;
            pack.style.height = 100f;
            UiKit.Round(pack, 6f);
            pack.style.backgroundImage = new StyleBackground(
                PlaceholderArt.Pack(banner.PackArtRef, banner.Kind == BannerKind.Standard ? 0 : 1));
            pack.style.flexShrink = 0f;
            row.Add(pack);

            var info = UiKit.Column(4f);
            info.style.flexGrow = 1f;
            row.Add(info);

            var titleRow = UiKit.Row(8f);
            titleRow.Add(KindBadge(banner.Kind));
            titleRow.Add(UiKit.Text(banner.DisplayName, 17f, true));
            titleRow.Add(UiKit.Spacer());
            var timer = UiKit.Dim("", 12f);
            if (banner.EndsAtUtc.HasValue)
            {
                timer.text = "⏳ " + UiKit.FormatTimeRemaining(banner.EndsAtUtc.Value - nowUtc);
                timer.style.color = (banner.EndsAtUtc.Value - nowUtc).TotalDays < 1 ? Theme.Danger : Theme.TextDim;
            }
            else
            {
                timer.text = "Permanent";
            }
            titleRow.Add(timer);
            info.Add(titleRow);

            if (!string.IsNullOrEmpty(banner.Description))
            {
                info.Add(UiKit.Dim(banner.Description, 12f));
            }

            if (banner.FeaturedUnits.Count > 0)
            {
                var featuredRow = UiKit.Row(6f);
                featuredRow.Add(UiKit.Dim("Featured:", 11f));
                foreach (var unit in banner.FeaturedUnits)
                {
                    featuredRow.Add(UiKit.PortraitCard(unit, 40f, 50f));
                }
                info.Add(featuredRow);
            }

            var bottomRow = UiKit.Row(10f);
            if (banner.Steps.Count > 0)
            {
                int done = 0;
                foreach (var s in banner.Steps) if (s.Consumed) done++;
                string stepText = done >= banner.Steps.Count
                    ? "All steps complete!"
                    : "Step " + (banner.CurrentStep + 1) + "/" + banner.Steps.Count +
                      "  ❖" + banner.Steps[banner.CurrentStep].GemCost +
                      " → " + banner.Steps[banner.CurrentStep].PullCount + " characters";
                bottomRow.Add(UiKit.Text(stepText, 12f, true, Theme.AccentWarm));
                if (banner.RotationIndex > 0)
                {
                    bottomRow.Add(UiKit.Dim("Rotation " + banner.RotationIndex, 11f));
                }
            }
            else
            {
                bottomRow.Add(UiKit.Text("❖" + banner.SingleCost + " single   ❖" + banner.MultiCost + " ×" + banner.MultiCount, 12f, true, Theme.GemColor));
            }
            if (!string.IsNullOrEmpty(banner.GuaranteeText))
            {
                bottomRow.Add(UiKit.Dim("• " + banner.GuaranteeText, 11f));
            }
            bottomRow.Add(UiKit.Spacer());
            bottomRow.Add(UiKit.Button("Open ▸", () => Router.Push(new BannerDetailScreen(banner.Id)), true));
            info.Add(bottomRow);

            card.RegisterCallback<PointerDownEvent>(evt =>
            {
                if (evt.target is Button) return;
                Router.Push(new BannerDetailScreen(banner.Id));
            });
            return card;
        }

        public static VisualElement KindBadge(BannerKind kind)
        {
            string text;
            Color color;
            switch (kind)
            {
                case BannerKind.Featured: text = "FEATURED"; color = Theme.AccentWarm; break;
                case BannerKind.GatherIn: text = "GATHER-IN"; color = Theme.Rarity6; break;
                case BannerKind.StepUp: text = "STEP-UP"; color = Theme.Accent; break;
                default: text = "STANDARD"; color = Theme.TextDim; break;
            }
            var badge = UiKit.Text(text, 10f, true, Theme.BgDeep);
            badge.style.backgroundColor = color;
            UiKit.Round(badge, 4f);
            badge.style.paddingLeft = 6f; badge.style.paddingRight = 6f;
            badge.style.paddingTop = 2f; badge.style.paddingBottom = 2f;
            return badge;
        }
    }
}
