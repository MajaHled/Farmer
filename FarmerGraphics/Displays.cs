using FarmerLibrary;

namespace FarmerGraphics
{
    public interface IDrawable
    {
        public void Draw(Graphics g, GameState state, int width, int height);
    }

    [System.Runtime.Versioning.SupportedOSPlatform("windows")]
    public class CursorHandler : IDrawable
    {
        private RelativePosition Position { get; set; } = new();
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
            Position = new RelativePosition(x - CURSOR_WIDTH / 2, x + CURSOR_WIDTH / 2, y - CURSOR_HEIGHT / 2, y + CURSOR_HEIGHT / 2);
        }
    }

    [System.Runtime.Versioning.SupportedOSPlatform("windows")]
    public class StaminaDisplay(RelativePosition position, Bitmap background, Bitmap level, Bitmap empty, Bitmap top) : IDrawable
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
                RelativePosition newPosition = new RelativePosition(position.X1, position.X2, top, position.Y2);
                using (Bitmap newLevel = level.Clone(CropArea, level.PixelFormat))
                    g.DrawImage(newLevel, newPosition.GetAbsolute(width, height));
            }


            g.DrawImage(top, position.GetAbsolute(width, height));
        }
    }

    [System.Runtime.Versioning.SupportedOSPlatform("windows")]
    public class EventDisplay : IDrawable
    {
        private Dictionary<Type, Bitmap> Visuals = [];

        public void RegisterEvent(Type eventType, Bitmap visual)
        {
            if (!typeof(DayEvent).IsAssignableFrom(eventType))
                throw new ArgumentException($"Can't register non-event type {eventType}.");
            if (Visuals.ContainsKey(eventType))
                Visuals[eventType] = visual;
            else
                Visuals.Add(eventType, visual);
        }

        public void Draw(Graphics g, GameState state, int width, int height)
        {
            foreach (var e in state.TodaysEvents)
                if (Visuals.ContainsKey(e.GetType()))
                    g.DrawImage(Visuals[e.GetType()], 0, 0, width, height);
        }
    }

    [System.Runtime.Versioning.SupportedOSPlatform("windows")]
    public class ChallengeBoard : IDrawable
    {
        private Bitmap Background, ChallengeBackground;
        private RelativePosition Position;
        private List<ChallengeDisplay> Displays = new();
        private double Padding;
        private int ToDisplay;

        public ChallengeBoard(Bitmap background, Bitmap challengeBackground, RelativePosition position, int toDisplay, double padding)
        {
            Background = background;
            ChallengeBackground = challengeBackground;
            Position = position;
            Padding = padding;
            ToDisplay = toDisplay;
        }

        private void UpdateChallenges(ChallengeHandler handler)
        {
            Displays.Clear();

            double lastY = Position.Y1 + Padding;
            double width = ((Position.Y2 - Position.Y1 - Padding) / ToDisplay) - Padding;

            int displayed = 0;
            foreach (Challenge c in handler.GetChallengeList())
            {
                if (displayed >= ToDisplay)
                    break;

                var p = new RelativePosition(Position.X1 + Padding, Position.X2 - Padding, lastY, lastY + width);
                Displays.Add(new ChallengeDisplay(c, ChallengeBackground, p));
                lastY = lastY + width + Padding;

                displayed++;
            }

        }

        public void Draw(Graphics g, GameState state, int width, int height)
        {
            g.DrawImage(Background, Position.GetAbsolute(width, height));

            UpdateChallenges(state.ChallengeHandler);

            foreach (ChallengeDisplay d in Displays)
                d.Draw(g, state, width, height);
        }
    }

}
