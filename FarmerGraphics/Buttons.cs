using FarmerLibrary;

namespace FarmerGraphics
{
    [System.Runtime.Versioning.SupportedOSPlatform("windows")]
    public abstract class GameButton : IClickable
    {
        protected Bitmap Icon;
        protected RelativePosition? Position;

        // Highlight behavior
        protected bool Highlighed = false;
        protected readonly double HIGHLIGHT_MARGIN = 0.01;
        public bool HighlightOn { get; set; } = true;

        public List<IClickable> ToEnable { get; init; }
        public List<IClickable> ToDisable { get; init; }

        public bool Enabled { get; private set; }

        public GameButton(Bitmap icon, RelativePosition position)
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

        public void SetPosition(RelativePosition position) => Position = position;

        public void UnsetPosition() => Position = null;

        public void Disable() => Enabled = false;

        public void Enable() => Enabled = true;

        public void Draw(Graphics g, GameState state, int width, int height)
        {
            if (Position is null)
                throw new InvalidOperationException("Cannot draw button with uninitialized position.");

            else if (Position is RelativePosition p && Enabled)
            {
                if (Highlighed && HighlightOn)
                {
                    var temp = new RelativePosition(p.X1 - HIGHLIGHT_MARGIN, p.X2 + HIGHLIGHT_MARGIN, p.Y1 - HIGHLIGHT_MARGIN, p.Y2 + HIGHLIGHT_MARGIN);
                    g.DrawImage(Icon, temp.GetAbsolute(width, height));
                }
                else
                    g.DrawImage(Icon, p.GetAbsolute(width, height));
            }
        }

        public void Click(double x, double y, GameState state)
        {
            if (Enabled && Position is RelativePosition p && p.InArea(x, y))
            {
                bool actionDone = Action(state);
                if (!actionDone)
                    return;

                foreach (IClickable c in ToEnable)
                {
                    c.Enable();
                }
                foreach (IClickable c in ToDisable)
                {
                    c.Disable();
                }

            }
        }

        protected abstract bool Action(GameState state);

        public void Hover(double x, double y, GameState state)
        {
            if (Enabled && Position is RelativePosition p && p.InArea(x, y))
            {
                Highlighed = true;
                HoverAction(state);
            }
            else
                Highlighed = false;
        }

        protected virtual void HoverAction(GameState state) { }
    }

    [System.Runtime.Versioning.SupportedOSPlatform("windows")]
    public sealed class PlantMenuButton : GameButton
    {
        private MenuHandler PlantMenu;
        public PlantMenuButton(Bitmap icon, RelativePosition position, MenuHandler plantMenu) : base(icon, position)
        {
            PlantMenu = plantMenu;
        }


        protected override bool Action(GameState state)
        {
            PlantMenu.Enable();
            state.CurrentTool = null;
            state.HeldProduct = null;
            return true;
        }
    }

    [System.Runtime.Versioning.SupportedOSPlatform("windows")]
    public sealed class HarvestButton : GameButton
    {
        public HarvestButton(Bitmap icon, RelativePosition position) : base(icon, position) { }

        protected override bool Action(GameState state)
        {
            return state.SellHeld();
        }
    }


    [System.Runtime.Versioning.SupportedOSPlatform("windows")]
    public class SceneSwitchButton : GameButton
    {
        private readonly FarmerLibrary.View Destination;
        private bool takesStamina = false;

        public SceneSwitchButton(Bitmap icon, RelativePosition position, FarmerLibrary.View destination) : base(icon, position)
        {
            Destination = destination;
        }
        public void EnableStamina() => takesStamina = true;
        public void DisableStamina() => takesStamina = false;

        protected override bool Action(GameState state)
        {
            if (takesStamina)
            {
                if (!state.CanWork())
                    return false;
                state.DoLabor();
            }

            state.UpdateChallenges();

            state.CurrentView = Destination;
            state.ResetTemps();
            return true;
        }
    }

    [System.Runtime.Versioning.SupportedOSPlatform("windows")]
    public class FarmMiniature : SceneSwitchButton
    {
        private uint FarmIndex;
        public FarmMiniature(Bitmap icon, RelativePosition position, uint farmIndex) : base(icon, position, FarmerLibrary.View.FarmView)
        {
            FarmIndex = farmIndex;
        }

        protected override bool Action(GameState state)
        {
            bool done = base.Action(state);
            if (done)
                state.SetFarm(FarmIndex);
            return done;
        }
    }


