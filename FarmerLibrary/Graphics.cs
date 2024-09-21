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
    public class SellableLoader
    {
        private Dictionary<Type, Bitmap> LoadedAssets = [];

        public void Add(Type type, Bitmap bitmap)
        {
            if (!typeof(ISellable).IsAssignableFrom(type))
                throw new ArgumentException($"Type {type} is not an ISellable implementation.");

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

    public interface IDrawable
    {
        public void Draw(Graphics g, GameState state, int width, int height);

    }

    #region clicking
    
    public interface IClickable : IDrawable
    {
        public void Click(double x, double y, GameState state);
        public void Hover(double x, double y, GameState state);
        public void Disable();
        public void Enable();
        public bool Enabled { get; }
    }

    public abstract class GameButton : IClickable
    {
        protected Bitmap Icon;
        protected ProportionalRectangle? Position;
        public List<IClickable> ToEnable { get; init; }
        public List<IClickable> ToDisable { get; init; }

        public bool Enabled { get; private set; }

        public GameButton(Bitmap icon, ProportionalRectangle position)
        {
            Icon = icon;
            Position = position;
            Enabled = true;
            ToEnable = [];
            ToDisable = [];
        }

        public GameButton(Bitmap icon)
        {
            Icon = icon;
            Position = null;
            Enabled = true;
            ToEnable = [];
            ToDisable = [];
        }

        public void SetPosition(ProportionalRectangle position) => Position = position;

        public void UnsetPosition() => Position = null;

        public void Disable() => Enabled = false;

        public void Enable() => Enabled = true; 

        public void Draw(Graphics g, GameState state, int width, int height)
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
                foreach (IClickable c in ToEnable)
                {
                    c.Enable();
                }
                foreach (IClickable c in ToDisable)
                {
                    c.Disable(); 
                }

                Action(state);
            }
        }

        protected abstract void Action(GameState state);

        public void Hover(double x, double y, GameState state)
        {
            // TODO button hover behavior
        }
    }

    public sealed class PlantMenuButton : GameButton
    {
        private MenuHandler PlantMenu;
        public PlantMenuButton(Bitmap icon, ProportionalRectangle position, MenuHandler plantMenu) : base(icon, position)
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

    public sealed class HarvestButton : GameButton
    {
        public HarvestButton(Bitmap icon, ProportionalRectangle position) : base(icon, position) { }

        protected override void Action(GameState state)
        {
            state.SellHeld();
        }
    }

    public sealed class SceneSwitchButton : GameButton
    {
        private readonly View Destination;
        public SceneSwitchButton(Bitmap icon, ProportionalRectangle position, View destination) : base(icon, position)
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

    public sealed class BuyButton : GameButton
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
            state.Buy(Product);
        }
    }

    public sealed class NewDayButton : GameButton
    {
        public NewDayButton(Bitmap icon, ProportionalRectangle position) : base(icon, position) { }

        protected override void Action(GameState state)
        {
            state.EndDay();
        }
    }

    [System.Runtime.Versioning.SupportedOSPlatform("windows")]
    public class MenuHandler : IClickable
    {
        public List<GameButton> Buttons { get; init; } // TODO Maybe like intexer, dont want it modified form outside maybe
        private Bitmap Background;
        public ProportionalRectangle BackgroundPosition { get; init; } 

        public bool Enabled { get; private set; }

        public MenuHandler(Bitmap background, ProportionalRectangle backgroundPosition)
        {
            Background = background;
            BackgroundPosition = backgroundPosition;
            Enabled = true;
            Buttons = [];
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

        public void Hover(double x, double y, GameState state)
        {
            if (!Enabled)
                return;

            foreach (GameButton button in Buttons)
                button.Hover(x, y, state);
        }

        public void Draw(Graphics g, GameState state, int width, int height)
        {
            if (!Enabled)
                return;

            g.DrawImage(Background, BackgroundPosition.GetAbsolute(width, height));
            foreach (GameButton button in Buttons)
                button.Draw(g, state, width, height);

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

    [System.Runtime.Versioning.SupportedOSPlatform("windows")]
    public class FarmDisplay : IClickable
    {
        private ProportionalRectangle[,] plotCoords;
        private NamedAssetsLoader NamedAssets = new();
        private PlantStatesLoader PlantStates;

        public FarmDisplay(PlantStatesLoader plantStates)
        {
            Enabled = true;
            PlantStates = plantStates;

            // Named assets
            NamedAssets.Load("Worm", "C:\\Users\\Marie Hledíková\\OneDrive\\Pictures\\Worm.png");
            NamedAssets.Load("Dead", "C:\\Users\\Marie Hledíková\\OneDrive\\Pictures\\Dead.png");

            NamedAssets.Load("Plot-watered", "C:\\Users\\Marie Hledíková\\OneDrive\\Pictures\\Plot-watered-center.png");
            NamedAssets.Load("Plot-highlighted", "C:\\Users\\Marie Hledíková\\OneDrive\\Pictures\\Plot-highlighted-center.png");
            NamedAssets.Load("Plot-both", "C:\\Users\\Marie Hledíková\\OneDrive\\Pictures\\Plot-both.png");
            NamedAssets.Load("Plots-default", "C:\\Users\\Marie Hledíková\\OneDrive\\Pictures\\Plots-default.png");


            // Initialize plots:
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

        public bool Enabled { get; private set; }

        public void Click(double x, double y, GameState state)
        {
            if (!Enabled)
                return;

            // See if inside a plot
            for (int i = 0; i < state.CurrentFarm.Rows; i++)
            {
                for (int j = 0; j < state.CurrentFarm.Cols; j++)
                {
                    if (plotCoords[i, j].InArea(x, y))
                    {
                        // TODO disable toolbar when harvesting somewhere
                        if (state.CurrentTool is Tool t && state.HeldProduct is null)
                            t.Use(state, state.CurrentFarm[i, j]);
                        break;
                    }
                }
            }
        }

        public void Hover(double x, double y, GameState state)
        {
            state.CurrentFarm.Unhighlight();

            if (!Enabled)
                return;

            // See if inside a plot
            for (int i = 0; i < state.CurrentFarm.Rows; i++)
            {
                for (int j = 0; j < state.CurrentFarm.Cols; j++)
                {
                    if (plotCoords[i, j].InArea(x, y))
                    {
                        state.CurrentFarm.Highlight(i, j);
                        break;
                    }
                }
            }
        }

        public void Draw(Graphics g, GameState state, int width, int height)
        {
            g.DrawImage(NamedAssets["Plots-default"], 0, 0, width, height);

            for (int i = 0; i < state.CurrentFarm.Rows; i++)
            {
                for (int j = 0; j < state.CurrentFarm.Cols; j++)
                {
                    bool watered = state.CurrentFarm[i, j].Watered;
                    bool highlighted = state.CurrentFarm[i, j] == state.CurrentFarm.Highlighted;
                    Rectangle position = plotCoords[i, j].GetAbsolute(width, height);

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
                            g.DrawImage(PlantStates.GetImage(t, s), position);
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
        }
        public void Disable() => Enabled = false;

        public void Enable() => Enabled = true;
    }

    #endregion

    public class CursorHandler : IDrawable
    {
        private ProportionalRectangle Position { get; set; } = new();
        private ToolIconLoader? ToolAssets;
        private SellableLoader? FruitAssets;

        public static double CURSOR_SIZE = 0.1;

        public void SetToolIcons(ToolIconLoader toolAssets)
        {
            ToolAssets = toolAssets;
        }

        public void SetFruitIcons(SellableLoader fruitAssets)
        {
            FruitAssets = fruitAssets;
        }

        public void Draw(Graphics g, GameState state, int absoluteWidth, int absoluteHeight)
        {
            if (ToolAssets is ToolIconLoader ta && state.CurrentTool is Tool t)
                g.DrawImage(ta.GetImage(t.GetType()), Position.GetAbsolute(absoluteWidth, absoluteHeight));
            else if (FruitAssets is SellableLoader sl && state.HeldProduct is Fruit f)
                g.DrawImage(sl.GetImage(f.GetType()), Position.GetAbsolute(absoluteWidth, absoluteHeight));
        }

        public void UpdatePosition(double x, double y)
        {
            Position = new ProportionalRectangle(x-CURSOR_SIZE/2, x+CURSOR_SIZE/2, y-CURSOR_SIZE/2, y+CURSOR_SIZE/2);
        }
    }

    public class MoneyDisplay : IDrawable
    {
        private Bitmap Background;
        private ProportionalRectangle Position;

        public MoneyDisplay(Bitmap background, ProportionalRectangle position)
        {
            Background = background;
            Position = position;
        }

        public void Draw(Graphics g, GameState state, int width, int height)
        {
            g.DrawImage(Background, Position.GetAbsolute(width, height));
            g.DrawString(state.PlayerMoney.ToString() + "$", new Font("Arial", 16), new SolidBrush(Color.Black), Position.GetAbsolute(width, height));
        }
    }

    #region scene handlers
    public abstract class SceneHandler : IDrawable
    {
        protected List<IClickable> Clickables = [];

        public abstract void Draw(Graphics g, GameState state, int absolueWidth, int absoluteHeight);

        protected void DrawClickables(GameState state, Graphics g, int absolueWidth, int absoluteHeight)
        {
            foreach (IClickable clickable in Clickables)
                clickable.Draw(g, state, absolueWidth, absoluteHeight);
        }

        public virtual void HandleClick(double X, double Y, GameState state)
        {
            foreach(IClickable clickable in Clickables)
            {
                clickable.Click(X, Y, state);
            }
        }
        public virtual void HandleMouseMove(double X, double Y, GameState state)
        {
            foreach (IClickable clickable in Clickables)
            {
                clickable.Hover(X, Y, state);
            }
        }
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
            // TODO remake as clickables
            farmCoords = new List<ProportionalRectangle>();
            double[] XBounds = { 0.02, 0.45, 0.54, 0.98 };
            double[] YBounds = { 0.37, 0.66, 0.70, 0.98 };

            farmCoords.Add(new ProportionalRectangle(XBounds[0], XBounds[1], YBounds[0], YBounds[1]));
            farmCoords.Add(new ProportionalRectangle(XBounds[2], XBounds[3], YBounds[0], YBounds[1]));
            farmCoords.Add(new ProportionalRectangle(XBounds[0], XBounds[1], YBounds[2], YBounds[3]));
            farmCoords.Add(new ProportionalRectangle(XBounds[2], XBounds[3], YBounds[2], YBounds[3]));

            Clickables.Add(new SceneSwitchButton(new Bitmap("C:\\Users\\Marie Hledíková\\OneDrive\\Pictures\\ArrowMain.png"), new ProportionalRectangle(0.48, 0.52, 0.84, 0.99), View.RoadView));
            Clickables.Add(new SceneSwitchButton(new Bitmap("C:\\Users\\Marie Hledíková\\OneDrive\\Pictures\\Coop-button.png"), new ProportionalRectangle(0.77, 0.94, 0.13, 0.352), View.CoopView));
            Clickables.Add(new SceneSwitchButton(new Bitmap("C:\\Users\\Marie Hledíková\\OneDrive\\Pictures\\House-button.png"), new ProportionalRectangle(0.39, 0.61, 0.01, 0.352), View.HouseView));

        }

        public override void Draw(Graphics g, GameState state, int absolueWidth, int absoluteHeight)
        {
            g.DrawImage(NamedAssets["Background"], 0, 0, absolueWidth, absoluteHeight);

            DrawClickables(state, g, absolueWidth, absoluteHeight);
        }

        public override void HandleClick(double X, double Y, GameState state)
        {
            base.HandleClick(X, Y, state);

            // TODO redu farms as clickables
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
        private ToolIconLoader ToolIconLoader = new();
        private PlantStatesLoader PlantAssets = new();
        private SellableLoader    FruitAssets = new();

        // Menus
        private MenuHandler ToolMenu;
        private MenuHandler PlantMenu;

        private FarmDisplay Farm;

        // Buttons
        private GameButton PlantMenuButton;
        private GameButton HarvestButton;
        private GameButton BackButton;

        // Cursor handler
        private CursorHandler CursorHandler = new();

        public FarmSceneHandler()
        {
            // Load assets:
            NamedAssets.Load("Background", "C:\\Users\\Marie Hledíková\\OneDrive\\Pictures\\Farm.png");

            // Assets for toolbar
            ToolIconLoader.Add(typeof(Hand), new Bitmap("C:\\Users\\Marie Hledíková\\OneDrive\\Pictures\\Hand.png"));
            ToolIconLoader.Add(typeof(Pail), new Bitmap("C:\\Users\\Marie Hledíková\\OneDrive\\Pictures\\Pail.png"));
            ToolIconLoader.Add(typeof(Bag), new Bitmap("C:\\Users\\Marie Hledíková\\OneDrive\\Pictures\\Bag.png"));
            ToolIconLoader.Add(typeof(Bottle), new Bitmap("C:\\Users\\Marie Hledíková\\OneDrive\\Pictures\\Bottle.png"));
            ToolIconLoader.Add(typeof(Scythe), new Bitmap("C:\\Users\\Marie Hledíková\\OneDrive\\Pictures\\Scythe.png"));

            // Held fruit assets for harvesting
            FruitAssets.Add(typeof(RaddishFruit), new Bitmap("C:\\Users\\Marie Hledíková\\OneDrive\\Pictures\\Raddish.png"));
            FruitAssets.Add(typeof(CarrotFruit), new Bitmap("C:\\Users\\Marie Hledíková\\OneDrive\\Pictures\\Carrot.png"));
            FruitAssets.Add(typeof(PotatoFruit), new Bitmap("C:\\Users\\Marie Hledíková\\OneDrive\\Pictures\\Potato.png"));
            FruitAssets.Add(typeof(TomatoFruit), new Bitmap("C:\\Users\\Marie Hledíková\\OneDrive\\Pictures\\Tomato.png"));


            // Plant assets
            //TODO plant loader from config files
            PlantAssets.Load(typeof(RaddishPlant),
                "C:\\Users\\Marie Hledíková\\OneDrive\\Pictures\\Seed.png",
                "C:\\Users\\Marie Hledíková\\OneDrive\\Pictures\\Small-seedling.png",
                "C:\\Users\\Marie Hledíková\\OneDrive\\Pictures\\Big-seedling.png",
                "C:\\Users\\Marie Hledíková\\OneDrive\\Pictures\\Adult-raddish.png",
                "C:\\Users\\Marie Hledíková\\OneDrive\\Pictures\\Fruiting-raddish.png"
            );
            PlantAssets.Load(typeof(CarrotPlant),
                "C:\\Users\\Marie Hledíková\\OneDrive\\Pictures\\Seed.png",
                "C:\\Users\\Marie Hledíková\\OneDrive\\Pictures\\Small-seedling.png",
                "C:\\Users\\Marie Hledíková\\OneDrive\\Pictures\\Big-seedling.png",
                "C:\\Users\\Marie Hledíková\\OneDrive\\Pictures\\Adult-carrot.png",
                "C:\\Users\\Marie Hledíková\\OneDrive\\Pictures\\Fruiting-carrot.png"
            );
            PlantAssets.Load(typeof(PotatoPlant),
                "C:\\Users\\Marie Hledíková\\OneDrive\\Pictures\\Seed.png",
                "C:\\Users\\Marie Hledíková\\OneDrive\\Pictures\\Small-seedling-multi.png",
                "C:\\Users\\Marie Hledíková\\OneDrive\\Pictures\\Big-seedling-multi.png",
                "C:\\Users\\Marie Hledíková\\OneDrive\\Pictures\\Adult-potato.png",
                "C:\\Users\\Marie Hledíková\\OneDrive\\Pictures\\Fruiting-potato.png"
            );
            PlantAssets.Load(typeof(TomatoPlant),
                "C:\\Users\\Marie Hledíková\\OneDrive\\Pictures\\Seed.png",
                "C:\\Users\\Marie Hledíková\\OneDrive\\Pictures\\Small-seedling-multi.png",
                "C:\\Users\\Marie Hledíková\\OneDrive\\Pictures\\Big-seedling-multi.png",
                "C:\\Users\\Marie Hledíková\\OneDrive\\Pictures\\Adult-tomato.png",
                "C:\\Users\\Marie Hledíková\\OneDrive\\Pictures\\Fruiting-tomato.png"
            );

            // Initialize cursor handler with icons
            CursorHandler.SetToolIcons(ToolIconLoader);
            CursorHandler.SetFruitIcons(FruitAssets);

            // Initialize menus:
            // Toolbar
            ToolMenu = new MenuHandler(new Bitmap("C:\\Users\\Marie Hledíková\\OneDrive\\Pictures\\Toolbar.png"), new ProportionalRectangle(0.31, 0.97, 0.82, 0.98));
            ToolMenu.Add(new ToolButton(ToolIconLoader.GetImage(typeof(Hand)), new Hand()));
            ToolMenu.Add(new ToolButton(ToolIconLoader.GetImage(typeof(Pail)), new Pail()));
            ToolMenu.Add(new ToolButton(ToolIconLoader.GetImage(typeof(Bag)), new Bag()));
            ToolMenu.Add(new ToolButton(ToolIconLoader.GetImage(typeof(Bottle)), new Bottle()));
            ToolMenu.Add(new ToolButton(ToolIconLoader.GetImage(typeof(Scythe)), new Scythe()));

            ToolMenu.RepositionButtons(0.08, 0.14, 0.02, 0.01);

            // Planting menu
            PlantMenu = new MenuHandler(new Bitmap("C:\\Users\\Marie Hledíková\\OneDrive\\Pictures\\Center-menu.png"), new ProportionalRectangle(0.16, 0.84, 0.10, 0.90));
            PlantMenu.Add(new PlantButton(FruitAssets.GetImage(typeof(RaddishFruit)), new RaddishSeed()));
            PlantMenu.Add(new PlantButton(FruitAssets.GetImage(typeof(CarrotFruit)), new CarrotSeed()));
            PlantMenu.Add(new PlantButton(FruitAssets.GetImage(typeof(TomatoFruit)), new TomatoSeed()));
            PlantMenu.Add(new PlantButton(FruitAssets.GetImage(typeof(PotatoFruit)), new PotatoSeed()));

            PlantMenu.RepositionButtons(0.10, 0.18, 0.03, 0.07);
            PlantMenu.Disable();

            // Initialize buttons:
            HarvestButton = new HarvestButton(new Bitmap("C:\\Users\\Marie Hledíková\\OneDrive\\Pictures\\Harvest-house.png"), new ProportionalRectangle(0.04, 0.18, 0.59, 0.91));
            BackButton = new SceneSwitchButton(new Bitmap("C:\\Users\\Marie Hledíková\\OneDrive\\Pictures\\Back-arrow.png"), new ProportionalRectangle(0.88, 0.965, 0.82, 0.975), View.FullView);
            PlantMenuButton = new PlantMenuButton(new Bitmap("C:\\Users\\Marie Hledíková\\OneDrive\\Pictures\\Planting-plant.png"), new ProportionalRectangle(0.01, 0.20, 0.18, 0.44), PlantMenu);

            Farm = new FarmDisplay(PlantAssets);

            // Handle enable/disable
            PlantMenuButton.ToDisable.Add(Farm);
            PlantMenuButton.ToDisable.Add(ToolMenu);
            foreach (var button in PlantMenu.Buttons)
            {
                button.ToEnable.Add(Farm);
                button.ToEnable.Add(ToolMenu);
            }
            BackButton.ToDisable.Add(PlantMenu);
            BackButton.ToEnable.Add(PlantMenuButton);
            BackButton.ToEnable.Add(Farm);
            BackButton.ToEnable.Add(ToolMenu);

            // Add controls to clickable list
            // (not Farm, because that is handled separately)
            Clickables.Add(HarvestButton);
            Clickables.Add(PlantMenuButton);
            Clickables.Add(ToolMenu);
            Clickables.Add(PlantMenu);
            Clickables.Add(BackButton);
        }

        public override void Draw(Graphics g, GameState state, int absoluteWidth, int absoluteHeight)
        {
            // Draw plots
            Farm.Draw(g, state, absoluteWidth, absoluteHeight); //TODO maybe redo dimentions to not be whole screen

            // Draw grass background
            g.DrawImage(NamedAssets["Background"], 0, 0, absoluteWidth, absoluteHeight);

            // Handle control visibility
            if (!state.CurrentFarm.Planted)
                PlantMenuButton.Enable();
            else
            {
                PlantMenuButton.Disable();
                PlantMenu.Disable();
            }

            // Draw controls
            DrawClickables(state, g, absoluteWidth, absoluteHeight);

            // Draw cursor
            CursorHandler.Draw(g, state, absoluteWidth, absoluteHeight);
        }

        public override void HandleClick(double x, double y, GameState state)
        {
            base.HandleClick(x, y, state);
            Farm.Click(x, y, state);
        }

        public override void HandleMouseMove(double x, double y, GameState state)
        {
            Farm.Hover(x, y, state);

            CursorHandler.UpdatePosition(x, y);
        }
    }

    public class RoadSceneHandler : SceneHandler
    {
        private NamedAssetsLoader NamedAssets = new();

        public RoadSceneHandler()
        {
            // Load assets
            NamedAssets.Load("Background", "C:\\Users\\Marie Hledíková\\OneDrive\\Pictures\\Shops-background.png");

            Clickables.Add(new SceneSwitchButton(new Bitmap("C:\\Users\\Marie Hledíková\\OneDrive\\Pictures\\ShopHouse.png"), new ProportionalRectangle(0.06, 0.37, 0.09, 0.79), View.SeedShopView));
            Clickables.Add(new SceneSwitchButton(new Bitmap("C:\\Users\\Marie Hledíková\\OneDrive\\Pictures\\ShopHouse.png"), new ProportionalRectangle(0.63, 0.94, 0.09, 0.79), View.ChickShopView));
            Clickables.Add(new SceneSwitchButton(new Bitmap("C:\\Users\\Marie Hledíková\\OneDrive\\Pictures\\Arrow-shops.png"), new ProportionalRectangle(0.45, 0.56, 0.07, 0.33), View.FullView));
        }

        public override void Draw(Graphics g, GameState state, int absolueWidth, int absoluteHeight)
        {
            g.DrawImage(NamedAssets["Background"], 0, 0, absolueWidth, absoluteHeight);

            DrawClickables(state, g, absolueWidth, absoluteHeight);
        }
    }

    public class CoopSceneHandler : SceneHandler
    {
        private NamedAssetsLoader NamedAssets = new();

        public CoopSceneHandler()
        {
            NamedAssets.Load("Background", "C:\\Users\\Marie Hledíková\\OneDrive\\Pictures\\Coop-background.png");

            Clickables.Add(new SceneSwitchButton(new Bitmap("C:\\Users\\Marie Hledíková\\OneDrive\\Pictures\\Back-arrow.png"), new ProportionalRectangle(0.88, 0.965, 0.82, 0.975), View.FullView));
        }

        public override void Draw(Graphics g, GameState state, int absolueWidth, int absoluteHeight)
        {
            g.DrawImage(NamedAssets["Background"], 0, 0, absolueWidth, absoluteHeight);

            DrawClickables(state, g, absolueWidth, absoluteHeight);
        }
    }

    public class ShopSceneHandler : SceneHandler
    {
        private NamedAssetsLoader NamedAssets = new();
        private MenuHandler ShoppingMenu; // TODO implement

        public ShopSceneHandler()
        {
            NamedAssets.Load("Background", "C:\\Users\\Marie Hledíková\\OneDrive\\Pictures\\Shop.png");

            ShoppingMenu = new MenuHandler(new Bitmap("C:\\Users\\Marie Hledíková\\OneDrive\\Pictures\\Shop-menu.png"), new ProportionalRectangle(0.06, 0.69, 0.13, 0.87));

            Clickables.Add(new SceneSwitchButton(new Bitmap("C:\\Users\\Marie Hledíková\\OneDrive\\Pictures\\Arrow-shop.png"), new ProportionalRectangle(0.79, 0.98, 0.01, 0.18), View.RoadView));
            Clickables.Add(ShoppingMenu);
        }

        public void AddStock(IBuyable item, Bitmap icon)
        {
            ShoppingMenu.Add(new BuyButton(icon, item));
            ShoppingMenu.RepositionButtons(0.11, 0.2, 0.01, 0.04);
        }

        public override void Draw(Graphics g, GameState state, int absolueWidth, int absoluteHeight)
        {
            g.DrawImage(NamedAssets["Background"], 0, 0, absolueWidth, absoluteHeight);

            DrawClickables(state, g, absolueWidth, absoluteHeight);
        }
    }

    public class HouseSceneHandler : SceneHandler
    {
        private NamedAssetsLoader NamedAssets = new();

        public HouseSceneHandler()
        {
            NamedAssets.Load("Background", "C:\\Users\\Marie Hledíková\\OneDrive\\Pictures\\House.png");

            Clickables.Add(new SceneSwitchButton(new Bitmap("C:\\Users\\Marie Hledíková\\OneDrive\\Pictures\\Back-arrow.png"), new ProportionalRectangle(0.88, 0.965, 0.82, 0.975), View.FullView));
            Clickables.Add(new NewDayButton(new Bitmap("C:\\Users\\Marie Hledíková\\OneDrive\\Pictures\\New-day.png"), new ProportionalRectangle(0.02, 0.14, 0.12, 0.35)));
        }

        public override void Draw(Graphics g, GameState state, int absolueWidth, int absoluteHeight)
        {
            g.DrawImage(NamedAssets["Background"], 0, 0, absolueWidth, absoluteHeight);

            DrawClickables(state, g, absolueWidth, absoluteHeight);
        }
    }

    #endregion

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

        private MoneyDisplay MoneyDisplay;

        public int width, height;
        // TODO better access

        public FarmerGraphics(GameState gameState)
        {
            this.gameState = gameState;

            width = 960;
            height = 540;
            //TODO better resize handling

            SeedShopScene.AddStock(new RaddishSeed(), new Bitmap("C:\\Users\\Marie Hledíková\\OneDrive\\Pictures\\Raddish.png"));
            SeedShopScene.AddStock(new CarrotSeed(), new Bitmap("C:\\Users\\Marie Hledíková\\OneDrive\\Pictures\\Carrot.png"));
            SeedShopScene.AddStock(new PotatoSeed(), new Bitmap("C:\\Users\\Marie Hledíková\\OneDrive\\Pictures\\Potato.png"));
            SeedShopScene.AddStock(new TomatoSeed(), new Bitmap("C:\\Users\\Marie Hledíková\\OneDrive\\Pictures\\Tomato.png"));

            MoneyDisplay = new MoneyDisplay(new Bitmap("C:\\Users\\Marie Hledíková\\OneDrive\\Pictures\\Money.png"), new ProportionalRectangle(0.01, 0.14, 0.02, 0.13));
        }

        public void Paint(Graphics g)
        {
            switch (gameState.CurrentView)
            {
                case View.FullView:
                    MainScene.Draw(g, gameState, width, height);
                    break;
                case View.FarmView:
                    FarmScene.Draw(g, gameState, width, height);
                    break;
                case View.CoopView:
                    CoopScene.Draw(g, gameState, width, height);
                    break;
                case View.HouseView:
                    HouseScene.Draw(g, gameState, width, height);
                    break;
                case View.RoadView:
                    RoadScene.Draw(g, gameState, width, height);
                    break;
                case View.SeedShopView:
                    SeedShopScene.Draw(g, gameState, width, height);
                    break;
                case View.ChickShopView:
                    ChickShopScene.Draw(g, gameState, width, height);
                    break;
                default:
                    break;
            }

            MoneyDisplay.Draw(g, gameState, width, height);
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
                    CoopScene.HandleClick(XProportional, YProportional, gameState);
                    break;
                case View.HouseView:
                    HouseScene.HandleClick(XProportional, YProportional, gameState);
                    break;
                case View.RoadView:
                    RoadScene.HandleClick(XProportional, YProportional, gameState);
                    break;
                case View.SeedShopView:
                    SeedShopScene.HandleClick(XProportional, YProportional, gameState);
                    break;
                case View.ChickShopView:
                    ChickShopScene.HandleClick(XProportional, YProportional, gameState);
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
                case View.FullView:
                    MainScene.HandleMouseMove(XProportional, YProportional, gameState);
                    break;
                case View.FarmView:
                    FarmScene.HandleMouseMove(XProportional, YProportional, gameState);
                    break;
                case View.CoopView:
                    CoopScene.HandleMouseMove(XProportional, YProportional, gameState);
                    break;
                case View.HouseView:
                    HouseScene.HandleMouseMove(XProportional, YProportional, gameState);
                    break;
                case View.RoadView:
                    RoadScene.HandleMouseMove(XProportional, YProportional, gameState);
                    break;
                case View.SeedShopView:
                    SeedShopScene.HandleMouseMove(XProportional, YProportional, gameState);
                    break;
                case View.ChickShopView:
                    ChickShopScene.HandleMouseMove(XProportional, YProportional, gameState);
                    break;
                default:
                    break;
            }
        }
    }
}
