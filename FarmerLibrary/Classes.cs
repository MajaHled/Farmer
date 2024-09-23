﻿using System.Reflection.Metadata.Ecma335;

namespace FarmerLibrary
{
    public enum View { FullView, FarmView, CoopView, HouseView, RoadView, SeedShopView, ChickShopView }

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
                for (int i = 0; i < farm.Rows * farm.Cols; i++) // TODO enumerable
                    farm[i].Water();

            // TODO view the rain, there should be an active events list in state that the graphics can respond to
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
                for (int i = 0; i < farm.Rows * farm.Cols; i++) // TODO enumerable
                    if (rnd.NextDouble() < WormChance)
                        farm[i].GiveBug();
        }
    }

    #endregion

    public abstract class Tool
    {
        public void Use(GameState state, IToolAcceptor target)
        {
            if (!state.CanWork())
                return;

            bool expended = UseInternal(state, target);
            if (expended)
                state.DoLabor();
        }

        public abstract bool UseInternal(GameState state, IToolAcceptor target);
    }

    #region Tool classes

    public sealed class Hand : Tool
    {
        public override bool UseInternal(GameState state, IToolAcceptor target)
        {
            if (target is Plot plot)
            {
                Fruit? harvest = plot.Harvest();
                if (harvest is Fruit f)
                {
                    state.HeldProduct = f;
                    state.CurrentTool = null;
                    return true;
                }
            }

            if (target is EggSpot spot)
            {
                Egg? collected = spot.Collect();
                if (collected is Egg e)
                {
                    state.HeldProduct = e;
                    state.CurrentTool = null;
                    return true;
                }

            }

            return false;
        }
    }

    public sealed class Pail : Tool
    {
        public override bool UseInternal(GameState state, IToolAcceptor target)
        {
            if (target is Plot plot)
            {
                return plot.Water();
            }
            return false;
        }
    }

    public sealed class Bag : Tool
    {
        public override bool UseInternal(GameState state, IToolAcceptor target)
        {
            if (target is Plot plot)
            {
                return plot.Fertilize();
                // TODO subtract fertilizer
            }

            if (target is ChickenFeeder feeder)
            {
                return feeder.AddFeed();
                // TODO subtract feed
            }

            return false;
        }
    }

    public sealed class Bottle : Tool
    {
        public override bool UseInternal(GameState state, IToolAcceptor target)
        {
            if (target is Plot plot)
                return plot.BugSpray();
            return false;
        }
    }

    public sealed class Scythe : Tool
    {
        public override bool UseInternal(GameState state, IToolAcceptor target)
        {
            if (target is Plot plot)
                return plot.DestroyPlant();
            return false;
        }
    }

    #endregion

    public class GameState
    {
        public View CurrentView { get; set; }

        // Farm state
        private List<Farm> Farms = [];
        private uint CurrentFarmIndex = 0;
        public Farm CurrentFarm { get => Farms[(int)CurrentFarmIndex]; }
        public void SetFarm(uint index)
        {
            if (index >= Farms.Count)
                throw new IndexOutOfRangeException($"Can't select farm index {index}, there are only {Farms.Count} farms.");
            CurrentFarmIndex = index;
        }
        public List<Farm> GetFarmList() => new List<Farm>(Farms); 

        // Coop state
        // Note: this is made more generally than the actual app uses currently
        // for the sake of extensibility. We're actually only ever using one coop.
        public List<Coop> Coops = [];
        private uint CurrentCoopIndex = 0;
        public Coop CurrentCoop { get => Coops[(int)CurrentCoopIndex]; }
        public void SetCoop(uint index)
        {
            if (index >= Coops.Count)
                throw new IndexOutOfRangeException($"Can't select coop index {index}, there are only {Coops.Count} coops.");
            CurrentCoopIndex = index;
        }

        // Economy
        public uint PlayerMoney { get; private set; }
        private Dictionary<Type, int> OwnedAmounts = [];
        public bool Buy(IBuyable product)
        {
            // Can player afford the product?
            if (product.BuyPrice > PlayerMoney)
                return false;

            if (product is Chicken c)
            {
                // Can we buy more chickens?
                if (CurrentCoop.ChickenCount >= CurrentCoop.Capacity)
                    return false;
                CurrentCoop.AddChicken(c);
            }

            // Add product to player's inventory
            if (!OwnedAmounts.ContainsKey(product.GetType()))
                OwnedAmounts.Add(product.GetType(), 1);
            else
                OwnedAmounts[product.GetType()] += 1;

            // Take money
            PlayerMoney -= product.BuyPrice;

            return true;
        }
        public int GetOwnedAmount(IBuyable product)
        {
            if (OwnedAmounts.ContainsKey(product.GetType()))
                return OwnedAmounts[product.GetType()];
            return 0;
        }

        // Farming
        public bool PlantSeedToCurrent(Seed seed)
        {
            if (GetOwnedAmount(seed) == 0)
                return false;

            if (CurrentFarm.PlantASeed(seed))
            {
                OwnedAmounts[seed.GetType()] -= 1;
                return true;
            }
            return false;
        }
        public Tool? CurrentTool { get; set; } = null;
        public ISellable? HeldProduct { get; set; } = null;
        public bool SellHeld()
        {
            if (HeldProduct == null)
                return false;

            PlayerMoney += HeldProduct.SellPrice;
            HeldProduct = null;
            return true;
        }

        // Stamina
        public double Stamina { get; private set; } = 1;
        public readonly double STAMINA_STEP;
        public void DoLabor() => Stamina = Math.Max(0, Stamina - STAMINA_STEP);
        public bool CanWork() => Stamina >= STAMINA_STEP;
        
        // Events
        private DayEventHandler eventHandler = new();
        public List<DayEvent> TodaysEvents { get; private set; } = [];

        public void ResetTemps()
        {
            CurrentTool = null;
            HeldProduct = null;
            CurrentFarmIndex = 0;
            CurrentCoopIndex = 0;
        }

        public GameState(uint numFarms, uint farmRows, uint farmCols, uint numCoops, uint coopCapacity, View startView, uint playerMoney, uint actionsPerDay, double eventChance)
        {
            for (int i = 0; i < numFarms; i++)
            {
                Farms.Add(new Farm(farmRows, farmCols));
            }

            for (int i = 0; i < numCoops; i++)
            {
                Coops.Add(new Coop(coopCapacity));
            }

            CurrentView = startView;
            PlayerMoney = playerMoney;

            //TODO temp
            OwnedAmounts.Add(typeof(RaddishSeed), 5);
            OwnedAmounts.Add(typeof(CarrotSeed), 5);
            OwnedAmounts.Add(typeof(PotatoSeed), 5);
            OwnedAmounts.Add(typeof(TomatoSeed), 5);
            OwnedAmounts.Add(typeof(Chicken), 2);

            STAMINA_STEP = 1 / (double) actionsPerDay;

            eventHandler.AddEvent(new WormEvent(eventChance, 0.5));
            eventHandler.AddEvent(new RainEvent(eventChance));
        }

        public void EndDay()
        {
            foreach (var farm in Farms)
                farm.EndDay();
            
            foreach (var coop in Coops)
                coop.EndDay();

            Stamina = 1;

            TodaysEvents = eventHandler.TryEvents(this);
        }

        public static GameState GetClassicStartingState()
        {
            return new GameState(4, 3, 4, 1, 5, View.FullView, 1000, 160, 0.1);
        }
    }

    public abstract class GameObject
    {
        public virtual void EndDay() { }
    }

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
        public bool HasBug { get; private set; } = false;

        public GrowthState State { get; private set; } = GrowthState.Seed;
        public  bool Alive { get; private set; } = true;
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

    //TODO proper selling prices
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

    #region Interfaces   
    public interface ISellable
    {
        uint SellPrice { get; }
    }

    public interface IBuyable
    {
        uint BuyPrice { get; }
        string Name { get; }
    }

    public interface IToolAcceptor { }

    #endregion
}
