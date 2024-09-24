using FarmerLibrary;

namespace FarmerGraphics
{
    public interface IClickable : IDrawable
    {
        public void Click(double x, double y, GameState state);
        public void Hover(double x, double y, GameState state);
        public void Disable();
        public void Enable();
        public bool Enabled { get; }
    }

    [System.Runtime.Versioning.SupportedOSPlatform("windows")]
    public class MenuHandler : IClickable
    {
        private readonly List<GameButton> Buttons;
        public List<GameButton> GetButtonsList() => new List<GameButton>(Buttons);

        private Bitmap Background;
        public RelativePosition BackgroundPosition { get; init; }

        private MenuExitButton? ExitButton;

        private TextDisplay? Text;

        public bool Enabled { get; private set; }

        public MenuHandler(Bitmap background, RelativePosition backgroundPosition)
        {
            Background = background;
            BackgroundPosition = backgroundPosition;
            Enabled = true;
            Buttons = [];
        }

        public void SetExitButton(MenuExitButton button) => ExitButton = button;
        public void SetTextDisplay(TextDisplay text) => Text = text;

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

        public void RepositionButtons(double width, double height, double gap, double padding)
        {
            var startX = BackgroundPosition.X1 + padding;
            var startY = BackgroundPosition.Y1 + padding;

            // If requested width of buttons doesn't allow for even one column
            if (startX + width + padding > BackgroundPosition.X2)
                throw new ArgumentException("Buttons do not fit with specified proportions");

            // If requested height of buttons doesn't allow for even one line
            if (startY + height + padding > BackgroundPosition.Y2)
                throw new ArgumentException("Buttons do not fit with specified proportions");

            foreach (GameButton button in Buttons)
            {
                if (!button.Enabled)
                    continue;

                button.SetPosition(new RelativePosition(startX, startX + width, startY, startY + height));
                startX += width + gap;

                if (startX + width + padding > BackgroundPosition.X2)
                {
                    // new line
                    startX = BackgroundPosition.X1 + gap;
                    startY += height + gap;

                    // If can't fit another line
                    if (startY + height + padding > BackgroundPosition.Y2)
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

            ExitButton?.Click(x, y, state);
        }

        public void Hover(double x, double y, GameState state)
        {
            if (!Enabled)
                return;

            foreach (GameButton button in Buttons)
                button.Hover(x, y, state);

            ExitButton?.Hover(x, y, state);
        }

        public void Draw(Graphics g, GameState state, int width, int height)
        {
            if (!Enabled)
                return;

            g.DrawImage(Background, BackgroundPosition.GetAbsolute(width, height));
            foreach (GameButton button in Buttons)
                button.Draw(g, state, width, height);
            Text?.Draw(g, state, width, height);
            ExitButton?.Draw(g, state, width, height);

        }

        public void Disable()
        {
            Enabled = false;
            ExitButton?.Disable();
            foreach (GameButton button in Buttons)
                button.Disable();
        }

        public void Enable()
        {
            Enabled = true;
            ExitButton?.Enable();
            foreach (GameButton button in Buttons)
                button.Enable();
        }
    }

    [System.Runtime.Versioning.SupportedOSPlatform("windows")]
    public class ToolMenuHandler : MenuHandler
    {
        private ToolExitButton? exit;
        public ToolMenuHandler(Bitmap background, RelativePosition backgroundPosition) : base(background, backgroundPosition) { }

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
        private RelativePosition[,] PlotCoords;
        private PlotStatesLoader PlotStates; 
        private PlantStatesLoader PlantStates;
        private Bitmap Worm, Dead;

        public FarmDisplay(PlantStatesLoader plantStates, PlotStatesLoader plotStates, Bitmap worm, Bitmap dead, RelativePosition[,] plotCoords)
        {
            Enabled = true;
            PlantStates = plantStates;
            PlotStates = plotStates;

            Worm = worm;
            Dead = dead;

            PlotCoords = plotCoords;
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
                    if (PlotCoords[i, j].InArea(x, y))
                    {
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
                    if (PlotCoords[i, j].InArea(x, y))
                    {
                        state.CurrentFarm.Highlight(i, j);
                        break;
                    }
                }
            }
        }

        public void Draw(Graphics g, GameState state, int width, int height)
        {
            g.DrawImage(PlotStates.Default, 0, 0, width, height);

            for (int i = 0; i < state.CurrentFarm.Rows; i++)
            {
                for (int j = 0; j < state.CurrentFarm.Cols; j++)
                {
                    bool watered = state.CurrentFarm[i, j].Watered;
                    bool highlighted = state.CurrentFarm[i, j] == state.CurrentFarm.Highlighted;
                    Rectangle position = PlotCoords[i, j].GetAbsolute(width, height);

                    // Draw plot highlights and watered color
                    if (watered && highlighted)
                    {
                        g.DrawImage(PlotStates.Both, position);
                    }
                    else if (watered)
                    {
                        g.DrawImage(PlotStates.Watered, position);
                    }
                    else if (highlighted)
                    {
                        g.DrawImage(PlotStates.Highlighted, position);
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
                                g.DrawImage(Worm, position);
                            }
                        }
                        else
                        {
                            g.DrawImage(Dead, position);
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
        private Bitmap Feed, Background;
        private RelativePosition BackgroundPosition;
        private RelativePosition[] FeederCoords;

        public bool Enabled { get; private set; }

        public FeederDisplay(Bitmap background, Bitmap feed, RelativePosition backgroundPosition, RelativePosition[] feederCoords)
        {
            Background = background;
            Feed = feed;
            BackgroundPosition = backgroundPosition;
            FeederCoords = feederCoords;

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
                if (i > FeederCoords.Length)
                    throw new InvalidOperationException($"Can't fill {i}th feeder, capacity is only {FeederCoords.Length}.");

                g.DrawImage(Feed, FeederCoords[i].GetAbsolute(width, height));
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

}
