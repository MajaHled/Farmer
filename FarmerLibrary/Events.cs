namespace FarmerLibrary
{
    public class DayEventHandler
    {
        private List<DayEvent> events = new List<DayEvent>();

        public List<DayEvent> TryEvents(GameState state)
        {
            var done = new List<DayEvent>();
            foreach (DayEvent e in events)
            {
                if (e.TryEvent(state))
                    done.Add(e);
            }
            return done;
        }

        public void AddEvent(DayEvent e) => events.Add(e);
    }

    public abstract class DayEvent
    {
        protected Random rnd = new Random();
        private double Chance;

        public DayEvent(double chance)
        {
            Chance = chance;
        }

        public bool TryEvent(GameState state)
        {
            if (rnd.NextDouble() < Chance)
            {
                StartEvent(state);
                return true;
            }
            return false;
        }

        protected abstract void StartEvent(GameState state);
    }

    #region Events

    public sealed class RainEvent : DayEvent
    {
        public RainEvent(double chance) : base(chance) { }

        protected override void StartEvent(GameState state)
        {
            foreach (Farm farm in state.GetFarmList())
                for (int i = 0; i < farm.Rows * farm.Cols; i++)
                    farm[i].Water();
        }
    }

    public sealed class WormEvent : DayEvent
    {
        double WormChance;
        public WormEvent(double occuracneChance, double wormChance) : base(occuracneChance)
        {
            WormChance = wormChance;
        }

        protected override void StartEvent(GameState state)
        {
            foreach (Farm farm in state.GetFarmList())
                for (int i = 0; i < farm.Rows * farm.Cols; i++)
                    if (rnd.NextDouble() < WormChance)
                        farm[i].GiveBug();
        }
    }

    #endregion
}
