using FarmerLibrary;

namespace FarmerGraphics
{
    [System.Runtime.Versioning.SupportedOSPlatform("windows")]
    public abstract class TextDisplay : IDrawable
    {
        protected Bitmap Background;
        protected RelativePosition Position;
        protected StringFormat Format = new StringFormat();
        protected Font Font = new Font("Arial", 16);
        protected Brush Brush = new SolidBrush(Color.Black);

        public TextDisplay(Bitmap background, RelativePosition position)
        {
            Background = background;
            Position = position;

            Format.LineAlignment = StringAlignment.Center;
            Format.Alignment = StringAlignment.Center;
        }

        protected abstract string GenerateText(GameState state);

        public void SetFont(Font font) => Font = font;
        public void SetBrush(Brush brush) => Brush = brush;


        public void Draw(Graphics g, GameState state, int width, int height)
        {
            g.DrawImage(Background, Position.GetAbsolute(width, height));

            string text = GenerateText(state);

            g.DrawString(text, Font, Brush, Position.GetAbsolute(width, height), Format);
        }
    }

    [System.Runtime.Versioning.SupportedOSPlatform("windows")]
    public class BasicTextDisplay : TextDisplay
    {
        private string Text { get; set; } = "";
        public BasicTextDisplay(Bitmap background, RelativePosition position) : base(background, position) { }
        public BasicTextDisplay(string text, Bitmap background, RelativePosition position) : base(background, position)
        {
            Text = text;
        }

        protected override string GenerateText(GameState state) => Text;
    }

    [System.Runtime.Versioning.SupportedOSPlatform("windows")]
    public class MoneyDisplay : TextDisplay
    {
        public MoneyDisplay(Bitmap background, RelativePosition position) : base(background, position) { }

        protected override string GenerateText(GameState state)
        {
            string text = "$" + state.PlayerMoney.ToString();
            if (state.PlayerMoney >= 1_000_000)
                text = "$" + (state.PlayerMoney / 1000000).ToString() + "M";
            else if (state.PlayerMoney >= 10_000)
                text = "$" + (state.PlayerMoney / 1000).ToString() + "k";
            return text;
        }
    }

    [System.Runtime.Versioning.SupportedOSPlatform("windows")]
    public class ProductTextDisplay : TextDisplay
    {
        private IBuyable? Product;

        public void SetProduct(IBuyable product) => Product = product;
        public void UnsetProduct() => Product = null;

        public bool ShowPrice { get; set; } = true;

        public ProductTextDisplay(Bitmap background, RelativePosition position) : base(background, position) { }
        public ProductTextDisplay(IBuyable product, Bitmap background, RelativePosition position) : base(background, position)
        {
            Product = product;
        }
        protected override string GenerateText(GameState state)
        {
            if (Product is IBuyable b)
            {
                int amount, price;
                amount = state.GetOwnedAmount(b);
                price = (int)b.BuyPrice;
                if (ShowPrice)
                    return $"{b.Name}: owned {amount}, price ${price}";
                else
                    return $"{b.Name}: owned {amount}";
            }
            return "";
        }
    }

    [System.Runtime.Versioning.SupportedOSPlatform("windows")]
    public class PointsDisplay : TextDisplay
    {
        public PointsDisplay(Bitmap background, RelativePosition position) : base(background, position) { }

        protected override string GenerateText(GameState state)
        {
            return $"{state.Points} pts";
        }
    }

    public class ChallengeDisplay : TextDisplay
    {
        private Challenge? Challenge;

        public void SetChallenge(Challenge challenge) => Challenge = challenge;
        public void UnsetChallenge() => Challenge = null;

        public ChallengeDisplay(Bitmap background, RelativePosition position) : base(background, position) { }
        public ChallengeDisplay(Challenge challenge, Bitmap background, RelativePosition position) : base(background, position)
        {
            Challenge = challenge;
        }
        protected override string GenerateText(GameState state)
        {
            if (Challenge is Challenge c)
            {
                return $"{c.ChallengeText}\nPoints: {c.Reward}";
            }
            return "";
        }
    }
}
