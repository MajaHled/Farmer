using System.Drawing;

namespace FarmerLibrary
{
    #region loaders
    [System.Runtime.Versioning.SupportedOSPlatform("windows")] //Windows only due to Bitmap,
                                                               //TODO add to appropriate places
    public class NamedAssetsLoader
    {
        private Dictionary<string, Bitmap> LoadedAssets = [];

        public void Load(string name, string path)
        {
            var image = new Bitmap(path);
            LoadedAssets.Add(name, image);
        }

        public Bitmap this [string index]
        {
            get => LoadedAssets[index];
        }
    }

    [System.Runtime.Versioning.SupportedOSPlatform("windows")]
    public class PlantStatesLoader
    {
        private Dictionary<Type, Dictionary<GrowthState, Bitmap>> LoadedAssets = [];

        public void Load(Type type, string seedPath, string smallSeedlingPath, string bigSeedlingPath, string adultPath, string fruitingPath)
        {
            if (!typeof(Plant).IsAssignableFrom(type))
                throw new ArgumentException($"Type {type} is not a Plant type.");

            if (LoadedAssets.ContainsKey(type))
                throw new ArgumentException($"Assets for {type} already loaded.");

            Dictionary<GrowthState, Bitmap> loaded = [];
            loaded.Add(GrowthState.Seed, new Bitmap(seedPath));
            loaded.Add(GrowthState.SmallSeedling, new Bitmap(smallSeedlingPath));
            loaded.Add(GrowthState.BigSeedling, new Bitmap(bigSeedlingPath));
            loaded.Add(GrowthState.Adult, new Bitmap(adultPath));
            loaded.Add(GrowthState.Fruiting, new Bitmap(fruitingPath));

            LoadedAssets.Add(type, loaded);
        }

        public Bitmap GetImage(Type type, GrowthState state)
        {
            if (!LoadedAssets.ContainsKey(type))
                throw new ArgumentException($"Assets for type {type} not loaded.");
            return LoadedAssets[type][state];
        }

    }
    public class ToolIconLoader
    {
        private Dictionary<Type, Bitmap> LoadedAssets = [];

        public void Add(Type type, Bitmap bitmap)
        {
            if (!typeof(Tool).IsAssignableFrom(type))
                throw new ArgumentException($"Type {type} is not a Tool type.");

            LoadedAssets.Add(type, bitmap);
        }

        public Bitmap GetImage(Type type)
        {
            if (!LoadedAssets.ContainsKey(type))
                throw new ArgumentException($"Image for type {type} not loaded.");
            return LoadedAssets[type];
        }
    }
    #endregion

    #region clicking
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

    interface IClickable
    {
        public void Click(double x, double y, GameState state);
        public void DrawSelf(Graphics g, int width, int height);
        public void Disable();
        public void Enable();
        public bool Enabled { get; }
    }

    public abstract class GameButton : IClickable
    {
        protected Bitmap Icon;
        protected ProportionalRectangle? Position;

        public bool Enabled { get; private set; }

        public GameButton(Bitmap icon, ProportionalRectangle position)
        {
            Icon = icon;
            Position = position;
            Enabled = true;
        }

        public GameButton(Bitmap icon)
        {
            Icon = icon;
            Position = null;
            Enabled = true;
        }

        public void SetPosition(ProportionalRectangle position) => Position = position;

        public void UnsetPosition() => Position = null;

        public void Disable() => Enabled = false;

        public void Enable() => Enabled = true; 

        public void DrawSelf(Graphics g, int width, int height)
        {
            if (Position is null)
                throw new InvalidOperationException("Cannot draw button with uninitialized position.");
            else if (Position is ProportionalRectangle p && Enabled)
                g.DrawImage(Icon, p.GetAbsolute(width, height));
        }

        public void Click(double x, double y, GameState state)
        {
            if (Enabled && Position is ProportionalRectangle p && p.InArea(x, y))
            {
                Action(state);
            }
        }

        protected abstract void Action(GameState state);
    }

    public sealed class PlantButton : GameButton
    {
        public PlantButton(Bitmap icon, ProportionalRectangle position) : base(icon, position) { }

        protected override void Action(GameState state)
        {
            state.CurrentFarm.PlantASeed(new RaddishSeed()); //TODO different seeds
            Disable();
        }
    }

    public sealed class HarvestHouseButton : GameButton
    {
        public HarvestHouseButton(Bitmap icon, ProportionalRectangle position) : base(icon, position) { }

