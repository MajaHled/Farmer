
namespace FarmerLibrary
{
    public sealed class Farm : GameObject
    {
        public uint Rows { get; }
        public uint Cols { get; }

        private Plot[,] Plots;

        public Plot? Highlighted { get; private set; }


        public Farm(uint rows, uint cols)
        {
            Rows = rows;
            Cols = cols;
            Plots = new Plot[rows, cols];
            for (int i = 0; i < rows; i++)
            {
                for (int j = 0; j < cols; j++)
                {
                    Plots[i, j] = new Plot();
                }
            }
            Highlighted = null;
        }

        public Plot this[int x, int y]
        {
            get => Plots[x, y];
        }

        public Plot this[int i]
        {
            get => Plots[i / Cols, i % Cols];
        }

        public bool Planted
        {
            get
            {
                for (int i = 0; i < Rows; i++)
                {
                    for (int j = 0; j < Cols; j++)
                    {
                        if (!Plots[i, j].IsEmpty)
                            return true;
                    }
                }
                return false;
            }
        }

        public void Highlight(int i, int j)
        {
            Highlighted = Plots[i, j];
        }

        public void Unhighlight()
        {
            Highlighted = null;
        }


        public bool PlantASeed(Seed seed)
        {
            foreach (Plot plot in Plots)
            {
                if (!plot.CanPlant)
                {
                    return false;
                }
            }

            foreach (Plot plot in Plots)
            {
                plot.PlantASeed(seed);
            }
            return true;
        }

        public override void EndDay()
        {
            base.EndDay();

            foreach (Plot plot in Plots)
            {
                plot.EndDay();
            }
        }

    }

    public enum GrowthState { Seed, SmallSeedling, BigSeedling, Adult, Fruiting }

    public abstract class Plant : GameObject
    {
        // Must be tied to a plot, because the harvest mechanic relies on the plot deleting the plant when appropriate
        private Plot Plot;
        public Plant(Plot plantedIn)
        {
            Plot = plantedIn;
        }

        private Random rnd = new Random();

        protected int BugChance = 50;
        public bool HasBug { get; private set; } = false;

        public GrowthState State { get; private set; } = GrowthState.Seed;
        public bool Alive { get; private set; } = true;
        protected abstract int DaysToGrow { get; }
        protected abstract int TimesHarvestable { get; }

        private int TimesHarvested = 0;
        private int DaysWateredSinceLastGrow = 0;
        private bool WateredToday = false;
        private bool FertilizedToday = false;

        public bool Water()
        {
            if (!WateredToday)
            {
                WateredToday = true;
                return true;
            }
            return false;
        }

        public bool Fertilize()
        {
            if (!FertilizedToday)
            {
                FertilizedToday = true;
                return true;
            }
            return false;
        }

        protected abstract Fruit CreateFruit();
        public Fruit? Harvest()
        {
            // Check if harvestable
            if (TimesHarvested >= TimesHarvestable)
            {
                throw new InvalidOperationException($"Cannot harvest an overharvested plant. Times harvested: {TimesHarvested}, times harvestable: {TimesHarvestable}.");
            }

            if (State != GrowthState.Fruiting || !Alive)
                return null;

            // Actually harvest
            TimesHarvested++;
            if (TimesHarvested < TimesHarvestable)
            {
                State = GrowthState.Adult;
                DaysWateredSinceLastGrow = 0;
            }
            else
                Plot.DestroyPlant();

            return CreateFruit();
        }

        public bool BugSpray()
        {
            if (HasBug)
            {
                HasBug = false;
                return true;
            }
            return false;
        }
        public void GiveBug() => HasBug = true;

        private bool DiesOfBug() => rnd.Next(2) == 0;
        public sealed override void EndDay()
        {
            base.EndDay();

            if (HasBug && DiesOfBug())
                Alive = false;
            HasBug = false;

            if (rnd.Next(BugChance) == 0)
                HasBug = true;

            if (WateredToday)
            {
                DaysWateredSinceLastGrow++;
                if (FertilizedToday)
                    DaysWateredSinceLastGrow++;
            }

            WateredToday = false;
            FertilizedToday = false;

            if (State != GrowthState.Fruiting && Alive && DaysWateredSinceLastGrow >= DaysToGrow)
            {
                State++;
                DaysWateredSinceLastGrow = 0;
            }
        }
    }

    #region Plant classes   
    public sealed class RaddishPlant : Plant
    {
        public RaddishPlant(Plot plot) : base(plot) { }
        protected override int DaysToGrow => 1;
        protected override int TimesHarvestable => 1;
        protected override Fruit CreateFruit() => new RaddishFruit();
    }

