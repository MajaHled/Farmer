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
            Position = new ProportionalRectangle(x - CURSOR_WIDTH / 2, x + CURSOR_WIDTH / 2, y - CURSOR_HEIGHT / 2, y + CURSOR_HEIGHT / 2);
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

}