        protected override void Action(GameState state)
        {
            state.SellHeld();
        }
    }

    public sealed class ArrowButton : GameButton
    {
        private View Destination;
        public ArrowButton(Bitmap icon, ProportionalRectangle position, View destination) : base(icon, position)
        {
            Destination = destination;
        }

        protected override void Action(GameState state)
        {
            state.CurrentView = Destination;
            state.ResetTemps();
        }
    }

    public sealed class ToolButton : GameButton
    {
        private Tool? Tool;

        public ToolButton(Bitmap icon, ProportionalRectangle position, Tool? tool) : base(icon, position)
        {
            Tool = tool;
        }

        protected override void Action(GameState state)
        {
            state.CurrentTool = Tool;
        }
    }

    public abstract class BuyButton : GameButton
    {
        private IBuyable Product;
        public BuyButton(Bitmap icon, ProportionalRectangle position, IBuyable product) : base(icon, position)
        {
            Product = product;
        }

        protected override void Action(GameState state)
        {
            throw new NotImplementedException();
        }
        //TODO
    }

    [System.Runtime.Versioning.SupportedOSPlatform("windows")]
    public class MenuHandler : IClickable
    {
        private List<GameButton> Buttons = [];
        private Bitmap Background;
        public ProportionalRectangle BackgroundPosition { get; init; }

        public bool Enabled { get; private set; }

        public MenuHandler(Bitmap background, ProportionalRectangle backgroundPosition)
        {
            Background = background;
            BackgroundPosition = backgroundPosition;
        }

        public void Add(GameButton button)
        {
            Buttons.Add(button);
        }

        public void RepositionButtons(double width, double height, double gap, double edgeGap)
        {
            var startX = BackgroundPosition.X1 + edgeGap;
            var startY = BackgroundPosition.Y1 + edgeGap;

            // If requested width of buttons doesn't allow for even one column
            if (startX + width + edgeGap > BackgroundPosition.X2)
                throw new ArgumentException("Buttons do not fit with specified proportions");

            // If requested height of buttons doesn't allow for even one line
            if (startY + height + edgeGap > BackgroundPosition.Y2)
                throw new ArgumentException("Buttons do not fit with specified proportions");

            foreach (GameButton button in Buttons)
            {
                if (!button.Enabled)
                    continue;

                button.SetPosition(new ProportionalRectangle(startX, startX + width, startY, startY + height));
                startX += width + gap;

                if (startX + width + edgeGap > BackgroundPosition.X2)
                {
                    // new line
                    startX = BackgroundPosition.X1 + gap;
                    startY += height + gap;

                    // If can't fit another line
                    if (startY + height + edgeGap > BackgroundPosition.Y2)
                        throw new ArgumentException("Buttons do not fit with specified proportions");
                }
            }
        }

        public void Click(double x, double y, GameState state)
        {
            foreach (GameButton button in Buttons)
                button.Click(x, y, state);
        }

        public void DrawSelf(Graphics g, int width, int height)
        {
            g.DrawImage(Background, BackgroundPosition.GetAbsolute(width, height));
            foreach (GameButton button in Buttons)
                button.DrawSelf(g, width, height);

        }

        public void Disable()
        {
            Enabled = false;
            foreach (GameButton button in Buttons)
                button.Disable();
        }

        public void Enable()
        {
            Enabled |= true;
            foreach (GameButton button in Buttons)
                button.Enable();
        }
    }
    #endregion


    [System.Runtime.Versioning.SupportedOSPlatform("windows")] //Windows only due to Bitmap
    public class FarmerSceneHandler
    {
        private GameState gameState;
        private NamedAssetsLoader assetLoader;
        private PlantStatesLoader plantAssets;

        private int width, height;

        private List<ProportionalRectangle> farmCoords;

        private ProportionalRectangle[,] plotCoords;

        private ToolIconLoader toolIconLoader;

        // Buttons
        // TODO encapsulate scenes
        private GameButton PlantButton; 
        private GameButton HarvestButton;
        private GameButton BackButton;

        private MenuHandler ToolMenu;
        private MenuHandler PlantMenu;

