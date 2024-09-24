using FarmerLibrary;

namespace FarmerGraphics
{
    [System.Runtime.Versioning.SupportedOSPlatform("windows")]
    public abstract class SceneHandler : IDrawable
    {
        // Background that is drawn first
        protected Bitmap? Background { get; init; }

        // All controls that are displayed by the base class behavior
        protected List<IClickable> Clickables = [];

        // Event viualizer
        protected EventDisplay? EventDisplay;

        // Cursor
        protected CursorHandler Cursor = new();

        // Things to display on top of everything
        protected List<IDrawable> TopIcons = [];

        public virtual void Draw(Graphics g, GameState state, int absoluteWidth, int absoluteHeight)
        {
            // Background
            if (Background is Bitmap b)
                g.DrawImage(b, 0, 0, absoluteWidth, absoluteHeight);

            // Controls
            DrawClickables(g, state, absoluteWidth, absoluteHeight);

            // Event
            EventDisplay?.Draw(g, state, absoluteWidth, absoluteHeight);

            // Cursor
            Cursor.Draw(g, state, absoluteWidth, absoluteHeight);

            // Top icons
            DrawTopIcons(g, state, absoluteWidth, absoluteHeight);
        }

        public void AddTopIcon(IDrawable icon)
        {
            TopIcons.Add(icon);
        }

        public void SetEventDisplay(EventDisplay eventDisplay) => EventDisplay = eventDisplay;

        protected void DrawClickables(Graphics g, GameState state, int absolueWidth, int absoluteHeight)
        {
            foreach (IClickable clickable in Clickables)
                clickable.Draw(g, state, absolueWidth, absoluteHeight);
        }

        protected void DrawTopIcons(Graphics g, GameState state, int absoluteWidth, int absoluteHeight)
        {
            foreach (IDrawable item in TopIcons)
                item.Draw(g, state, absoluteWidth, absoluteHeight);
        }

        public virtual void HandleClick(double x, double y, GameState state)
        {
            foreach (IClickable clickable in Clickables)
            {
                clickable.Click(x, y, state);
            }
        }

        public virtual void HandleMouseMove(double x, double y, GameState state)
        {
            foreach (IClickable clickable in Clickables)
            {
                clickable.Hover(x, y, state);
            }

            Cursor.UpdatePosition(x, y);
        }
    }

    [System.Runtime.Versioning.SupportedOSPlatform("windows")]
    public class MainSceneHandler : SceneHandler
    {
        private List<SceneSwitchButton> farmMiniatures = new(4);

        private SceneSwitchButton ArrowButton;
        private SceneSwitchButton HouseButton;
        private SceneSwitchButton CoopButton;

        public MainSceneHandler()
        {
            // Load assets
            Background = new Bitmap("Assets\\Farmer-even.png");

            // Initialize farm interaction areas
            double[] XBounds = { 0.02, 0.455, 0.547, 0.98 };
            double[] YBounds = { 0.368, 0.66, 0.694, 0.984 };

            Bitmap farm = new Bitmap("Assets\\Farm-mini.png");
            farmMiniatures.Add(new SceneSwitchButton(farm, new ProportionalRectangle(XBounds[0], XBounds[1], YBounds[0], YBounds[1]), FarmerLibrary.View.FarmView));
            farmMiniatures.Add(new SceneSwitchButton(farm, new ProportionalRectangle(XBounds[2], XBounds[3], YBounds[0], YBounds[1]), FarmerLibrary.View.FarmView));
            farmMiniatures.Add(new SceneSwitchButton(farm, new ProportionalRectangle(XBounds[0], XBounds[1], YBounds[2], YBounds[3]), FarmerLibrary.View.FarmView));
            farmMiniatures.Add(new SceneSwitchButton(farm, new ProportionalRectangle(XBounds[2], XBounds[3], YBounds[2], YBounds[3]), FarmerLibrary.View.FarmView));

            foreach (var f in farmMiniatures)
            {
                f.HighlightOn = false;
                Clickables.Add(f);
            }

            // Initialize buttons
            ArrowButton = new SceneSwitchButton(new Bitmap("Assets\\ArrowMain.png"), new ProportionalRectangle(0.48, 0.52, 0.84, 0.99), FarmerLibrary.View.RoadView);
            ArrowButton.EnableStamina();
            Clickables.Add(ArrowButton);

            HouseButton = new SceneSwitchButton(new Bitmap("Assets\\Coop-button.png"), new ProportionalRectangle(0.77, 0.94, 0.13, 0.352), FarmerLibrary.View.CoopView);
            HouseButton.HighlightOn = false;
            Clickables.Add(HouseButton);

            CoopButton = new SceneSwitchButton(new Bitmap("Assets\\House-button.png"), new ProportionalRectangle(0.39, 0.61, 0.01, 0.352), FarmerLibrary.View.HouseView);
            CoopButton.HighlightOn = false;
            Clickables.Add(CoopButton);

        }
    }

