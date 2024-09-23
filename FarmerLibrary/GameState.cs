namespace FarmerLibrary
{
    public enum View { FullView, FarmView, CoopView, HouseView, RoadView, SeedShopView, ChickShopView }


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