        public FarmerSceneHandler(GameState gameState)
        {
            this.gameState = gameState;

            assetLoader = new NamedAssetsLoader();
            assetLoader.Load("Background", "C:\\Users\\Marie Hledíková\\OneDrive\\Pictures\\Farmer-even.png");
            width = 960;
            height = 540;
            //TODO better resize handling

            assetLoader.Load("Plant", "C:\\Users\\Marie Hledíková\\OneDrive\\Pictures\\Raddish-placeholder.png");

            assetLoader.Load("Plot-background", "C:\\Users\\Marie Hledíková\\OneDrive\\Pictures\\Farm.png");
            assetLoader.Load("Plot-watered", "C:\\Users\\Marie Hledíková\\OneDrive\\Pictures\\Plot-watered-center.png");
            assetLoader.Load("Plot-highlighted", "C:\\Users\\Marie Hledíková\\OneDrive\\Pictures\\Plot-highlighted-center.png");
            assetLoader.Load("Plot-both", "C:\\Users\\Marie Hledíková\\OneDrive\\Pictures\\Plot-both.png");
            assetLoader.Load("Plots-default", "C:\\Users\\Marie Hledíková\\OneDrive\\Pictures\\Plots-default.png");

            assetLoader.Load("Harvest-house", "C:\\Users\\Marie Hledíková\\OneDrive\\Pictures\\Harvest-house.png");
            assetLoader.Load("Planting-plant", "C:\\Users\\Marie Hledíková\\OneDrive\\Pictures\\Planting-plant.png");

            assetLoader.Load("Worm", "C:\\Users\\Marie Hledíková\\OneDrive\\Pictures\\Worm.png");
            assetLoader.Load("Dead", "C:\\Users\\Marie Hledíková\\OneDrive\\Pictures\\Dead.png");

            // Buttons and menus
            PlantButton = new PlantButton(assetLoader["Planting-plant"], new ProportionalRectangle(0.01, 0.20, 0.18, 0.44));
            HarvestButton = new HarvestHouseButton(assetLoader["Harvest-house"], new ProportionalRectangle(0.04, 0.18, 0.59, 0.91));
            BackButton = new ArrowButton(new Bitmap("C:\\Users\\Marie Hledíková\\OneDrive\\Pictures\\Back-arrow.png"), new ProportionalRectangle(0.88, 0.965, 0.82, 0.975), View.FullView);

            //TODO held interface instead of just tool, do it for fruits also
            toolIconLoader = new ToolIconLoader();
            toolIconLoader.Add(typeof(Hand),new Bitmap("C:\\Users\\Marie Hledíková\\OneDrive\\Pictures\\Hand.png"));
            toolIconLoader.Add(typeof(Pail), new Bitmap("C:\\Users\\Marie Hledíková\\OneDrive\\Pictures\\Pail.png"));
            toolIconLoader.Add(typeof(Bag), new Bitmap("C:\\Users\\Marie Hledíková\\OneDrive\\Pictures\\Bag.png"));
            toolIconLoader.Add(typeof(Bottle), new Bitmap("C:\\Users\\Marie Hledíková\\OneDrive\\Pictures\\Bottle.png"));
            toolIconLoader.Add(typeof(Scythe), new Bitmap("C:\\Users\\Marie Hledíková\\OneDrive\\Pictures\\Scythe.png"));

            //TODO proportions to file???
            ToolMenu = new MenuHandler(new Bitmap("C:\\Users\\Marie Hledíková\\OneDrive\\Pictures\\Toolbar.png"), new ProportionalRectangle(0.31, 0.97, 0.82, 0.98));
            ToolMenu.Add(new ToolButton(toolIconLoader.GetImage(typeof(Hand)), new ProportionalRectangle(0.32, 0.40, 0.82, 0.97), new Hand()));
            ToolMenu.Add(new ToolButton(toolIconLoader.GetImage(typeof(Pail)), new ProportionalRectangle(0.43, 0.51, 0.82, 0.97), new Pail()));
            ToolMenu.Add(new ToolButton(toolIconLoader.GetImage(typeof(Bag)), new ProportionalRectangle(0.54, 0.62, 0.82, 0.97), new Bag()));
            ToolMenu.Add(new ToolButton(toolIconLoader.GetImage(typeof(Bottle)), new ProportionalRectangle(0.63, 0.71, 0.82, 0.97), new Bottle()));
            ToolMenu.Add(new ToolButton(toolIconLoader.GetImage(typeof(Scythe)), new ProportionalRectangle(0.72, 0.80, 0.82, 0.97), new Scythe()));

            ToolMenu.RepositionButtons(0.08, 0.14, 0.03, 0.01);


            // Load plant assets
            //TODO plant loader from config files
            plantAssets = new();
            plantAssets.Load(typeof(RaddishPlant),
                "C:\\Users\\Marie Hledíková\\OneDrive\\Pictures\\Seed.png",
                "C:\\Users\\Marie Hledíková\\OneDrive\\Pictures\\Small-seedling.png",
                "C:\\Users\\Marie Hledíková\\OneDrive\\Pictures\\Big-seedling.png",
                "C:\\Users\\Marie Hledíková\\OneDrive\\Pictures\\Adult-raddish.png",
                "C:\\Users\\Marie Hledíková\\OneDrive\\Pictures\\Fruiting-raddish.png"
            );
            plantAssets.Load(typeof(CarrotPlant),
                "C:\\Users\\Marie Hledíková\\OneDrive\\Pictures\\Seed.png",
                "C:\\Users\\Marie Hledíková\\OneDrive\\Pictures\\Small-seedling.png",
                "C:\\Users\\Marie Hledíková\\OneDrive\\Pictures\\Big-seedling.png",
                "C:\\Users\\Marie Hledíková\\OneDrive\\Pictures\\Adult-carrot.png",
                "C:\\Users\\Marie Hledíková\\OneDrive\\Pictures\\Fruiting-carrot.png"
            );
            plantAssets.Load(typeof(PotatoPlant),
                "C:\\Users\\Marie Hledíková\\OneDrive\\Pictures\\Seed.png",
                "C:\\Users\\Marie Hledíková\\OneDrive\\Pictures\\Small-seedling-multi.png",
                "C:\\Users\\Marie Hledíková\\OneDrive\\Pictures\\Big-seedling-multi.png",
                "C:\\Users\\Marie Hledíková\\OneDrive\\Pictures\\Adult-potato.png",
                "C:\\Users\\Marie Hledíková\\OneDrive\\Pictures\\Fruiting-potato.png"
            );
            plantAssets.Load(typeof(TomatoPlant),
                "C:\\Users\\Marie Hledíková\\OneDrive\\Pictures\\Seed.png",
                "C:\\Users\\Marie Hledíková\\OneDrive\\Pictures\\Small-seedling-multi.png",
                "C:\\Users\\Marie Hledíková\\OneDrive\\Pictures\\Big-seedling-multi.png",
                "C:\\Users\\Marie Hledíková\\OneDrive\\Pictures\\Adult-tomato.png",
                "C:\\Users\\Marie Hledíková\\OneDrive\\Pictures\\Fruiting-tomato.png"
            );

            // Initialize farm interaction areas
            farmCoords = new List<ProportionalRectangle>();
            double[] XBounds = { 0.02, 0.45, 0.54, 0.98 };
            double[] YBounds = { 0.37, 0.66, 0.70, 0.98 };

            farmCoords.Add(new ProportionalRectangle(XBounds[0], XBounds[1], YBounds[0], YBounds[1]));
            farmCoords.Add(new ProportionalRectangle(XBounds[2], XBounds[3], YBounds[0], YBounds[1]));
            farmCoords.Add(new ProportionalRectangle(XBounds[0], XBounds[1], YBounds[2], YBounds[3]));
            farmCoords.Add(new ProportionalRectangle(XBounds[2], XBounds[3], YBounds[2], YBounds[3]));

            // Initialize plots
            plotCoords = new ProportionalRectangle[gameState.CurrentFarm.Rows, gameState.CurrentFarm.Cols];
            XBounds = [0.21, 0.39, 0.58, 0.77, 0.96];
            YBounds = [0.07, 0.31, 0.57, 0.81];
            for (int i = 0; i < plotCoords.GetLength(0); i++) 
            {
                for (int j = 0; j < plotCoords.GetLength(1); j++)
                {
                    plotCoords[i,j] = new ProportionalRectangle(XBounds[j], XBounds[j+1], YBounds[i], YBounds[i+1] );
                }
            }

        }