    public sealed class CarrotPlant : Plant
    {
        public CarrotPlant(Plot plot) : base(plot) { }
        protected override int DaysToGrow => 2;
        protected override int TimesHarvestable => 1;
        protected override Fruit CreateFruit() => new CarrotFruit();

    }

    public sealed class PotatoPlant : Plant
    {
        public PotatoPlant(Plot plot) : base(plot) { }
        protected override int DaysToGrow => 3;
        protected override int TimesHarvestable => 1;
        protected override Fruit CreateFruit() => new PotatoFruit();
    }

    public sealed class TomatoPlant : Plant
    {
        public TomatoPlant(Plot plot) : base(plot) { }
        protected override int DaysToGrow => 4;
        protected override int TimesHarvestable => 3;
        protected override Fruit CreateFruit() => new TomatoFruit();
    }

    public sealed class MelonPlant : Plant
    {
        public MelonPlant(Plot plot) : base(plot) { }
        protected override int DaysToGrow => 6;
        protected override int TimesHarvestable => 3;
        protected override Fruit CreateFruit() => new MelonFruit();
    }
    #endregion

    public abstract class Fruit : ISellable
    {
        public abstract uint SellPrice { get; }
    }

    #region Fruit classes  
    public sealed class RaddishFruit : Fruit
    {
        public override uint SellPrice => 50;
    }
    public sealed class CarrotFruit : Fruit
    {
        public override uint SellPrice => 60;
    }
    public sealed class PotatoFruit : Fruit
    {
        public override uint SellPrice => 100;
    }
    public sealed class TomatoFruit : Fruit
    {
        public override uint SellPrice => 150;
    }
    public sealed class MelonFruit : Fruit
    {
        public override uint SellPrice => 500;
    }
    #endregion  

    public abstract class Seed : IBuyable
    {
        public abstract uint BuyPrice { get; }
        public abstract Plant PlantToPlot(Plot plot);
        public abstract string Name { get; }
    }

    #region Seed classes   
    public sealed class RaddishSeed : Seed
    {
        public override uint BuyPrice => 100;
        public override Plant PlantToPlot(Plot plot) => new RaddishPlant(plot);
        public override string Name => "Raddish";
    }

    public sealed class CarrotSeed : Seed
    {
        public override uint BuyPrice => 200;
        public override Plant PlantToPlot(Plot plot) => new CarrotPlant(plot);
        public override string Name => "Carrot";
    }

    public sealed class PotatoSeed : Seed
    {
        public override uint BuyPrice => 300;
        public override Plant PlantToPlot(Plot plot) => new PotatoPlant(plot);
        public override string Name => "Potato";
    }

    public sealed class TomatoSeed : Seed
    {
        public override uint BuyPrice => 500;
        public override Plant PlantToPlot(Plot plot) => new TomatoPlant(plot);
        public override string Name => "Tomato";
    }
    public sealed class MelonSeed : Seed
    {
        public override uint BuyPrice => 900;
        public override Plant PlantToPlot(Plot plot) => new MelonPlant(plot);
        public override string Name => "Melon";
    }

    #endregion

    public sealed class Plot : GameObject, IToolAcceptor
    {
        public bool Watered { get; private set; } = false;
        private Plant? PlantedPlant = null;

        public bool IsEmpty => PlantedPlant is null;
        public override void EndDay()
        {
            base.EndDay();
            Watered = false;
            PlantedPlant?.EndDay();
        }

        public bool DestroyPlant()
        {
            if (PlantedPlant is null)
                return false;

            PlantedPlant = null;
            return true;
        }

        public bool CanPlant => PlantedPlant is null;
        public bool PlantASeed(Seed seed)
        {
            if (!CanPlant)
                return false;
            PlantedPlant = seed.PlantToPlot(this);
            if (Watered)
                PlantedPlant.Water();
            return true;
        }

        // Forwarding plant interactions
        public bool Water()
        {
            if (!Watered)
            {
                Watered = true;
                if (PlantedPlant is Plant p)
                    p.Water();
                return true;
            }
            return false;
        }
        public bool Fertilize()
        {
            if (PlantedPlant is Plant p)
                return p.Fertilize();
            return false;
        }

        public GrowthState? State => PlantedPlant?.State;
        public bool? Alive => PlantedPlant?.Alive;
        public Type? PlantType => PlantedPlant?.GetType();
        public void GiveBug() => PlantedPlant?.GiveBug();
        public bool HasBug => PlantedPlant switch
        {
            Plant => PlantedPlant.HasBug,
            _ => false
        };
        public Fruit? Harvest() => PlantedPlant?.Harvest();
        public bool BugSpray() => PlantedPlant switch
        {
            Plant => PlantedPlant.BugSpray(),
            _ => false
        };
    }

}
