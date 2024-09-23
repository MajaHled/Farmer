namespace FarmerLibrary
{
    public class ChallengeHandler
    {
        protected List<Challenge> Challenges = new List<Challenge>();
        public List<Challenge> GetChallengeList() => new List<Challenge>(Challenges);
        
        public void AddChallenge(Challenge challenge) => Challenges.Add(challenge);

        public int CheckChallenges(GameState state)
        {
            int reward = 0;
            for (int i = 0; i < Challenges.Count; i++)
            {
                if (Challenges[i].CheckFinished(state))
                {
                    reward++;
                    Challenges.RemoveAt(i);
                }
            }
            return reward;
        }
    }

    public class DefaultChallengeHandler : ChallengeHandler
    {
        public DefaultChallengeHandler()
        {
            AddChallenge(new MoneyChallenge(200, 1));
            AddChallenge(new EventChallenge(typeof(RainEvent), 1, 1));
            AddChallenge(new ChickenChallenge(1, 1));
        }
        // TODO updates
    }

    public abstract class Challenge
    {
        public int Reward { get; init; }
        public Challenge(int reward) => Reward = reward;

        public abstract bool CheckFinished(GameState state);
        public abstract string ChallengeText();
        public virtual void LogDayEnd(GameState state) { }
    }

    public class MoneyChallenge : Challenge
    {
        private int MoneyGoal;

        public MoneyChallenge(int moneyGoal, int reward) : base(reward)
        {
            MoneyGoal = moneyGoal;
        }

        public override string ChallengeText() => $"Have over ${MoneyGoal}.";

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

        public override string ChallengeText() => $"See {Event} {Goal} times.";

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

        public override string ChallengeText() => $"Have {Goal} chickens.";

        public override bool CheckFinished(GameState state) => state.CurrentCoop.ChickenCount >= Goal;
    }
}
