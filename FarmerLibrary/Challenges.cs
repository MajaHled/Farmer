namespace FarmerLibrary
{
    public class ChallengeHandler
    {
        protected List<Challenge> Challenges = new List<Challenge>();
        public List<Challenge> GetChallengeList() => new List<Challenge>(Challenges);
        // Number of active challenges.
        // Challenges further down the list will be ignored for evaluation until space before them frees up
        // If null, all challenges in the list are considered.
        protected int? ActiveNumber;

        public ChallengeHandler(int activeNumber)
        {
            ActiveNumber = activeNumber;
        }

        public ChallengeHandler() { }

        public void SetActiveNumber(int activeNumber) => ActiveNumber = activeNumber;

        public void AddChallenge(Challenge challenge) => Challenges.Add(challenge);

        public int CheckChallenges(GameState state)
        {
            int reward = 0;

            bool cont = true;
            while (cont)
            {
                ReplenishChallenges();
                cont = false;
                int limit;
                
                if (ActiveNumber is int n)
                    limit = Math.Min(Challenges.Count, n);
                else
                    limit = Challenges.Count;

                var toRemove = new List<int>();
                for (int i = 0; i < limit; i++)
                {
                    if (Challenges[i].CheckFinished(state))
                    {
                        reward++;
                        toRemove.Add(i);
                        cont = true;
                    }
                }

                foreach (int i in toRemove)
                    Challenges.RemoveAt(i);
            }

            return reward;
        }

        public void LogDayEnd(GameState state)
        {
            foreach (Challenge c in Challenges)
                c.LogDayEnd(state);
        }

        // Should be overriden in subclasses to indicate whether creation of new challenges via NextChallenge() is possible
        protected virtual bool CanCreateNext => false;

        // Should be overriden in subclasses to supply more challenges for ReplenishChallenges()
        protected virtual Challenge NextChallenge()
        {
            throw new InvalidOperationException("Can't create more Challenges.");
        }

        // Generates new challenges to that there's at least ActiveNumber of them
        // If ActiveNumber is null, this does nothing
        public void ReplenishChallenges()
        {
            if (ActiveNumber is int n)
                for (int i = Challenges.Count; i < n; i++)
                {
                    if (!CanCreateNext)
                        return;
                    AddChallenge(NextChallenge());
                }
        }
    }

    public class DefaultChallengeHandler : ChallengeHandler
    {
        private static List<int> MoneyValues = new List<int> { 200, 500, 1000, 2000, 3000, 5000, 10000 };
        private int MoneyIndex = 0;
        
        private static List<int> EventValues = new List<int> { 1, 2, 3, 5, 10 };
        private int RainEventIndex = 0;
        private int WormEventIndex = 0;
        private bool WormNext = false;

        private int ChickenIndex = 1;

        private int NextType = 0;

        public DefaultChallengeHandler() : base(3)
        {
            ReplenishChallenges();
        }

        protected override bool CanCreateNext => true;

        protected override Challenge NextChallenge()
        {
            Challenge result;

            switch (NextType)
            {
                case 0:
                    if (MoneyIndex < MoneyValues.Count)
                        result = new MoneyChallenge(MoneyValues[MoneyIndex], MoneyIndex);
                    else
                    {
                        int value = MoneyValues[MoneyValues.Count - 1] * (MoneyIndex - MoneyValues.Count + 1);
                        result = new MoneyChallenge(value, MoneyIndex);
                    }
                    MoneyIndex++;
                    break;
                case 1:
                    if (WormNext)
                    {
                        result = new EventChallenge(typeof(WormEvent), EventValues[WormEventIndex], WormEventIndex+1);
                        WormEventIndex++;
                        WormNext = false;
                    }
                    else
                    {
                        result = new EventChallenge(typeof(RainEvent), EventValues[RainEventIndex], RainEventIndex+1);
                        RainEventIndex++;
                        WormNext = true;
                    }
                    break;
                case 2:
                    if (ChickenIndex <= 5)
                    {
                        result = new ChickenChallenge(ChickenIndex, ChickenIndex);
                        ChickenIndex++;
                    }
                    else
                    {
                        NextType = 0;
                        result = NextChallenge();
                    }
                    break;
                default:
                    throw new ArgumentException("Invalid NextType."); 
            }

            NextType = (NextType + 1) % 3;
            return result;
        }
    }

    public abstract class Challenge
    {
        public int Reward { get; init; }
        public Challenge(int reward) => Reward = reward;

        public abstract bool CheckFinished(GameState state);

        public abstract string ChallengeText { get; }


        public virtual void LogDayEnd(GameState state) { }
    }

    public class MoneyChallenge : Challenge
    {
        private int MoneyGoal;

        public MoneyChallenge(int moneyGoal, int reward) : base(reward)
        {
            MoneyGoal = moneyGoal;
        }

        public override string ChallengeText => $"Have over ${MoneyGoal}.";

        public override bool CheckFinished(GameState state) => state.PlayerMoney >= MoneyGoal;
    }

    public class EventChallenge : Challenge
    {
        private Type Event;
        private int Goal, Seen = 0;

        public EventChallenge(Type eventType, int number, int reward) : base(reward)
        {
            if (!typeof(DayEvent).IsAssignableFrom(eventType))
                throw new ArgumentException($"{eventType} is not a DayEvent type.");
            Event = eventType;
            Goal = number;
        }

        public override string ChallengeText => $"See {Event.Name} {Goal} times.";

        public override bool CheckFinished(GameState state) => Seen >= Goal;

        public override void LogDayEnd(GameState state)
        {
            foreach (var e in state.TodaysEvents)
            {
                if (e.GetType() == Event)
                    Seen++;
            }
        }
    }

    public class ChickenChallenge : Challenge
    {
        private int Goal;
        public ChickenChallenge(int number, int reward) : base(reward)
        {
            Goal = number;
        }

        public override string ChallengeText => $"Have {Goal} chickens.";

        public override bool CheckFinished(GameState state) => state.CurrentCoop.ChickenCount >= Goal;
    }
}