        private void PaintFullView(Graphics g)
        {
            g.DrawImage(assetLoader["Background"], 0, 0, width, height);
            //g.DrawImage(assetLoader["Plant"], 100, 100);
        }

        private void PaintFarmView(int farm, Graphics g)
        {
            // Draw plots
            g.DrawImage(assetLoader["Plots-default"], 0, 0, width, height);

            for (int i = 0; i < gameState.CurrentFarm.Rows; i++)
            {
                for (int j = 0; j < gameState.CurrentFarm.Cols; j++)
                {
                    bool watered = gameState.CurrentFarm[i, j].Watered;
                    bool highlighted = gameState.CurrentFarm[i, j] == gameState.CurrentFarm.Highlighted;
                    Rectangle position = plotCoords[i, j].GetAbsolute(width, height);

                    // Draw plot highlights and watered color
                    if (watered && highlighted)
                    {
                        g.DrawImage(assetLoader["Plot-both"], position);
                    }
                    else if (watered)
                    {
                        g.DrawImage(assetLoader["Plot-watered"], position);
                    }
                    else if (highlighted)
                    {
                        g.DrawImage(assetLoader["Plot-highlighted"], position);
                    }

                    Type? plantType = gameState.CurrentFarm[i, j].PlantType;
                    GrowthState? state = gameState.CurrentFarm[i, j].State;
                    bool? alive = gameState.CurrentFarm[i, j].Alive;

                    // Draw plant
                    if (plantType is Type t && state is GrowthState s)
                    {
                        if (alive == true)
                        {
                            g.DrawImage(plantAssets.GetImage(t, s), position);
                            if (gameState.CurrentFarm[i, j].HasBug)
                            {
                                g.DrawImage(assetLoader["Worm"], position);
                            }
                        }
                        else
                        {
                            g.DrawImage(assetLoader["Dead"], position);
                        }
                    }

                }
            }

            // Draw background
            g.DrawImage(assetLoader["Plot-background"], 0, 0, width, height);

            // Draw controls
            if (!gameState.CurrentFarm.Planted)
                PlantButton.Enable();
            else
                PlantButton.Disable();
            PlantButton.DrawSelf(g, width, height);
            HarvestButton.DrawSelf(g, width, height);

            ToolMenu.DrawSelf(g, width, height);
            BackButton.DrawSelf(g, width, height);
        }

