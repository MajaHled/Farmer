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
            get
            {
                if (LoadedAssets.ContainsKey(index))
                    return LoadedAssets[index];
                else
                    throw new ArgumentException($"Asset '{index}' not loaded.");
            }
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

    public interface IClickable
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

    public sealed class PlantMenuButton : GameButton
    {
        private MenuHandler PlantMenu;
        public PlantMenuButton(Bitmap icon, ProportionalRectangle position, MenuHandler plantMenu) : base(icon, position)
        {
            PlantMenu = plantMenu;
        }
        public PlantMenuButton(Bitmap icon, MenuHandler plantMenu) : base(icon)
        {
            PlantMenu = plantMenu;
        }

        protected override void Action(GameState state)
        {
            PlantMenu.Enable();
            //TODO handle disabling
            state.CurrentTool = null;
            state.HeldProduct = null; //TODO handle enabling this only once we sell lmao 
        }
    }

    public sealed class HarvestHouseButton : GameButton
    {
        public HarvestHouseButton(Bitmap icon, ProportionalRectangle position) : base(icon, position) { }
        public HarvestHouseButton(Bitmap icon) : base(icon) { }


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
        public ArrowButton(Bitmap icon, View destination) : base(icon)
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
        public ToolButton(Bitmap icon, Tool? tool) : base(icon)
        {
            Tool = tool;
        }

        protected override void Action(GameState state)
        {
            state.CurrentTool = Tool;
        }
    }

    public sealed class PlantButton : GameButton
    {
        private Seed ToPlant;

        public PlantButton(Bitmap icon, ProportionalRectangle position, Seed toPlant) : base(icon, position)
        {
            ToPlant = toPlant;
        }
        public PlantButton(Bitmap icon, Seed toPlant) : base(icon)
        {
            ToPlant = toPlant;
        }

        protected override void Action(GameState state)
        {
            state.PlantSeedToCurrent(ToPlant);
        }
    }


    public abstract class BuyButton : GameButton
    {
        private IBuyable Product;
        public BuyButton(Bitmap icon, ProportionalRectangle position, IBuyable product) : base(icon, position)
        {
            Product = product;
        }
        public BuyButton(Bitmap icon, IBuyable product) : base(icon)
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
            Enabled = true;
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
            if (!Enabled)
                return;

            foreach (GameButton button in Buttons)
                button.Click(x, y, state);
        }

        public void DrawSelf(Graphics g, int width, int height)
        {
            if (!Enabled)
                return;

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
            Enabled = true;
            foreach (GameButton button in Buttons)
                button.Enable();
        }
    }
    #endregion

    public class CursorHandler
    {
        public Bitmap? Icon { get; set; }
        public ProportionalRectangle Position { get; set; } = new();

        public void Draw(Graphics g, int absoluteWidth, int absoluteHeight)
        {
            if (Icon is Bitmap i)
                g.DrawImage(i, Position.GetAbsolute(absoluteWidth, absoluteHeight));
        }
    }
    
    public abstract class SceneHandler
    {
        protected List<IClickable> Clickables = [];

        public abstract void Draw(GameState state, Graphics g, int absolueWidth, int absoluteHeight);

        public virtual void HandleClick(double X, double Y, GameState state)
        {
            foreach(IClickable clickable in Clickables)
            {
                clickable.Click(X, Y, state);
            }
        }
        public virtual void HandleMouseMove(double X, double Y, GameState state) { }
    }

    public class MainSceneHandler : SceneHandler
    {
        private NamedAssetsLoader NamedAssets = new();
        private List<ProportionalRectangle> farmCoords;

        public MainSceneHandler()
        {
            // Load assets
            NamedAssets.Load("Background", "C:\\Users\\Marie Hledíková\\OneDrive\\Pictures\\Farmer-even.png");

            // Initialize farm interaction areas
            // TODO remake as clickables, add hover handling to clickables, which may do nothing by default
            farmCoords = new List<ProportionalRectangle>();
            double[] XBounds = { 0.02, 0.45, 0.54, 0.98 };
            double[] YBounds = { 0.37, 0.66, 0.70, 0.98 };

            farmCoords.Add(new ProportionalRectangle(XBounds[0], XBounds[1], YBounds[0], YBounds[1]));
            farmCoords.Add(new ProportionalRectangle(XBounds[2], XBounds[3], YBounds[0], YBounds[1]));
            farmCoords.Add(new ProportionalRectangle(XBounds[0], XBounds[1], YBounds[2], YBounds[3]));
            farmCoords.Add(new ProportionalRectangle(XBounds[2], XBounds[3], YBounds[2], YBounds[3]));
        }

        public override void Draw(GameState state, Graphics g, int absolueWidth, int absoluteHeight)
        {
            g.DrawImage(NamedAssets["Background"], 0, 0, absolueWidth, absoluteHeight);
        }

        public override void HandleClick(double X, double Y, GameState state)
        {
            base.HandleClick(X, Y, state);

            // See if inside a farm
            for (int i = 0; i < farmCoords.Count; i++)
            {
                if (farmCoords[i].InArea(X, Y))
                {
                    state.CurrentView = View.FarmView;
                    state.CurrentFarmIndex = i;
                    break;
                }
            }
            //TODO temp
            if (Y < 0.3)
                state.EndDay();
        }

        public override void HandleMouseMove(double X, double Y, GameState state)
        {
            base.HandleMouseMove(X, Y, state); //TODO redo base calls as public wrappers in parent class with protected utility methods
        }
    }

    [System.Runtime.Versioning.SupportedOSPlatform("windows")]
    public class FarmSceneHandler : SceneHandler
    {
        // Loaders
        private NamedAssetsLoader NamedAssets = new();
        private ToolIconLoader toolIconLoader;
        private PlantStatesLoader plantAssets;

        // Menus
        private MenuHandler ToolMenu;
        private MenuHandler PlantMenu;

        // Buttons
        private GameButton PlantMenuButton;
        private GameButton HarvestButton;
        private GameButton BackButton;

        private ProportionalRectangle[,] plotCoords;

        public FarmSceneHandler()
        {
            // Load assets:

            // Named assets
            NamedAssets.Load("Background", "C:\\Users\\Marie Hledíková\\OneDrive\\Pictures\\Farm.png");
            NamedAssets.Load("Worm", "C:\\Users\\Marie Hledíková\\OneDrive\\Pictures\\Worm.png");
            NamedAssets.Load("Dead", "C:\\Users\\Marie Hledíková\\OneDrive\\Pictures\\Dead.png");

            NamedAssets.Load("Plot-watered", "C:\\Users\\Marie Hledíková\\OneDrive\\Pictures\\Plot-watered-center.png");
            NamedAssets.Load("Plot-highlighted", "C:\\Users\\Marie Hledíková\\OneDrive\\Pictures\\Plot-highlighted-center.png");
            NamedAssets.Load("Plot-both", "C:\\Users\\Marie Hledíková\\OneDrive\\Pictures\\Plot-both.png");
            NamedAssets.Load("Plots-default", "C:\\Users\\Marie Hledíková\\OneDrive\\Pictures\\Plots-default.png");

            // Assets for toolbar
            //TODO held interface instead of just tool, do it for fruits also
            toolIconLoader = new ToolIconLoader();
            toolIconLoader.Add(typeof(Hand), new Bitmap("C:\\Users\\Marie Hledíková\\OneDrive\\Pictures\\Hand.png"));
            toolIconLoader.Add(typeof(Pail), new Bitmap("C:\\Users\\Marie Hledíková\\OneDrive\\Pictures\\Pail.png"));
            toolIconLoader.Add(typeof(Bag), new Bitmap("C:\\Users\\Marie Hledíková\\OneDrive\\Pictures\\Bag.png"));
            toolIconLoader.Add(typeof(Bottle), new Bitmap("C:\\Users\\Marie Hledíková\\OneDrive\\Pictures\\Bottle.png"));
            toolIconLoader.Add(typeof(Scythe), new Bitmap("C:\\Users\\Marie Hledíková\\OneDrive\\Pictures\\Scythe.png"));

            // Plant assets
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

            // Initialize menus:

            // Toolbar
            //TODO proportions to file???
            ToolMenu = new MenuHandler(new Bitmap("C:\\Users\\Marie Hledíková\\OneDrive\\Pictures\\Toolbar.png"), new ProportionalRectangle(0.31, 0.97, 0.82, 0.98));
            ToolMenu.Add(new ToolButton(toolIconLoader.GetImage(typeof(Hand)), new Hand()));
            ToolMenu.Add(new ToolButton(toolIconLoader.GetImage(typeof(Pail)), new Pail()));
            ToolMenu.Add(new ToolButton(toolIconLoader.GetImage(typeof(Bag)), new Bag()));
            ToolMenu.Add(new ToolButton(toolIconLoader.GetImage(typeof(Bottle)), new Bottle()));
            ToolMenu.Add(new ToolButton(toolIconLoader.GetImage(typeof(Scythe)), new Scythe()));

            ToolMenu.RepositionButtons(0.08, 0.14, 0.02, 0.01);

            // Planting menu
            PlantMenu = new MenuHandler(new Bitmap("C:\\Users\\Marie Hledíková\\OneDrive\\Pictures\\Center-menu.png"), new ProportionalRectangle(0.16, 0.84, 0.10, 0.90));
            PlantMenu.Add(new PlantButton(plantAssets.GetImage(typeof(RaddishPlant), GrowthState.Fruiting), new RaddishSeed())); //TODO temp icons
            PlantMenu.Add(new PlantButton(plantAssets.GetImage(typeof(CarrotPlant), GrowthState.Fruiting), new CarrotSeed()));
            PlantMenu.Add(new PlantButton(plantAssets.GetImage(typeof(TomatoPlant), GrowthState.Fruiting), new TomatoSeed()));
            PlantMenu.Add(new PlantButton(plantAssets.GetImage(typeof(PotatoPlant), GrowthState.Fruiting), new PotatoSeed()));

            PlantMenu.RepositionButtons(0.10, 0.18, 0.03, 0.07);
            PlantMenu.Disable();

            // Initialize buttons:
            HarvestButton = new HarvestHouseButton(new Bitmap("C:\\Users\\Marie Hledíková\\OneDrive\\Pictures\\Harvest-house.png"), new ProportionalRectangle(0.04, 0.18, 0.59, 0.91));
            BackButton = new ArrowButton(new Bitmap("C:\\Users\\Marie Hledíková\\OneDrive\\Pictures\\Back-arrow.png"), new ProportionalRectangle(0.88, 0.965, 0.82, 0.975), View.FullView);
            PlantMenuButton = new PlantMenuButton(new Bitmap("C:\\Users\\Marie Hledíková\\OneDrive\\Pictures\\Planting-plant.png"), new ProportionalRectangle(0.01, 0.20, 0.18, 0.44), PlantMenu);
            //TODO add exit button

            // Add controls to clickable list
            Clickables.Add(HarvestButton);
            Clickables.Add(PlantMenuButton);
            Clickables.Add(ToolMenu);
            Clickables.Add(PlantMenu);
            Clickables.Add(BackButton);

            // Initialize plots:
            // TODO remake as clickables
            plotCoords = new ProportionalRectangle[3, 4]; //TODO hardcoded constants
            double[] XBounds = [0.21, 0.39, 0.58, 0.77, 0.96];
            double[] YBounds = [0.07, 0.31, 0.57, 0.81];
            for (int i = 0; i < plotCoords.GetLength(0); i++)
            {
                for (int j = 0; j < plotCoords.GetLength(1); j++)
                {
                    plotCoords[i, j] = new ProportionalRectangle(XBounds[j], XBounds[j + 1], YBounds[i], YBounds[i + 1]);
                }
            }
        }

        public override void Draw(GameState state, Graphics g, int absolueWidth, int absoluteHeight)
        {
            // Draw plots
            g.DrawImage(NamedAssets["Plots-default"], 0, 0, absolueWidth, absoluteHeight);

            for (int i = 0; i < state.CurrentFarm.Rows; i++)
            {
                for (int j = 0; j < state.CurrentFarm.Cols; j++)
                {
                    bool watered = state.CurrentFarm[i, j].Watered;
                    bool highlighted = state.CurrentFarm[i, j] == state.CurrentFarm.Highlighted;
                    Rectangle position = plotCoords[i, j].GetAbsolute(absolueWidth, absoluteHeight);

                    // Draw plot highlights and watered color
                    if (watered && highlighted)
                    {
                        g.DrawImage(NamedAssets["Plot-both"], position);
                    }
                    else if (watered)
                    {
                        g.DrawImage(NamedAssets["Plot-watered"], position);
                    }
                    else if (highlighted)
                    {
                        g.DrawImage(NamedAssets["Plot-highlighted"], position);
                    }

                    Type? plantType = state.CurrentFarm[i, j].PlantType;
                    GrowthState? growthState = state.CurrentFarm[i, j].State;
                    bool? alive = state.CurrentFarm[i, j].Alive;

                    // Draw plant
                    if (plantType is Type t && growthState is GrowthState s)
                    {
                        if (alive == true)
                        {
                            g.DrawImage(plantAssets.GetImage(t, s), position);
                            if (state.CurrentFarm[i, j].HasBug)
                            {
                                g.DrawImage(NamedAssets["Worm"], position);
                            }
                        }
                        else
                        {
                            g.DrawImage(NamedAssets["Dead"], position);
                        }
                    }
                }
            }

            // Draw grass background
            g.DrawImage(NamedAssets["Background"], 0, 0, absolueWidth, absoluteHeight);

            // Handle control visibility
            if (!state.CurrentFarm.Planted)
                PlantMenuButton.Enable();
            else
            {
                PlantMenuButton.Disable();
                PlantMenu.Disable();
            }

            // Draw controls
            foreach(IClickable clickable in Clickables)
            {
                clickable.DrawSelf(g, absolueWidth, absoluteHeight);
            }
        }

        public override void HandleClick(double X, double Y, GameState state)
        {
            base.HandleClick(X, Y, state);

            //TODO handle disabling plots

            // See if inside a plot
            for (int i = 0; i < state.CurrentFarm.Rows; i++)
            {
                for (int j = 0; j < state.CurrentFarm.Cols; j++)
                {
                    if (plotCoords[i, j].InArea(X, Y))
                    {
                        if (state.CurrentTool is Tool t && state.HeldProduct is null)
                            t.Use(state, state.CurrentFarm[i, j]);
                        break;
                    }
                }
            }
        }

        public override void HandleMouseMove(double X, double Y, GameState state) { }
    }




    [System.Runtime.Versioning.SupportedOSPlatform("windows")] //Windows only due to Bitmap
    public class FarmerGraphics
    {
        private GameState gameState;
        private NamedAssetsLoader assetLoader;

        // Individual scene handlers
        private SceneHandler MainScene = new MainSceneHandler();
        private SceneHandler FarmScene = new FarmSceneHandler();

        private CursorHandler CursorHandler = new();

        public int width, height;
        // TODO better access

        public FarmerGraphics(GameState gameState)
        {
            this.gameState = gameState;

            assetLoader = new NamedAssetsLoader();
            //assetLoader.Load("Background", "C:\\Users\\Marie Hledíková\\OneDrive\\Pictures\\Farmer-even.png");
            width = 960;
            height = 540;
            //TODO better resize handling

            assetLoader.Load("Plant", "C:\\Users\\Marie Hledíková\\OneDrive\\Pictures\\Raddish-placeholder.png");
            // TODO cleanup
            
        }

        public void Paint(Graphics g)
        {
            switch (gameState.CurrentView)
            {
                case View.FullView:
                    MainScene.Draw(gameState, g, width, height);
                    break;
                case View.FarmView:
                    FarmScene.Draw(gameState, g, width, height);
                    if (gameState.HeldProduct is not null || gameState.CurrentTool is not null)
                        CursorHandler.Icon = assetLoader["Plant"]; //TODO temp icon
                    else
                        CursorHandler.Icon = null;
                    CursorHandler.Draw(g, width, height);
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
                    MainScene.HandleClick(XProportional, YProportional, gameState);
                    break;

                case View.FarmView:
                    FarmScene.HandleClick(XProportional, YProportional, gameState);
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

            CursorHandler.Position = new ProportionalRectangle(XProportional - 0.1, XProportional + 0.1, YProportional - 0.1, YProportional +0.1);
            // TODO proper sizing

            switch (gameState.CurrentView)
            {
                case View.FullView:
                    MainScene.HandleMouseMove(XProportional, YProportional, gameState);
                    break;
                case View.FarmView:
                    FarmScene.HandleMouseMove(XProportional, YProportional, gameState);
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
