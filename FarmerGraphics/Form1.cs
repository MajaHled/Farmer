using FarmerLibrary;
using System.Reflection;

namespace FarmerGraphics
{
    public partial class Farmer : Form
    {
        double AspectRatio { get; init; }
        public Farmer()
        {
            InitializeComponent();

            AspectRatio = Height / Width;

            //Panel needs to be double buffered to display properly
            typeof(Panel).InvokeMember("DoubleBuffered", BindingFlags.SetProperty | BindingFlags.Instance | BindingFlags.NonPublic, null, panel1, new object[] { true });
            panel1.Width = 960;
            panel1.Height = 540;
            panel1.BackColor = Color.Red;

            gameState = GameState.GetClassicStartingState();
            gameSceneHandler = new FarmerGraphics(gameState);
        }

        private GameState gameState;
        private FarmerGraphics gameSceneHandler;

        private void Form1_Load(object sender, EventArgs e) { }

        private void panel1_Paint(object sender, PaintEventArgs e)
        {
            gameSceneHandler.Paint(e.Graphics);
        }

        private void panel1_MouseClick(object sender, MouseEventArgs e)
        {
            gameSceneHandler.HandleClick(e.X, e.Y);
            Refresh();
        }

        private void panel1_MouseMove(object sender, MouseEventArgs e)
        {
            gameSceneHandler.HandleMouseMove(e.X, e.Y);
            Refresh();
        }

        private void Farmer_Resize(object sender, EventArgs e)
        {
            // TODO keep aspect ratio
        }

        private void panel1_Resize(object sender, EventArgs e)
        {
            gameSceneHandler.width = panel1.Width;
            gameSceneHandler.height = panel1.Height;
            Refresh();
        }
    }
}
