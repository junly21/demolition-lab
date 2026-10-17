namespace DemolitionLab
{
    public enum StageState
    {
        Initializing,
        Ready,
        Active,
        Settling,
        Clear,
        Failed
    }

    public sealed class StageFlowStateMachine
    {
        public StageState State { get; private set; } = StageState.Initializing;
        public int StartingAmmo { get; private set; }
        public int AmmoRemaining { get; private set; }
        public int ShotsUsed => StartingAmmo - AmmoRemaining;

        public void Initialize(int startingAmmo)
        {
            StartingAmmo = startingAmmo;
            AmmoRemaining = startingAmmo;
            State = StageState.Initializing;
        }

        public bool MarkReady()
        {
            return Transition(StageState.Initializing, StageState.Ready);
        }

        public bool Activate()
        {
            return Transition(StageState.Ready, StageState.Active);
        }

        public bool RegisterShot()
        {
            if (State != StageState.Active || AmmoRemaining <= 0)
            {
                return false;
            }

            AmmoRemaining--;
            return true;
        }

        public bool BeginSettling()
        {
            return Transition(StageState.Active, StageState.Settling);
        }

        public bool Complete(bool cleared)
        {
            return Transition(
                StageState.Settling,
                cleared ? StageState.Clear : StageState.Failed);
        }

        private bool Transition(StageState expected, StageState next)
        {
            if (State != expected)
            {
                return false;
            }

            State = next;
            return true;
        }
    }
}