    [System.Runtime.Versioning.SupportedOSPlatform("windows")]
    public class FarmSceneHandler : SceneHandler
    {
        // Loaders
        private ToolIconLoader ToolIconLoader = new();
        private PlantStatesLoader PlantAssets = new();
        private PlotStatesLoader PlotAssets;
        private SellableLoader FruitAssets = new();

        // Menus
        private ToolMenuHandler ToolMenu;
        private MenuHandler PlantMenu;
        private ProductTextDisplay Text;

        private FarmDisplay Farm;

        // Buttons
        private PlantMenuButton PlantMenuButton;
        private HarvestButton HarvestButton;
        private SceneSwitchButton BackButton;
        private MenuExitButton PlantMenuExitButton;
        private ToolExitButton ToolExitButton;

        public FarmSceneHandler()
        {
            // Load assets:
            Background = new Bitmap("Assets\\Farm.png");

            // Assets for toolbar
            ToolIconLoader.Add(typeof(Hand), new Bitmap("Assets\\Hand.png"));
            ToolIconLoader.Add(typeof(Pail), new Bitmap("Assets\\Pail.png"));
            ToolIconLoader.Add(typeof(Bag), new Bitmap("Assets\\Bag.png"));
            ToolIconLoader.Add(typeof(Bottle), new Bitmap("Assets\\Bottle.png"));
            ToolIconLoader.Add(typeof(Scythe), new Bitmap("Assets\\Scythe.png"));

            // Held fruit assets for harvesting
            FruitAssets.Add(typeof(RaddishFruit), new Bitmap("Assets\\Raddish.png"));
            FruitAssets.Add(typeof(CarrotFruit), new Bitmap("Assets\\Carrot.png"));
            FruitAssets.Add(typeof(PotatoFruit), new Bitmap("Assets\\Potato.png"));
            FruitAssets.Add(typeof(TomatoFruit), new Bitmap("Assets\\Tomato.png"));
            FruitAssets.Add(typeof(MelonFruit), new Bitmap("Assets\\Melon.png"));

            // Plant assets
            PlantAssets.Load(typeof(RaddishPlant),
                "Assets\\Seed.png",
                "Assets\\Small-seedling.png",
                "Assets\\Big-seedling.png",
                "Assets\\Adult-raddish.png",
                "Assets\\Fruiting-raddish.png"
            );
            PlantAssets.Load(typeof(CarrotPlant),
                "Assets\\Seed.png",
                "Assets\\Small-seedling.png",
                "Assets\\Big-seedling.png",
                "Assets\\Adult-carrot.png",
                "Assets\\Fruiting-carrot.png"
            );
            PlantAssets.Load(typeof(PotatoPlant),
                "Assets\\Seed.png",
                "Assets\\Small-seedling-multi.png",
                "Assets\\Big-seedling-multi.png",
                "Assets\\Adult-potato.png",
                "Assets\\Fruiting-potato.png"
            );
            PlantAssets.Load(typeof(TomatoPlant),
                "Assets\\Seed.png",
                "Assets\\Small-seedling-multi.png",
                "Assets\\Big-seedling-multi.png",
                "Assets\\Adult-tomato.png",
                "Assets\\Fruiting-tomato.png"
            );
            PlantAssets.Load(typeof(MelonPlant),
                "Assets\\Seed-support.png",
                "Assets\\Small-seedling-support.png",
                "Assets\\Big-seedling-support.png",
                "Assets\\Adult-melon.png",
                "Assets\\Fruiting-melon.png"
            );

            // Plot assets
            PlotAssets = new PlotStatesLoader(new Bitmap("Assets\\Plots-default.png"),
                                              new Bitmap("Assets\\Plot-watered-center.png"),
                                              new Bitmap("Assets\\Plot-highlighted-center.png"),
                                              new Bitmap("Assets\\Plot-both.png"));

            // Named assets

            // Initialize cursor handler with icons
            Cursor.SetToolIcons(ToolIconLoader);
            Cursor.SetSellableIcons(FruitAssets);

            // Initialize menus:
            // Toolbar
            ToolMenu = new ToolMenuHandler(new Bitmap("Assets\\Toolbar.png"), new ProportionalRectangle(0.31, 0.97, 0.82, 0.98));
            ToolMenu.Add(new ToolButton(ToolIconLoader.GetImage(typeof(Hand)), new Hand()));
            ToolMenu.Add(new ToolButton(ToolIconLoader.GetImage(typeof(Pail)), new Pail()));
            ToolMenu.Add(new ToolButton(ToolIconLoader.GetImage(typeof(Bag)), new Bag()));
            ToolMenu.Add(new ToolButton(ToolIconLoader.GetImage(typeof(Bottle)), new Bottle()));
            ToolMenu.Add(new ToolButton(ToolIconLoader.GetImage(typeof(Scythe)), new Scythe()));

            ToolMenu.RepositionButtons(0.08, 0.14, 0.02, 0.01);

            ToolExitButton = new ToolExitButton(ToolMenu, new Bitmap("Assets\\Exit.png"), new ProportionalRectangle(0.87, 0.97, 0.81, 0.99));
            ToolExitButton.Disable();
            ToolMenu.SetExitButton(ToolExitButton);

            // Planting menu
            Text = new ProductTextDisplay(new Bitmap("Assets\\Plant-text.png"), new ProportionalRectangle(0.22, 0.79, 0.71, 0.86));
            Text.ShowPrice = false;

            PlantMenu = new MenuHandler(new Bitmap("Assets\\Center-menu.png"), new ProportionalRectangle(0.16, 0.84, 0.10, 0.90));
            AddSeed(FruitAssets.GetImage(typeof(RaddishFruit)), new RaddishSeed());
            AddSeed(FruitAssets.GetImage(typeof(CarrotFruit)), new CarrotSeed());
            AddSeed(FruitAssets.GetImage(typeof(TomatoFruit)), new TomatoSeed());
            AddSeed(FruitAssets.GetImage(typeof(PotatoFruit)), new PotatoSeed());
            AddSeed(FruitAssets.GetImage(typeof(MelonFruit)), new MelonSeed());
            PlantMenu.SetTextDisplay(Text);

            // Initialize buttons:
            HarvestButton = new HarvestButton(new Bitmap("Assets\\Harvest-house.png"), new ProportionalRectangle(0.04, 0.18, 0.59, 0.91));
            BackButton = new SceneSwitchButton(new Bitmap("Assets\\Back-arrow.png"), new ProportionalRectangle(0.88, 0.965, 0.82, 0.975), FarmerLibrary.View.FullView);
            PlantMenuButton = new PlantMenuButton(new Bitmap("Assets\\Planting-plant.png"), new ProportionalRectangle(0.01, 0.20, 0.18, 0.44), PlantMenu);
            PlantMenuExitButton = new MenuExitButton(PlantMenu, new Bitmap("Assets\\Exit.png"), new ProportionalRectangle(0.82, 0.91, 0.1, 0.25));
            PlantMenu.SetExitButton(PlantMenuExitButton);

            PlantMenu.Disable();

            // Initialize farm plots:
            var plotCoords = new ProportionalRectangle[3, 4];
            double[] XBounds = [0.209, 0.395, 0.585, 0.773, 0.96];
            double[] YBounds = [0.07, 0.315, 0.57, 0.81];
            for (int i = 0; i < plotCoords.GetLength(0); i++)
            {
                for (int j = 0; j < plotCoords.GetLength(1); j++)
                {
                    plotCoords[i, j] = new ProportionalRectangle(XBounds[j], XBounds[j + 1], YBounds[i], YBounds[i + 1]);
                }
            }

            Farm = new FarmDisplay(PlantAssets, PlotAssets, new Bitmap("Assets\\Worm.png"), new Bitmap("Assets\\Dead.png"), plotCoords);

            // Handle enable/disable
            PlantMenuButton.ToDisable.Add(Farm);
            PlantMenuButton.ToDisable.Add(ToolMenu);
            PlantMenuButton.ToDisable.Add(BackButton);
            PlantMenu.AddToEnable(Farm);
            PlantMenu.AddToEnable(ToolMenu);
            PlantMenu.AddToEnable(BackButton);
            PlantMenuExitButton.ToEnable.Add(Farm);
            PlantMenuExitButton.ToEnable.Add(ToolMenu);
            PlantMenuExitButton.ToEnable.Add(BackButton);

            ToolExitButton.ToEnable.Add(BackButton);
            ToolMenu.AddToDisable(BackButton);

            HarvestButton.ToEnable.Add(ToolMenu);
            HarvestButton.ToEnable.Add(BackButton);
            HarvestButton.ToDisable.Add(ToolExitButton);

            // Add controls to clickable list
            // (not Farm, because that is handled separately)
            Clickables.Add(HarvestButton);
            Clickables.Add(PlantMenuButton);
            Clickables.Add(ToolMenu);
            Clickables.Add(PlantMenu);
            Clickables.Add(BackButton);
            Clickables.Add(ToolExitButton);
        }