    [System.Runtime.Versioning.SupportedOSPlatform("windows")]
    public sealed class ToolButton : GameButton
    {
        private Tool? Tool;

        public ToolButton(Bitmap icon, RelativePosition position, Tool? tool) : base(icon, position)
        {
            Tool = tool;
        }
        public ToolButton(Bitmap icon, Tool? tool) : base(icon)
        {
            Tool = tool;
        }

        protected override bool Action(GameState state)
        {
            if (state.CurrentTool?.GetType() == Tool?.GetType())
                return false;
            state.CurrentTool = Tool;
            return true;
        }
    }

    [System.Runtime.Versioning.SupportedOSPlatform("windows")]
    public sealed class PlantButton : GameButton
    {
        private Seed ToPlant;
        public ProductTextDisplay? Display;

        public PlantButton(Bitmap icon, RelativePosition position, Seed toPlant) : base(icon, position)
        {
            ToPlant = toPlant;
            HighlightOn = false;
        }
        public PlantButton(Bitmap icon, Seed toPlant) : base(icon)
        {
            ToPlant = toPlant;
            HighlightOn = false;
        }

        public void SetTextDisplay(ProductTextDisplay display) => Display = display;

        protected override bool Action(GameState state)
        {
            return state.PlantSeedToCurrent(ToPlant);
        }

        protected override void HoverAction(GameState state)
        {
            Display?.SetProduct(ToPlant);
        }
    }

    [System.Runtime.Versioning.SupportedOSPlatform("windows")]
    public sealed class BuyButton : GameButton
    {
        private IBuyable Product;
        private ProductTextDisplay? TextDisplay;

        public BuyButton(Bitmap icon, RelativePosition position, IBuyable product) : base(icon, position)
        {
            Product = product;
            HighlightOn = false;
        }
        public BuyButton(Bitmap icon, IBuyable product) : base(icon)
        {
            Product = product;
            HighlightOn = false;
        }

        public void SetProductDisplay(ProductTextDisplay textDisplay) => TextDisplay = textDisplay;

        protected override bool Action(GameState state)
        {
            return state.Buy(Product);
        }

        protected override void HoverAction(GameState state)
        {
            TextDisplay?.SetProduct(Product);
        }
    }

    [System.Runtime.Versioning.SupportedOSPlatform("windows")]
    public sealed class NewDayButton : GameButton
    {
        public NewDayButton(Bitmap icon, RelativePosition position) : base(icon, position) { }

        protected override bool Action(GameState state)
        {
            state.EndDay();
            state.UpdateChallenges();

            return true;
        }
    }

    [System.Runtime.Versioning.SupportedOSPlatform("windows")]
    public sealed class EggButton : GameButton, IClickable // Reimplement to get the new Draw() method
    {
        private int Index;

        public EggButton(Bitmap icon, RelativePosition position, int index) : base(icon, position)
        {
            Index = index;
            HighlightOn = false;
        }

        public new void Draw(Graphics g, GameState state, int width, int height)
        {
            if (Position is null)
                throw new InvalidOperationException("Cannot draw button with uninitialized position.");

            else if (Position is RelativePosition p && Enabled && state.CurrentCoop.GetEggSpots()[Index].HasEgg())
                g.DrawImage(Icon, p.GetAbsolute(width, height));
        }

        protected override bool Action(GameState state)
        {
            if (state.CurrentTool is Tool t && state.HeldProduct is null)
            {
                t.Use(state, state.CurrentCoop.GetEggSpots()[Index]);
                return true;
            }
            return false;
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

        public ToolExitButton(MenuHandler toolMenu, Bitmap icon, RelativePosition position) : base(icon, position)
        {
            ToolMenu = toolMenu;
        }

        protected override bool Action(GameState state)
        {
            if (state.CurrentTool is null)
                return false;

            state.CurrentTool = null;
            ToolMenu.Enable();
            this.Disable();
            return true;
        }
    }

    [System.Runtime.Versioning.SupportedOSPlatform("windows")]
    public sealed class MenuExitButton : GameButton
    {
        private MenuHandler Menu;
        public MenuExitButton(MenuHandler menu, Bitmap icon) : base(icon)
        {
            Menu = menu;
        }

        public MenuExitButton(MenuHandler menu, Bitmap icon, RelativePosition position) : base(icon, position)
        {
            Menu = menu;
        }

        protected override bool Action(GameState state)
        {
            if (Menu.Enabled)
            {
                Menu.Disable();
                this.Disable();
                return true;
            }
            return false;
        }
    }
}
