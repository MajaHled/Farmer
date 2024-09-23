using FarmerLibrary;

namespace FarmerGraphics
{
    public struct ProportionalRectangle
    {
        public double X1 { get; init; }
        public double X2 { get; init; }
        public double Y1 { get; init; }
        public double Y2 { get; init; }

        public ProportionalRectangle(double X1, double X2, double Y1, double Y2)
        {
            this.X1 = X1;
            this.X2 = X2;
            this.Y1 = Y1;
            this.Y2 = Y2;
        }

        public bool InArea(double x, double y) => x > X1 && y > Y1 && x < X2 && y < Y2;
        public Rectangle GetAbsolute(int canvasWidth, int canvasHeight) => new Rectangle(
            (int)(canvasWidth * X1),
            (int)(canvasHeight * Y1),
            GetAbsoluteWidth(canvasWidth),
            GetAbsoluteHeight(canvasHeight)
        );

        public int GetAbsoluteWidth(int canvasWidth) => (int)(canvasWidth * (X2 - X1));
        public int GetAbsoluteHeight(int canvasHeight) => (int)(canvasHeight * (Y2 - Y1));
    }

    [System.Runtime.Versioning.SupportedOSPlatform("windows")] //Windows only due to Bitmap
    public class FarmerGraphics
    {
        private GameState gameState;

        // Individual scene handlers
        private SceneHandler MainScene = new MainSceneHandler();
        private SceneHandler FarmScene = new FarmSceneHandler();
        private SceneHandler RoadScene = new RoadSceneHandler();
        private SceneHandler CoopScene = new CoopSceneHandler();
        private ShopSceneHandler SeedShopScene = new ShopSceneHandler();
        private ShopSceneHandler ChickShopScene = new ShopSceneHandler();
        private SceneHandler HouseScene = new HouseSceneHandler();

        private MoneyDisplay Money;
        private StaminaDisplay Stamina;
        private EventDisplay EventDisplay;

        public int Width { get; set; }
        public int Height { get; set; }

        public FarmerGraphics(GameState gameState, int startWidth, int startHeight)
        {
            this.gameState = gameState;

            Width = startWidth;
            Height = startHeight;

            SeedShopScene.AddStock(new RaddishSeed(), new Bitmap("Assets\\Raddish.png"));
            SeedShopScene.AddStock(new CarrotSeed(), new Bitmap("Assets\\Carrot.png"));
            SeedShopScene.AddStock(new PotatoSeed(), new Bitmap("Assets\\Potato.png"));
            SeedShopScene.AddStock(new TomatoSeed(), new Bitmap("Assets\\Tomato.png"));
            //SeedShopScene.AddStock(new MelonSeed(), new Bitmap("Assets\\Melon.png"));

            ChickShopScene.AddStock(new Chicken(), new Bitmap("Assets\\Chicken.png"));
            //ChickShopScene.AddStock(new Bag(), new Bitmap("Assets\\Bag.png"));

            Money = new MoneyDisplay(new Bitmap("Assets\\Money.png"), new ProportionalRectangle(0.01, 0.14, 0.02, 0.13));
            Stamina = new StaminaDisplay(new ProportionalRectangle(0.9, 0.98, 0.02, 0.15),
                                         new Bitmap("Assets\\Stamina-background.png"),
                                         new Bitmap("Assets\\Stamina-level.png"),
                                         new Bitmap("Assets\\Stamina-empty.png"),
                                         new Bitmap("Assets\\Stamina-top.png"));

            // Add top icons to appropriate scenes
            MainScene.AddTopIcon(Money);
            MainScene.AddTopIcon(Stamina);
            FarmScene.AddTopIcon(Money);
            FarmScene.AddTopIcon(Stamina);
            HouseScene.AddTopIcon(Money);
            HouseScene.AddTopIcon(Stamina);
            RoadScene.AddTopIcon(Money);
            RoadScene.AddTopIcon(Stamina);
            CoopScene.AddTopIcon(Money);
            CoopScene.AddTopIcon(Stamina);
            ChickShopScene.AddTopIcon(Money);
            SeedShopScene.AddTopIcon(Money);

            // Add event display to appropriate scenes
            EventDisplay = new();
            EventDisplay.RegisterEvent(typeof(WormEvent), new Bitmap("Assets//Worms.png"));
            EventDisplay.RegisterEvent(typeof(RainEvent), new Bitmap("Assets//Rain.png"));

            MainScene.SetEventDisplay(EventDisplay);
            FarmScene.SetEventDisplay(EventDisplay);
        }

