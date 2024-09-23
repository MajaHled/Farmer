using FarmerLibrary;

namespace FarmerGraphics
{
    [System.Runtime.Versioning.SupportedOSPlatform("windows")] //Windows only due to Bitmap
    public class NamedAssetsLoader
    {
        private Dictionary<string, Bitmap> LoadedAssets = [];

        public void Load(string name, string path)
        {
            var image = new Bitmap(path);
            LoadedAssets.Add(name, image);
        }

        public Bitmap this[string index]
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
}
