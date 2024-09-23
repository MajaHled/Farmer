namespace FarmerLibrary
{

    public sealed class Coop : GameObject
    {
        private readonly List<Chicken> Chickens;
        public uint Capacity { get; init; }
        public uint ChickenCount { get => (uint)Chickens.Count; }
        public ChickenFeeder Feeder { get; init; }
        private List<EggSpot> Spots { get; init; }
        public List<EggSpot> GetEggSpots() => new(Spots);

        public Coop(uint chickenSlots)
        {
            Chickens = new List<Chicken>((int)chickenSlots);
            Capacity = chickenSlots;
            Feeder = new ChickenFeeder(chickenSlots);
            Spots = new List<EggSpot>((int)chickenSlots);

            //TODO temp
            AddChicken(new Chicken());
            AddChicken(new Chicken());
        }

        public void AddChicken(Chicken chicken)
        {
            if (ChickenCount >= Capacity)
                throw new InvalidOperationException($"Cannot add more chickens, coop is already at capacity of {Capacity}.");

            Chickens.Add(chicken);
            Spots.Add(new EggSpot());
        }

        public override void EndDay()
        {
            base.EndDay();

            // Feed chicken
            for (int i = 0; i < Feeder.NumFilled; i++)
            {
                if (i >= ChickenCount)
                    break;
                Chickens[i].Feed();
            }

            // Empty feeder
            Feeder.EndDay();

            // Manage egg laying
            for (int i = 0; i < Chickens.Count; i++)
            {
                Spots[i].EndDay();
                Chickens[i].Lay(Spots[i]);
                Chickens[i].EndDay();
            }

        }
    }

    public sealed class ChickenFeeder : GameObject, IToolAcceptor
    {
        public uint Capacity { get; init; }

        public uint NumFilled { get; private set; } = 0;

        public ChickenFeeder(uint capacity)
        {
            Capacity = capacity;
        }

        public bool AddFeed()
        {
            if (NumFilled >= Capacity)
                return false;
            NumFilled++;
            return true;
        }

        public override void EndDay()
        {
            base.EndDay();
            NumFilled = 0;
        }
    }

    public sealed class Chicken : GameObject, IBuyable
    {
        private bool fed = false;
        public uint BuyPrice => 1000;
        public string Name => "Chicken";

        public bool Feed()
        {
            if (fed)
                return false;
            fed = true;
            return true;
        }

        public void Lay(EggSpot spot)
        {
            if (fed)
            {
                fed = false;
                spot.LayEgg(new Egg());
            }
        }

        public override void EndDay()
        {
            base.EndDay();
            fed = false;
        }
    }

    public class EggSpot : GameObject, IToolAcceptor
    {
        private Egg? Egg;

        public override void EndDay()
        {
            base.EndDay();
            Egg = null;
        }

        public bool LayEgg(Egg egg)
        {
            if (Egg == null)
            {
                Egg = egg;
                return true;
            }
            return false;
        }

        public bool HasEgg() => Egg is Egg;

        public Egg? Collect()
        {
            if (Egg is Egg e)
            {
                Egg = null;
                return e;
            }
            return null;
        }

    }

    public class Egg : ISellable
    {
        public uint SellPrice => 100; //TODO temp
    }
}