        // Custom override due to needing to draw background over the plots
        public override void Draw(Graphics g, GameState state, int absoluteWidth, int absoluteHeight)
        {
            // Handle control visibility
            if (!state.CurrentFarm.Planted)
                PlantMenuButton.Enable();
            else
            {
                PlantMenuButton.Disable();
                PlantMenu.Disable();
            }

            if (state.HeldProduct is Fruit)
            {
                PlantMenuButton.Disable();
                HarvestButton.Enable();
                ToolExitButton.Disable();
            }
            else
                HarvestButton.Disable();

            // Draw plots
            Farm.Draw(g, state, absoluteWidth, absoluteHeight);

            base.Draw(g, state, absoluteWidth, absoluteHeight);

        }

        public override void HandleClick(double x, double y, GameState state)
        {
            base.HandleClick(x, y, state);
            Farm.Click(x, y, state);
        }

        public override void HandleMouseMove(double x, double y, GameState state)
        {
            base.HandleMouseMove(x, y, state);
            Farm.Hover(x, y, state);
        }

        private void AddSeed(Bitmap icon, Seed seed)
        {
            PlantButton btn = new PlantButton(icon, seed);
            btn.SetTextDisplay(Text);
            PlantMenu.Add(btn);
            PlantMenu.RepositionButtons(0.10, 0.18, 0.03, 0.07);
        }
    }

