using System;

namespace PuzzleGame.Core.Gacha
{
    public enum PackSummonState
    {
        PreStart,
        PurchaseValidated,
        PackPresented,
        PackRipStarted,
        PackOpened,
        CardReady,
        CardRevealed,
        AllCardsRevealed,
        ResultsComplete
    }

    public sealed class PackSummonFlowEvent
    {
        internal PackSummonFlowEvent(PackSummonState state, SummonBatch batch, int cardIndex)
        { State = state; Batch = batch; CardIndex = cardIndex; }
        public PackSummonState State { get; private set; }
        public SummonBatch Batch { get; private set; }
        public int CardIndex { get; private set; }
    }

    public sealed class PackSummonFlow
    {
        private readonly SummonBatch batch;
        private PackSummonState state;
        private int nextCardIndex;
        private bool dispatching;

        private PackSummonFlow(SummonBatch batch)
        {
            this.batch = batch ?? throw new ArgumentNullException("batch");
            if (batch.Results == null || batch.Results.Count == 0) throw new ArgumentException("Summon batches must contain results.", "batch");
            state = PackSummonState.PreStart;
        }

        public static PackSummonFlow Create(SummonBatch batch) { return new PackSummonFlow(batch); }
        public static PackSummonFlow Start(SummonBatch batch) { var flow = Create(batch); flow.Begin(); return flow; }
        public SummonBatch Batch { get { return batch; } }
        public PackSummonState State { get { return state; } }
        public int NextCardIndex { get { return nextCardIndex; } }

        public event Action<PackSummonFlowEvent> Transitioned;
        public event Action<PackSummonFlowEvent> PurchaseValidated;
        public event Action<PackSummonFlowEvent> PackPresented;
        public event Action<PackSummonFlowEvent> PackRipStarted;
        public event Action<PackSummonFlowEvent> PackOpened;
        public event Action<PackSummonFlowEvent> CardReady;
        public event Action<PackSummonFlowEvent> CardRevealed;
        public event Action<PackSummonFlowEvent> AllCardsRevealed;
        public event Action<PackSummonFlowEvent> ResultsComplete;

        public void Begin() { Transition(PackSummonState.PreStart, PackSummonState.PurchaseValidated, -1); }
        public void PresentPack() { Transition(PackSummonState.PurchaseValidated, PackSummonState.PackPresented, -1); }
        public void StartPackRip() { Transition(PackSummonState.PackPresented, PackSummonState.PackRipStarted, -1); }
        public void OpenPack() { Transition(PackSummonState.PackRipStarted, PackSummonState.PackOpened, -1); }

        public void PrepareNextCard()
        {
            EnsureNotDispatching();
            if ((state != PackSummonState.PackOpened && state != PackSummonState.CardRevealed) || nextCardIndex >= batch.Results.Count)
                throw new InvalidOperationException("No card may be prepared in the current flow state.");
            SetAndNotify(PackSummonState.CardReady, nextCardIndex);
        }

        public void RevealReadyCard()
        {
            EnsureNotDispatching();
            if (state != PackSummonState.CardReady) throw new InvalidOperationException("A card must be ready before it can be revealed.");
            var cardIndex = nextCardIndex;
            nextCardIndex++;
            SetAndNotify(PackSummonState.CardRevealed, cardIndex);
        }

        public void MarkAllCardsRevealed()
        {
            EnsureNotDispatching();
            if (state != PackSummonState.CardRevealed || nextCardIndex != batch.Results.Count)
                throw new InvalidOperationException("Every card must be revealed before finishing the reveal sequence.");
            SetAndNotify(PackSummonState.AllCardsRevealed, -1);
        }

        public void CompleteResults()
        {
            EnsureNotDispatching();
            if (state != PackSummonState.AllCardsRevealed) throw new InvalidOperationException("Results can complete only after all cards are revealed.");
            SetAndNotify(PackSummonState.ResultsComplete, -1);
        }

        public void RevealAll()
        {
            EnsureNotDispatching();
            if (state != PackSummonState.PackOpened && state != PackSummonState.CardReady && state != PackSummonState.CardRevealed)
                throw new InvalidOperationException("Reveal All is unavailable in the current flow state.");
            dispatching = true;
            try
            {
                if (state == PackSummonState.CardReady)
                {
                    var readyIndex = nextCardIndex; nextCardIndex++; SetAndNotifyWhileDispatching(PackSummonState.CardRevealed, readyIndex);
                }
                while (nextCardIndex < batch.Results.Count)
                {
                    SetAndNotifyWhileDispatching(PackSummonState.CardReady, nextCardIndex);
                    var revealIndex = nextCardIndex; nextCardIndex++; SetAndNotifyWhileDispatching(PackSummonState.CardRevealed, revealIndex);
                }
                SetAndNotifyWhileDispatching(PackSummonState.AllCardsRevealed, -1);
                SetAndNotifyWhileDispatching(PackSummonState.ResultsComplete, -1);
            }
            finally { dispatching = false; }
        }

        private void Transition(PackSummonState expected, PackSummonState next, int cardIndex)
        {
            EnsureNotDispatching();
            if (state != expected) throw new InvalidOperationException("Transition is out of order.");
            SetAndNotify(next, cardIndex);
        }

        private void SetAndNotify(PackSummonState next, int cardIndex)
        {
            dispatching = true;
            try { SetAndNotifyWhileDispatching(next, cardIndex); }
            finally { dispatching = false; }
        }

        private void SetAndNotifyWhileDispatching(PackSummonState next, int cardIndex)
        {
            state = next;
            var args = new PackSummonFlowEvent(next, batch, cardIndex);
            var transition = Transitioned; if (transition != null) transition(args);
            switch (next)
            {
                case PackSummonState.PurchaseValidated: Invoke(PurchaseValidated, args); break;
                case PackSummonState.PackPresented: Invoke(PackPresented, args); break;
                case PackSummonState.PackRipStarted: Invoke(PackRipStarted, args); break;
                case PackSummonState.PackOpened: Invoke(PackOpened, args); break;
                case PackSummonState.CardReady: Invoke(CardReady, args); break;
                case PackSummonState.CardRevealed: Invoke(CardRevealed, args); break;
                case PackSummonState.AllCardsRevealed: Invoke(AllCardsRevealed, args); break;
                case PackSummonState.ResultsComplete: Invoke(ResultsComplete, args); break;
            }
        }

        private static void Invoke(Action<PackSummonFlowEvent> hook, PackSummonFlowEvent args) { if (hook != null) hook(args); }
        private void EnsureNotDispatching() { if (dispatching) throw new InvalidOperationException("Pack flow cannot transition from a transition callback."); }
    }
}