        public void Paint(Graphics g)
        {
            switch (gameState.CurrentView)
            {
                case View.FullView:
                    PaintFullView(g);
                    break;
                case View.FarmView:
                    PaintFarmView(gameState.CurrentFarmIndex, g);
                    break;
                case View.CoopView:
                    break;
                case View.HouseView:
                    break;
                default:
                    break;
            }
        }

        public void HandleClick(int X, int Y)
        {
            double XProportional = (double)X/width;
            double YProportional = (double)Y/height;

            switch (gameState.CurrentView)
            {
                case View.FullView:
                    // See if inside a farm
                    for (int i = 0; i < farmCoords.Count; i++)
                    {
                        if (farmCoords[i].InArea(XProportional, YProportional))
                        {
                            gameState.CurrentView = View.FarmView;
                            gameState.CurrentFarmIndex = i;
                            break;
                        }
                    }
                    //TODO temp
                    if (YProportional < 0.3)
                        gameState.EndDay();

                    break;


                case View.FarmView:

                    // Buttons
                    PlantButton.Click(XProportional, YProportional, gameState);
                    HarvestButton.Click(XProportional, YProportional, gameState);
                    ToolMenu.Click(XProportional, YProportional, gameState);
                    BackButton.Click(XProportional, YProportional, gameState);

                    // See if inside a plot
                    for (int i = 0; i < gameState.CurrentFarm.Rows; i++)
                    {
                        for (int j = 0; j < gameState.CurrentFarm.Cols; j++)
                        {
                            if (plotCoords[i, j].InArea(XProportional, YProportional))
                            {
                                if (gameState.CurrentTool is Tool t && gameState.HeldProduct is null)
                                    t.Use(gameState, gameState.CurrentFarm[i, j]);
                                break;
                            }
                        }
                    }
                    break;
                case View.CoopView:
                    break;
                case View.HouseView:
                    break;
                default:
                    break;
            }
        }

        public void HandleMouseMove(int X, int Y)
        {
            double XProportional = (double)X / width;
            double YProportional = (double)Y / height;

            switch (gameState.CurrentView)
            {
                case View.FarmView:
                    // See if inside a plot
                    gameState.CurrentFarm.Unhighlight();
                    for (int i = 0; i < gameState.CurrentFarm.Rows; i++)
                    {
                        for (int j = 0; j < gameState.CurrentFarm.Cols; j++)
                        {
                            if (plotCoords[i, j].InArea(XProportional, YProportional))
                            {
                                gameState.CurrentFarm.Highlight(i, j);
                                break;
                            }
                        }
                    }
                    break;
                case View.CoopView:
                    break;
                case View.HouseView:
                    break;
                default:
                    break;
            }
        }
    }
}