    [System.Runtime.Versioning.SupportedOSPlatform("windows")]
    public class RoadSceneHandler : SceneHandler
    {
        private SceneSwitchButton SeedShop, ChickShop;
        public RoadSceneHandler()
        {
            // Load assets
            Background = new Bitmap("Assets\\Shops-background.png");

            SeedShop = new SceneSwitchButton(new Bitmap("Assets\\ShopHouse.png"), new ProportionalRectangle(0.06, 0.37, 0.09, 0.79), FarmerLibrary.View.SeedShopView);
            SeedShop.HighlightOn = false;
            ChickShop = new SceneSwitchButton(new Bitmap("Assets\\ShopHouse.png"), new ProportionalRectangle(0.63, 0.94, 0.09, 0.79), FarmerLibrary.View.ChickShopView);
            ChickShop.HighlightOn = false;

            Clickables.Add(SeedShop);
            Clickables.Add(ChickShop);
            Clickables.Add(new SceneSwitchButton(new Bitmap("Assets\\Arrow-shops.png"), new ProportionalRectangle(0.45, 0.56, 0.07, 0.33), FarmerLibrary.View.FullView));

            TopIcons.Add(new BasicTextDisplay("Seed shop", new Bitmap("Assets\\Shop-sign.png"), new ProportionalRectangle(0.11, 0.32, 0.42, 0.55)));
            TopIcons.Add(new BasicTextDisplay("Chicken shop", new Bitmap("Assets\\Shop-sign.png"), new ProportionalRectangle(0.68, 0.89, 0.42, 0.55)));
        }
    }

