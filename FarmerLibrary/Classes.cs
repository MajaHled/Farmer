namespace FarmerLibrary
{
    public enum View { FullView, FarmView, CoopView, HouseView } //TODO shop view

    public abstract class Tool
    {
        public abstract void Use(GameState state, Plot plot);
    }

    #region Tool classes

    public sealed class Hand : Tool
    {
        public override void Use(GameState state, Plot plot)
        {
            state.HeldProduct = plot.Harvest();
        }
    }

    public sealed class Pail : Tool
    {
        public override void Use(GameState state, Plot plot)
        {
            plot.Water();
        }
    }

    public sealed class Bag : Tool
    {
        public override void Use(GameState state, Plot plot)
        {
            throw new NotImplementedException();
        }
    }

    public sealed class Bottle : Tool
    {
        public override void Use(GameState state, Plot plot)
        {
            plot.BugSpray();
        }
    }

    public sealed class Scythe : Tool
    {
        public override void Use(GameState state, Plot plot)
        {
            plot.DestroyPlant();
        }
    }

    #endregion

    public class GameState
    {
        public View CurrentView { get; set; }

        private List<Farm> Farms = [];
        public int CurrentFarmIndex { get; set; } = 0;
        //TODO limit checks on set, or maybe replace with SetFarm
        public Farm CurrentFarm { get => Farms[CurrentFarmIndex]; }

        public int PlayerMoney { get; private set; }

        private Dictionary<Type, int> OwnedAmounts = [];

        public void Buy(IBuyable product)
        {
            // Can player afford the product?
            if (product.BuyPrice > PlayerMoney)
                return;

            // TODO limit chicken amount

            // Add product to player's inventory
            if (!OwnedAmounts.ContainsKey(product.GetType()))
                OwnedAmounts.Add(product.GetType(), 1);
            else
                OwnedAmounts[product.GetType()] += 1;

            // Take money
            PlayerMoney -= product.BuyPrice;
        }

        public int GetOwnedAmount(IBuyable product)
        {
            return OwnedAmounts[product.GetType()];
        }

        public Tool? CurrentTool { get; set; } = null;
        public ISellable? HeldProduct { get; set; } = null;

        public void SellHeld()
        {
            if (HeldProduct is not null)
                PlayerMoney += HeldProduct.SellPrice;
            HeldProduct = null;
        }

        public void ResetTemps()
        {
            CurrentTool = null;
            HeldProduct = null;
            CurrentFarmIndex = 0;
        }

        public GameState(int numFarms, int farmRows, int farmCols, View startView, int playerMoney)
        {
            //TODO non negative checks
            for (int i = 0; i < numFarms; i++)
            {
                Farms.Add(new Farm(farmRows, farmCols));
            }

            CurrentView = startView;
            PlayerMoney = playerMoney;

            //TODO temp
            OwnedAmounts.Add(typeof(RaddishSeed), 5);
            OwnedAmounts.Add(typeof(CarrotSeed), 5);
            OwnedAmounts.Add(typeof(PotatoSeed), 5);
            OwnedAmounts.Add(typeof(TomatoSeed), 5);
        }

        public void EndDay()
        {
            foreach (var farm in Farms)
            {
                farm.EndDay();
            }
        }

        public static GameState GetClassicStartingState()
        {
            return new GameState(4, 3, 4, View.FullView, 1000);
        }
    }

    public abstract class GameObject
    {
        public virtual void EndDay() { }
    }

    public sealed class Farm : GameObject
    {
        public int Rows { get; }
        public int Cols { get; }

        private Plot[,] Plots;

        public Plot? Highlighted { get; private set; }


        public Farm(int rows, int cols)
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

        public bool Planted { get
            {
                for (int i = 0; i < Rows; i++)
                {
                    for(int j = 0; j < Cols; j++)
                    {
                        if (!Plots[i,j].IsEmpty)
                            return true;
                    }
                }
                return false;
            }
        }

        public void Highlight (int i, int j)
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

        public bool HasBug { get; private set; } = false; //TODO maybe make a bug instance which keeps wiggle state
        public GrowthState State { get; private set; } = GrowthState.Seed;
        public  bool Alive { get; private set; } = true;
        protected abstract int DaysToGrow { get; }
        protected abstract int TimesHarvestable { get; }

        private int TimesHarvested = 0;
        private int DaysWateredSinceLastGrow = 0;
        private bool WateredToday = false;
        public void Water()
        {
            if (!WateredToday)
                WateredToday = true;
        }
        
        protected abstract Fruit CreateFruit();
        public Fruit? Harvest()
        {
            // Check if harvestable
            if (TimesHarvested >= TimesHarvestable) //TODO: see how the harvested times actually work lmao
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
                DaysWateredSinceLastGrow++;
            WateredToday = false;

            if (State != GrowthState.Fruiting && Alive && DaysWateredSinceLastGrow == DaysToGrow)
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
    #endregion

    public abstract class Fruit : ISellable
    {
        public abstract int SellPrice { get; }
    }

    //TODO proper selling prices
    #region Fruit classes  
    public sealed class RaddishFruit : Fruit
    {
        public override int SellPrice => 50;
    }
    public sealed class CarrotFruit : Fruit
    {
        public override int SellPrice => 60;
    }
    public sealed class PotatoFruit : Fruit
    {
        public override int SellPrice => 100;
    }
    public sealed class TomatoFruit : Fruit
    {
        public override int SellPrice => 150;
    }
    #endregion  

    public abstract class Seed : IBuyable
    {
        public abstract int BuyPrice { get; }
        public abstract Plant PlantToPlot(Plot plot);
    }

    #region Seed classes   
    public sealed class RaddishSeed : Seed
    {
        public override int BuyPrice => 100;
        public override Plant PlantToPlot(Plot plot) => new RaddishPlant(plot);
    }

    public sealed class CarrotSeed : Seed
    {
        public override int BuyPrice => 200;
        public override Plant PlantToPlot(Plot plot) => new CarrotPlant(plot);
    }

    public sealed class PotatoSeed : Seed
    {
        public override int BuyPrice => 300;
        public override Plant PlantToPlot(Plot plot) => new PotatoPlant(plot);
    }

    public sealed class TomatoSeed : Seed
    {
        public override int BuyPrice => 500;
        public override Plant PlantToPlot(Plot plot) => new TomatoPlant(plot);
    }

    #endregion

    public sealed class Plot : GameObject
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

        public void DestroyPlant()
        {
            PlantedPlant = null;
        }

        public bool CanPlant => PlantedPlant is null;
        public bool PlantASeed(Seed seed)
        {
            if (!CanPlant)
                return false;
            PlantedPlant = seed.PlantToPlot(this);
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

    #region Interfaces   
    public interface ISellable
    {
        int SellPrice { get; }
    }

    public interface IBuyable
    {
        int BuyPrice { get; }
    }

    #endregion
}
