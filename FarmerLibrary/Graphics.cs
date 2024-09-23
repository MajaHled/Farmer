using System.Drawing;

namespace FarmerLibrary
{
    #region loaders
    [System.Runtime.Versioning.SupportedOSPlatform("windows")] //Windows only due to Bitmap
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
    
    [System.Runtime.Versioning.SupportedOSPlatform("windows")]
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

    [System.Runtime.Versioning.SupportedOSPlatform("windows")]
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



    #region Buttons
    [System.Runtime.Versioning.SupportedOSPlatform("windows")]
    public abstract class GameButton : IClickable
    {
        protected Bitmap Icon;
        protected ProportionalRectangle? Position;

        // Highlight behavior
        protected bool Highlighed = false;
        protected readonly double HIGHLIGHT_MARGIN = 0.01;
        public bool HighlightOn { get; set; } = true;

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
            {
                if (Highlighed && HighlightOn)
                {
                    var temp = new ProportionalRectangle(p.X1 - HIGHLIGHT_MARGIN, p.X2 + HIGHLIGHT_MARGIN, p.Y1 - HIGHLIGHT_MARGIN, p.Y2 + HIGHLIGHT_MARGIN);
                    g.DrawImage(Icon, temp.GetAbsolute(width, height));
                }
                else
                    g.DrawImage(Icon, p.GetAbsolute(width, height));
            }
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
            if (Enabled && Position is ProportionalRectangle p && p.InArea(x, y))
                Highlighed = true;
            else
                Highlighed = false;
        }
    }

    [System.Runtime.Versioning.SupportedOSPlatform("windows")]
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
            state.CurrentTool = null;
            state.HeldProduct = null;
        }
    }

    [System.Runtime.Versioning.SupportedOSPlatform("windows")]
    public sealed class HarvestButton : GameButton
    {
        public HarvestButton(Bitmap icon, ProportionalRectangle position) : base(icon, position) { }

        protected override void Action(GameState state)
        {
            state.SellHeld();
        }
    }

    [System.Runtime.Versioning.SupportedOSPlatform("windows")]
    public sealed class SceneSwitchButton : GameButton
    {
        private readonly View Destination;
        private bool takesStamina = false;

        public SceneSwitchButton(Bitmap icon, ProportionalRectangle position, View destination) : base(icon, position)
        {
            Destination = destination;
        }
        public void EnableStamina() => takesStamina = true;
        public void DisableStamina() => takesStamina = false;


        protected override void Action(GameState state)
        {
            if (takesStamina)
            {
                if (!state.CanWork())
                    return;
                state.DoLabor();
            }

            state.CurrentView = Destination;
            state.ResetTemps();
        }
    }

    [System.Runtime.Versioning.SupportedOSPlatform("windows")]
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

    [System.Runtime.Versioning.SupportedOSPlatform("windows")]
    public sealed class PlantButton : GameButton
    {
        private Seed ToPlant;

        public PlantButton(Bitmap icon, ProportionalRectangle position, Seed toPlant) : base(icon, position)
        {
            ToPlant = toPlant;
            HighlightOn = false;
        }
        public PlantButton(Bitmap icon, Seed toPlant) : base(icon)
        {
            ToPlant = toPlant;
            HighlightOn = false;
        }

        protected override void Action(GameState state)
        {
            state.PlantSeedToCurrent(ToPlant);
        }
    }

    [System.Runtime.Versioning.SupportedOSPlatform("windows")]
    public sealed class BuyButton : GameButton
    {
        private IBuyable Product;

        public BuyButton(Bitmap icon, ProportionalRectangle position, IBuyable product) : base(icon, position)
        {
            Product = product;
            HighlightOn = false;
        }
        public BuyButton(Bitmap icon, IBuyable product) : base(icon)
        {
            Product = product;
            HighlightOn = false;
        }

        protected override void Action(GameState state)
        {
            state.Buy(Product);
        }
    }

    [System.Runtime.Versioning.SupportedOSPlatform("windows")]
    public sealed class NewDayButton : GameButton
    {
        public NewDayButton(Bitmap icon, ProportionalRectangle position) : base(icon, position) { }

        protected override void Action(GameState state)
        {
            state.EndDay();
        }
    }

    [System.Runtime.Versioning.SupportedOSPlatform("windows")]
    public sealed class EggButton : GameButton, IClickable // Reimplement to get the new Draw() method
    {
        private EggSpot Spot;

        public EggButton(Bitmap icon, ProportionalRectangle position, EggSpot spot) : base(icon, position)
        {
            Spot = spot;
            HighlightOn = false;
        }

        public new void Draw(Graphics g, GameState state, int width, int height)
        {
            if (Position is null)
                throw new InvalidOperationException("Cannot draw button with uninitialized position.");

            else if (Position is ProportionalRectangle p && Enabled && Spot.HasEgg())
                g.DrawImage(Icon, p.GetAbsolute(width, height));
        }

        protected override void Action(GameState state)
        {
            if (state.CurrentTool is Tool t && state.HeldProduct is null)
                t.Use(state, Spot);
        }
    }

    [System.Runtime.Versioning.SupportedOSPlatform("windows")]
    public sealed class ToolExitButton : GameButton
    {
        private MenuHandler ToolMenu;
        public ToolExitButton(MenuHandler toolMenu, Bitmap icon) : base(icon)
        {
            ToolMenu = toolMenu;
        }

        public ToolExitButton(MenuHandler toolMenu, Bitmap icon, ProportionalRectangle position) : base(icon, position)
        {
            ToolMenu = toolMenu;
        }

        protected override void Action(GameState state)
        {
            state.CurrentTool = null;
            ToolMenu.Enable();
            this.Disable();
        }
    }

    #endregion


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

        public void AddToEnable(IClickable clickable)
        {
            foreach (var b in Buttons)
            {
                b.ToEnable.Add(clickable);
            }
        }

        public void AddToDisable(IClickable clickable)
        {
            foreach (var b in Buttons)
            {
                b.ToDisable.Add(clickable);
            }
        }

        public void Add(GameButton button) => Buttons.Add(button);

        // TODO do better, maybe just stretch to max or something, this is real difficult to use
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

        public virtual void Click(double x, double y, GameState state)
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
    public class ToolMenuHandler : MenuHandler
    {
        private ToolExitButton? exit;
        public ToolMenuHandler(Bitmap background, ProportionalRectangle backgroundPosition) : base(background, backgroundPosition) { }

        public void SetExitButton(ToolExitButton exitButton)
        {
            exit = exitButton;
        }

        public override void Click(double x, double y, GameState state)
        {
            base.Click(x, y, state);
            if (state.CurrentTool is not null)
            {
                exit?.Enable();
                this.Disable();
            }
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
            NamedAssets.Load("Worm", "Assets\\Worm.png");
            NamedAssets.Load("Dead", "Assets\\Dead.png");

            NamedAssets.Load("Plot-watered", "Assets\\Plot-watered-center.png");
            NamedAssets.Load("Plot-highlighted", "Assets\\Plot-highlighted-center.png");
            NamedAssets.Load("Plot-both", "Assets\\Plot-both.png");
            NamedAssets.Load("Plots-default", "Assets\\Plots-default.png");


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

    [System.Runtime.Versioning.SupportedOSPlatform("windows")]
    public class FeederDisplay : IClickable
    {
        private ProportionalRectangle[] feederCoords;
        private Bitmap Feed, Background;

        public ProportionalRectangle BackgroundPosition { get; init; }

        public bool Enabled { get; private set; }

        public FeederDisplay(Bitmap background, Bitmap feed, ProportionalRectangle backgroundPosition)
        {
            Background = background;
            Feed = feed;
            BackgroundPosition = backgroundPosition;

            //TODO hardcoded
            feederCoords = new ProportionalRectangle[5];
            feederCoords[0] = new ProportionalRectangle(0.24, 0.327, 0.4, 0.72);
            feederCoords[1] = new ProportionalRectangle(0.312, 0.40, 0.4, 0.72);
            feederCoords[2] = new ProportionalRectangle(0.385, 0.472, 0.4, 0.72);
            feederCoords[3] = new ProportionalRectangle(0.457, 0.544, 0.4, 0.72);
            feederCoords[4] = new ProportionalRectangle(0.529, 0.616, 0.4, 0.72);

            Enabled = true;
        }


        public void Disable() => Enabled = false;

        public void Enable() => Enabled = true;

        public void Draw(Graphics g, GameState state, int width, int height)
        {
            if (!Enabled)
                return;

            g.DrawImage(Background, BackgroundPosition.GetAbsolute(width, height));

            for (int i = 0; i < state.CurrentCoop.Feeder.NumFilled; i++)
            {
                if (i > feederCoords.Length)
                    throw new InvalidOperationException($"Can't fill {i}th feeder, capacity is only {feederCoords.Length}.");

                g.DrawImage(Feed, feederCoords[i].GetAbsolute(width, height));
            }
        }

        public void Click(double x, double y, GameState state)
        {
            if (!Enabled)
                return;

            if (BackgroundPosition.InArea(x, y) && state.CurrentTool is Tool t && state.HeldProduct is null)
                t.Use(state, state.CurrentCoop.Feeder);
        }

        public void Hover(double x, double y, GameState state) { }
    }
    #endregion



    [System.Runtime.Versioning.SupportedOSPlatform("windows")]
    public class CursorHandler : IDrawable
    {
        private ProportionalRectangle Position { get; set; } = new();
        private ToolIconLoader? ToolAssets;
        private SellableLoader? SellAssets;

        public static double CURSOR_WIDTH = 0.08;
        public static double CURSOR_HEIGHT = 0.14;

        public void SetToolIcons(ToolIconLoader toolAssets) => ToolAssets = toolAssets;

        public void SetSellableIcons(SellableLoader assets) => SellAssets = assets;

        public void Draw(Graphics g, GameState state, int absoluteWidth, int absoluteHeight)
        {
            if (ToolAssets is ToolIconLoader ta && state.CurrentTool is Tool t)
                g.DrawImage(ta.GetImage(t.GetType()), Position.GetAbsolute(absoluteWidth, absoluteHeight));
            else if (SellAssets is SellableLoader sl && state.HeldProduct is ISellable s)
                g.DrawImage(sl.GetImage(s.GetType()), Position.GetAbsolute(absoluteWidth, absoluteHeight));
        }

        public void UpdatePosition(double x, double y)
        {
            Position = new ProportionalRectangle(x-CURSOR_WIDTH/2, x+CURSOR_WIDTH/2, y-CURSOR_HEIGHT/2, y+CURSOR_HEIGHT/2);
        }
    }

    [System.Runtime.Versioning.SupportedOSPlatform("windows")]
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

    [System.Runtime.Versioning.SupportedOSPlatform("windows")]
    public class StaminaDisplay(ProportionalRectangle position, Bitmap background, Bitmap level, Bitmap empty, Bitmap top) : IDrawable
    {
        public void Draw(Graphics g, GameState state, int width, int height)
        {
            g.DrawImage(background, position.GetAbsolute(width, height));

            if (!state.CanWork())
                g.DrawImage(empty, position.GetAbsolute(width, height));
            else
            {
                // Crop out bottom part of level picture
                int y = (int)(level.Height * (1 - state.Stamina));
                int h = (int)(level.Height * state.Stamina);
                Rectangle CropArea = new Rectangle(0, y, level.Width, h);

                // Display at the bottom of stamina display
                double top = position.Y1 + (position.Y2 - position.Y1) * (1 - state.Stamina);
                ProportionalRectangle newPosition = new ProportionalRectangle(position.X1, position.X2, top, position.Y2);
                using (Bitmap newLevel = level.Clone(CropArea, level.PixelFormat))
                    g.DrawImage(newLevel, newPosition.GetAbsolute(width, height));
            }


            g.DrawImage(top, position.GetAbsolute(width, height));
        }
    }



    #region scene handlers
    [System.Runtime.Versioning.SupportedOSPlatform("windows")]
    public abstract class SceneHandler : IDrawable
    {
        // Background that is drawn first
        protected Bitmap? Background { get; init; }

        // All controls that are displayed by the base class behavior
        protected List<IClickable> Clickables = [];
        
        // Cursor
        protected CursorHandler Cursor = new();

        // Things to display on top of everything
        protected List<IDrawable> topIcons = [];

        public virtual void Draw(Graphics g, GameState state, int absoluteWidth, int absoluteHeight)
        {
            // Background
            if (Background is Bitmap b)
                g.DrawImage(b, 0, 0, absoluteWidth, absoluteHeight);

            // Controls
            DrawClickables(g, state, absoluteWidth, absoluteHeight);

            // Cursor
            Cursor.Draw(g, state, absoluteWidth, absoluteHeight);

            // Top icons
            DrawTopIcons(g, state, absoluteWidth, absoluteHeight);
        }

        public void AddTopIcon(IDrawable icon)
        {
            topIcons.Add(icon);
        }

        protected void DrawClickables(Graphics g, GameState state, int absolueWidth, int absoluteHeight)
        {
            foreach (IClickable clickable in Clickables)
                clickable.Draw(g, state, absolueWidth, absoluteHeight);
        }

        protected void DrawTopIcons(Graphics g, GameState state, int absoluteWidth, int absoluteHeight)
        {
            foreach (IDrawable item in topIcons)
                item.Draw(g, state, absoluteWidth, absoluteHeight);
        }

        public virtual void HandleClick(double x, double y, GameState state)
        {
            foreach(IClickable clickable in Clickables)
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
        private List<ProportionalRectangle> farmCoords;

        private SceneSwitchButton ArrowButton;
        private SceneSwitchButton HouseButton;
        private SceneSwitchButton CoopButton;

        public MainSceneHandler()
        {
            // Load assets
            Background = new Bitmap("Assets\\Farmer-even.png");

            // Initialize farm interaction areas
            // TODO remake as clickables
            farmCoords = new List<ProportionalRectangle>();
            double[] XBounds = { 0.02, 0.45, 0.54, 0.98 };
            double[] YBounds = { 0.37, 0.66, 0.70, 0.98 };

            farmCoords.Add(new ProportionalRectangle(XBounds[0], XBounds[1], YBounds[0], YBounds[1]));
            farmCoords.Add(new ProportionalRectangle(XBounds[2], XBounds[3], YBounds[0], YBounds[1]));
            farmCoords.Add(new ProportionalRectangle(XBounds[0], XBounds[1], YBounds[2], YBounds[3]));
            farmCoords.Add(new ProportionalRectangle(XBounds[2], XBounds[3], YBounds[2], YBounds[3]));

            ArrowButton = new SceneSwitchButton(new Bitmap("Assets\\ArrowMain.png"), new ProportionalRectangle(0.48, 0.52, 0.84, 0.99), View.RoadView);
            ArrowButton.EnableStamina();
            Clickables.Add(ArrowButton);

            HouseButton = new SceneSwitchButton(new Bitmap("Assets\\Coop-button.png"), new ProportionalRectangle(0.77, 0.94, 0.13, 0.352), View.CoopView);
            HouseButton.HighlightOn = false;
            Clickables.Add(HouseButton);

            CoopButton = new SceneSwitchButton(new Bitmap("Assets\\House-button.png"), new ProportionalRectangle(0.39, 0.61, 0.01, 0.352), View.HouseView);
            CoopButton.HighlightOn = false;
            Clickables.Add(CoopButton);

        }

        public override void HandleClick(double X, double Y, GameState state)
        {
            base.HandleClick(X, Y, state);

            // TODO redo farms as clickables
            // See if inside a farm
            for (uint i = 0; i < farmCoords.Count; i++)
            {
                if (farmCoords[(int)i].InArea(X, Y))
                {
                    state.CurrentView = View.FarmView;
                    state.SetFarm(i);
                    break;
                }
            }
        }
    }

    [System.Runtime.Versioning.SupportedOSPlatform("windows")]
    public class FarmSceneHandler : SceneHandler
    {
        // Loaders
        private ToolIconLoader ToolIconLoader = new();
        private PlantStatesLoader PlantAssets = new();
        private SellableLoader FruitAssets = new();

        // Menus
        private ToolMenuHandler ToolMenu;
        private ToolExitButton ExitButton;
        private MenuHandler PlantMenu;

        private FarmDisplay Farm;

        // Buttons
        private GameButton PlantMenuButton;
        private GameButton HarvestButton;
        private GameButton BackButton;

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

            ExitButton = new ToolExitButton(ToolMenu, new Bitmap("Assets\\Exit.png"), new ProportionalRectangle(0.87, 0.97, 0.81, 0.99));
            ExitButton.Disable();
            ToolMenu.SetExitButton(ExitButton);

            // Planting menu
            PlantMenu = new MenuHandler(new Bitmap("Assets\\Center-menu.png"), new ProportionalRectangle(0.16, 0.84, 0.10, 0.90));
            PlantMenu.Add(new PlantButton(FruitAssets.GetImage(typeof(RaddishFruit)), new RaddishSeed()));
            PlantMenu.Add(new PlantButton(FruitAssets.GetImage(typeof(CarrotFruit)), new CarrotSeed()));
            PlantMenu.Add(new PlantButton(FruitAssets.GetImage(typeof(TomatoFruit)), new TomatoSeed()));
            PlantMenu.Add(new PlantButton(FruitAssets.GetImage(typeof(PotatoFruit)), new PotatoSeed()));

            PlantMenu.RepositionButtons(0.10, 0.18, 0.03, 0.07);
            PlantMenu.Disable();

            // Initialize buttons:
            HarvestButton = new HarvestButton(new Bitmap("Assets\\Harvest-house.png"), new ProportionalRectangle(0.04, 0.18, 0.59, 0.91));
            BackButton = new SceneSwitchButton(new Bitmap("Assets\\Back-arrow.png"), new ProportionalRectangle(0.88, 0.965, 0.82, 0.975), View.FullView);
            PlantMenuButton = new PlantMenuButton(new Bitmap("Assets\\Planting-plant.png"), new ProportionalRectangle(0.01, 0.20, 0.18, 0.44), PlantMenu);

            Farm = new FarmDisplay(PlantAssets);

            // Handle enable/disable
            PlantMenuButton.ToDisable.Add(Farm);
            PlantMenuButton.ToDisable.Add(ToolMenu);
            PlantMenu.AddToEnable(Farm);
            PlantMenu.AddToEnable(ToolMenu);

            BackButton.ToDisable.Add(PlantMenu);
            BackButton.ToEnable.Add(PlantMenuButton);
            BackButton.ToEnable.Add(Farm);
            BackButton.ToEnable.Add(ToolMenu);

            ExitButton.ToEnable.Add(BackButton);
            ToolMenu.AddToDisable(BackButton);

            HarvestButton.ToEnable.Add(ToolMenu);
            HarvestButton.ToEnable.Add(BackButton);
            HarvestButton.ToDisable.Add(ExitButton);

            // Add controls to clickable list
            // (not Farm, because that is handled separately)
            Clickables.Add(HarvestButton);
            Clickables.Add(PlantMenuButton);
            Clickables.Add(ToolMenu);
            Clickables.Add(PlantMenu);
            Clickables.Add(BackButton);
            Clickables.Add(ExitButton);
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
                ExitButton.Disable();
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
    }

    [System.Runtime.Versioning.SupportedOSPlatform("windows")]
    public class RoadSceneHandler : SceneHandler
    {
        private SceneSwitchButton SeedShop, ChickShop;
        public RoadSceneHandler()
        {
            // Load assets
            Background = new Bitmap("Assets\\Shops-background.png");

            SeedShop = new SceneSwitchButton(new Bitmap("Assets\\ShopHouse.png"), new ProportionalRectangle(0.06, 0.37, 0.09, 0.79), View.SeedShopView);
            SeedShop.HighlightOn = false;
            ChickShop = new SceneSwitchButton(new Bitmap("Assets\\ShopHouse.png"), new ProportionalRectangle(0.63, 0.94, 0.09, 0.79), View.ChickShopView);
            ChickShop.HighlightOn = false;

            Clickables.Add(SeedShop);
            Clickables.Add(ChickShop);
            Clickables.Add(new SceneSwitchButton(new Bitmap("Assets\\Arrow-shops.png"), new ProportionalRectangle(0.45, 0.56, 0.07, 0.33), View.FullView));
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
            BackButton = new SceneSwitchButton(new Bitmap("Assets\\Back-arrow.png"), new ProportionalRectangle(0.88, 0.965, 0.82, 0.975), View.FullView);
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

            base.Draw(g, state, absoluteWidth, absoluteHeight);

            // Initialize new chicken and egg positions if needed
            for  (int i = ChickenPositions.Count; i < state.CurrentCoop.ChickenCount; i++)
            {
                ChickenPositions.Add(GetNewPosition());
                // Sort by Y position, so that higher chickens are further back
                ChickenPositions.Sort(Comparer<ProportionalRectangle>.Create((p1, p2) => p1.Y1.CompareTo(p2.Y1)));

                // TODO this might break when loading
                EggSpots.Add(new EggButton(EggAssets.GetImage(typeof(Egg)), GetNewPosition(), state.CurrentCoop.GetEggSpots()[i]));
                Clickables.Add(EggSpots[i]);
            }

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

        public ShopSceneHandler()
        {
            Background = new Bitmap("Assets\\Shop.png");

            ShoppingMenu = new MenuHandler(new Bitmap("Assets\\Shop-menu.png"), new ProportionalRectangle(0.06, 0.69, 0.13, 0.87));

            Clickables.Add(new SceneSwitchButton(new Bitmap("Assets\\Arrow-shop.png"), new ProportionalRectangle(0.79, 0.98, 0.01, 0.18), View.RoadView));
            Clickables.Add(ShoppingMenu);
        }

        public void AddStock(IBuyable item, Bitmap icon)
        {
            ShoppingMenu.Add(new BuyButton(icon, item));
            ShoppingMenu.RepositionButtons(0.11, 0.2, 0.01, 0.04);
        }
    }

    [System.Runtime.Versioning.SupportedOSPlatform("windows")]
    public class HouseSceneHandler : SceneHandler
    {
        public HouseSceneHandler()
        {
            Background = new Bitmap("Assets\\House.png");

            Clickables.Add(new SceneSwitchButton(new Bitmap("Assets\\Back-arrow.png"), new ProportionalRectangle(0.88, 0.965, 0.82, 0.975), View.FullView));
            Clickables.Add(new NewDayButton(new Bitmap("Assets\\New-day.png"), new ProportionalRectangle(0.02, 0.14, 0.12, 0.35)));
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

        private MoneyDisplay Money;
        private StaminaDisplay Stamina;

        public int width, height;
        // TODO better access

        public FarmerGraphics(GameState gameState)
        {
            this.gameState = gameState;

            width = 960;
            height = 540;
            //TODO better resize handling

            SeedShopScene.AddStock(new RaddishSeed(), new Bitmap("Assets\\Raddish.png"));
            SeedShopScene.AddStock(new CarrotSeed(), new Bitmap("Assets\\Carrot.png"));
            SeedShopScene.AddStock(new PotatoSeed(), new Bitmap("Assets\\Potato.png"));
            SeedShopScene.AddStock(new TomatoSeed(), new Bitmap("Assets\\Tomato.png"));

            ChickShopScene.AddStock(new Chicken(), new Bitmap("Assets\\Chicken.png"));
            //ChickShopScene.AddStock(new Bag(), new Bitmap("Assets\\Bag.png"));

            Money = new MoneyDisplay(new Bitmap("Assets\\Money.png"), new ProportionalRectangle(0.01, 0.14, 0.02, 0.13));
            Stamina = new StaminaDisplay(new ProportionalRectangle(0.9, 0.98, 0.02, 0.15),
                                         new Bitmap("Assets\\Stamina-background.png"),
                                         new Bitmap("Assets\\Stamina-level.png"),
                                         new Bitmap("Assets\\Stamina-empty.png"),
                                         new Bitmap("Assets\\Stamina-top.png"));

            // Add icons to scenes
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

// TODO plan:
// do chicken shop
// deal with text displays (amounts and prices)
// saving
// challenges & events
// housekeeping (restructure, TODOs)
// Testing chicken
// Docs
// Presentation
// Possibly: More plants
// Possibly: Extensible scene loaders