    [System.Runtime.Versioning.SupportedOSPlatform("windows")]
    public class CoopSceneHandler : SceneHandler
    {
        // Loaders
        private ToolIconLoader ToolIconLoader = new();
        private SellableLoader EggAssets = new();

        // Chicken and egg rendering
        private Bitmap Chicken;
        private List<ProportionalRectangle> ChickenPositions = new();
        private List<EggButton> EggSpots = new();

        // Menus
        private ToolMenuHandler ToolMenu;
        private ToolExitButton ExitButton;
        private HarvestButton HarvestHouse;
        private SceneSwitchButton BackButton;

        Random rnd = new();

        public CoopSceneHandler()
        {
            // Load Assets
            Background = new Bitmap("Assets\\Coop-background.png");

            ToolIconLoader.Add(typeof(Hand), new Bitmap("Assets\\Hand.png"));
            ToolIconLoader.Add(typeof(Bag), new Bitmap("Assets\\Bag.png"));

            EggAssets.Add(typeof(Egg), new Bitmap("Assets\\Egg.png"));

            Chicken = new Bitmap("Assets\\Chicken.png");

            // Initialize cursor handler with icons
            Cursor.SetToolIcons(ToolIconLoader);
            Cursor.SetSellableIcons(EggAssets);

            // Menu
            ToolMenu = new ToolMenuHandler(new Bitmap("Assets\\Coop-menu.png"), new ProportionalRectangle(0.32, 0.59, 0.11, 0.27));
            ToolMenu.Add(new ToolButton(ToolIconLoader.GetImage(typeof(Hand)), new Hand()));
            ToolMenu.Add(new ToolButton(ToolIconLoader.GetImage(typeof(Bag)), new Bag()));

            ToolMenu.RepositionButtons(0.069, 0.12, 0.02, 0.01);

            ExitButton = new ToolExitButton(ToolMenu, new Bitmap("Assets\\Exit.png"), new ProportionalRectangle(0.87, 0.97, 0.81, 0.99));
            ExitButton.Disable();
            ToolMenu.SetExitButton(ExitButton);

            // Controls
            BackButton = new SceneSwitchButton(new Bitmap("Assets\\Back-arrow.png"), new ProportionalRectangle(0.88, 0.965, 0.82, 0.975), FarmerLibrary.View.FullView);
            Clickables.Add(BackButton);
            Clickables.Add(new FeederDisplay(new Bitmap("Assets\\Feeder.png"),
                                             new Bitmap("Assets\\Feed.png"),
                                             new ProportionalRectangle(0.24, 0.64, 0.4, 0.72)));
            Clickables.Add(ToolMenu);

            HarvestHouse = new HarvestButton(new Bitmap("Assets\\Harvest-house.png"), new ProportionalRectangle(0.04, 0.18, 0.59, 0.91));
            Clickables.Add(HarvestHouse);
            Clickables.Add(ExitButton);

            ExitButton.ToEnable.Add(BackButton);
            ToolMenu.AddToDisable(BackButton);
            HarvestHouse.ToEnable.Add(BackButton);
            HarvestHouse.ToEnable.Add(ToolMenu);
        }