        public void Paint(Graphics g)
        {
            switch (gameState.CurrentView)
            {
                case FarmerLibrary.View.FullView:
                    MainScene.Draw(g, gameState, Width, Height);
                    break;
                case FarmerLibrary.View.FarmView:
                    FarmScene.Draw(g, gameState, Width, Height);
                    break;
                case FarmerLibrary.View.CoopView:
                    CoopScene.Draw(g, gameState, Width, Height);
                    break;
                case FarmerLibrary.View.HouseView:
                    HouseScene.Draw(g, gameState, Width, Height);
                    break;
                case FarmerLibrary.View.RoadView:
                    RoadScene.Draw(g, gameState, Width, Height);
                    break;
                case FarmerLibrary.View.SeedShopView:
                    SeedShopScene.Draw(g, gameState, Width, Height);
                    break;
                case FarmerLibrary.View.ChickShopView:
                    ChickShopScene.Draw(g, gameState, Width, Height);
                    break;
                default:
                    break;
            }
        }

        public void HandleClick(int X, int Y)
        {
            double XProportional = (double)X / Width;
            double YProportional = (double)Y / Height;

            switch (gameState.CurrentView)
            {
                case FarmerLibrary.View.FullView:
                    MainScene.HandleClick(XProportional, YProportional, gameState);
                    break;
                case FarmerLibrary.View.FarmView:
                    FarmScene.HandleClick(XProportional, YProportional, gameState);
                    break;
                case FarmerLibrary.View.CoopView:
                    CoopScene.HandleClick(XProportional, YProportional, gameState);
                    break;
                case FarmerLibrary.View.HouseView:
                    HouseScene.HandleClick(XProportional, YProportional, gameState);
                    break;
                case FarmerLibrary.View.RoadView:
                    RoadScene.HandleClick(XProportional, YProportional, gameState);
                    break;
                case FarmerLibrary.View.SeedShopView:
                    SeedShopScene.HandleClick(XProportional, YProportional, gameState);
                    break;
                case FarmerLibrary.View.ChickShopView:
                    ChickShopScene.HandleClick(XProportional, YProportional, gameState);
                    break;
                default:
                    break;
            }
        }

        public void HandleMouseMove(int X, int Y)
        {
            double XProportional = (double)X / Width;
            double YProportional = (double)Y / Height;

            switch (gameState.CurrentView)
            {
                case FarmerLibrary.View.FullView:
                    MainScene.HandleMouseMove(XProportional, YProportional, gameState);
                    break;
                case FarmerLibrary.View.FarmView:
                    FarmScene.HandleMouseMove(XProportional, YProportional, gameState);
                    break;
                case FarmerLibrary.View.CoopView:
                    CoopScene.HandleMouseMove(XProportional, YProportional, gameState);
                    break;
                case FarmerLibrary.View.HouseView:
                    HouseScene.HandleMouseMove(XProportional, YProportional, gameState);
                    break;
                case FarmerLibrary.View.RoadView:
                    RoadScene.HandleMouseMove(XProportional, YProportional, gameState);
                    break;
                case FarmerLibrary.View.SeedShopView:
                    SeedShopScene.HandleMouseMove(XProportional, YProportional, gameState);
                    break;
                case FarmerLibrary.View.ChickShopView:
                    ChickShopScene.HandleMouseMove(XProportional, YProportional, gameState);
                    break;
                default:
                    break;
            }
        }
    }
}

// TODO plan:
// challenges
// housekeeping (TODOs)
// Docs
// Presentation