        public override void Draw(Graphics g, GameState state, int absoluteWidth, int absoluteHeight)
        {
            // Determine harvest house visibility
            if (state.HeldProduct is Egg)
            {
                HarvestHouse.Enable();
                ExitButton.Disable();
            }
            else
            {
                HarvestHouse.Disable();
            }

            // Initialize new chicken and egg positions if needed
            for (int i = ChickenPositions.Count; i < state.CurrentCoop.ChickenCount; i++)
            {
                ChickenPositions.Add(GetNewPosition());
                // Sort by Y position, so that higher chickens are further back
                ChickenPositions.Sort(Comparer<ProportionalRectangle>.Create((p1, p2) => p1.Y1.CompareTo(p2.Y1)));

                EggSpots.Add(new EggButton(EggAssets.GetImage(typeof(Egg)), GetNewPosition(), state.CurrentCoop.GetEggSpots()[i]));
                Clickables.Add(EggSpots[i]);
            }

            base.Draw(g, state, absoluteWidth, absoluteHeight);

            // Draw chicken
            for (int i = 0; i < state.CurrentCoop.ChickenCount; i++)
            {
                g.DrawImage(Chicken, ChickenPositions[i].GetAbsolute(absoluteWidth, absoluteHeight));
            }

            // Reraw cursor so that it is drawn over the chickens
            Cursor.Draw(g, state, absoluteWidth, absoluteHeight);
        }

        private ProportionalRectangle GetNewPosition()
        {
            var x1 = rnd.NextDouble() * 0.79;
            var x2 = x1 + 0.11;
            var y1 = 1 - rnd.NextDouble() * 0.3 - 0.2;
            var y2 = y1 + 0.2;

            return new ProportionalRectangle(x1, x2, y1, y2);
        }
    }

    [System.Runtime.Versioning.SupportedOSPlatform("windows")]
    public class ShopSceneHandler : SceneHandler
    {
        private MenuHandler ShoppingMenu;
        private ProductTextDisplay Text;

        public ShopSceneHandler()
        {
            Background = new Bitmap("Assets\\Shop.png");

            ShoppingMenu = new MenuHandler(new Bitmap("Assets\\Shop-menu.png"), new ProportionalRectangle(0.06, 0.69, 0.13, 0.87));

            Clickables.Add(new SceneSwitchButton(new Bitmap("Assets\\Arrow-shop.png"), new ProportionalRectangle(0.79, 0.98, 0.01, 0.18), FarmerLibrary.View.RoadView));
            Clickables.Add(ShoppingMenu);

            Text = new ProductTextDisplay(new Bitmap("Assets\\Text-display.png"), new ProportionalRectangle(0.11, 0.65, 0.66, 0.8));

            ShoppingMenu.SetTextDisplay(Text);
        }

        public void AddStock(IBuyable item, Bitmap icon)
        {
            var BuyButton = new BuyButton(icon, item);
            BuyButton.SetProductDisplay(Text);
            ShoppingMenu.Add(BuyButton);
            ShoppingMenu.RepositionButtons(0.11, 0.2, 0.01, 0.04);
        }
    }

    [System.Runtime.Versioning.SupportedOSPlatform("windows")]
    public class HouseSceneHandler : SceneHandler
    {
        private ChallengeBoard Board;
        public HouseSceneHandler()
        {
            Background = new Bitmap("Assets\\House.png");

            Clickables.Add(new SceneSwitchButton(new Bitmap("Assets\\Back-arrow.png"), new ProportionalRectangle(0.88, 0.965, 0.82, 0.975), FarmerLibrary.View.FullView));
            Clickables.Add(new NewDayButton(new Bitmap("Assets\\New-day.png"), new ProportionalRectangle(0.02, 0.14, 0.12, 0.35)));

            TopIcons.Add(new PointsDisplay(new Bitmap("Assets\\Money.png"), new ProportionalRectangle(0.01, 0.14, 0.8, 0.91)));

            Board = new ChallengeBoard(new Bitmap("Assets\\Center-menu.png"), new Bitmap("Assets\\Challenge.png"), new ProportionalRectangle(0.16, 0.84, 0.1, 0.9), 3, 0.05);
            TopIcons.Add(Board);
        }
    }
}
