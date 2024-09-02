using FarmerLibrary;

namespace FarmerTests
{
    public class PlotTests
    {
        [Fact]
        public void DestroyPlantWorks()
        {
            Plot testPlot = new Plot();
            Seed testSeed = new RaddishSeed();
            testPlot.PlantASeed(testSeed);

            testPlot.DestroyPlant();

            Assert.True(testPlot.IsEmpty);
        }

        [Fact]
        public void RaddishGrowsWhenWateredEnough()
        {
            Plot testPlot = new Plot();
            Seed testSeed = new RaddishSeed();
            testPlot.PlantASeed(testSeed);
            const int DAYS_TO_WATER = 1;

            for (int i = 0; i < DAYS_TO_WATER; i++)
            {
                testPlot.Water();
                testPlot.EndDay();
            }

            Assert.Equal(testPlot.State, GrowthState.SmallSeedling);
        }

        [Fact]
        public void TomatoGrowsWhenWateredEnough()
        {
            Plot testPlot = new Plot();
            Seed testSeed = new TomatoSeed();
            testPlot.PlantASeed(testSeed);
            const int DAYS_TO_WATER = 4;

            for (int i = 0; i < DAYS_TO_WATER; i++)
            {
                testPlot.Water();
                testPlot.EndDay();
            }

            Assert.Equal(testPlot.State, GrowthState.SmallSeedling);
        }

        [Fact]
        public void RaddishFruitsWhenWateredEnough()
        {
            Plot testPlot = new Plot();
            Seed testSeed = new RaddishSeed();
            testPlot.PlantASeed(testSeed);
            const int DAYS_TO_WATER = 4;

            for (int i = 0; i < DAYS_TO_WATER; i++)
            {
                testPlot.Water();
                testPlot.EndDay();
            }

            Assert.Equal(testPlot.State, GrowthState.Fruiting);
        }

        [Fact]
        public void RaddishHarvestsProperly()
        {
            Plot testPlot = new Plot();
            Seed testSeed = new RaddishSeed();
            testPlot.PlantASeed(testSeed);
            const int DAYS_TO_WATER = 4;

            for (int i = 0; i < DAYS_TO_WATER; i++)
            {
                testPlot.Water();
                testPlot.EndDay();
            }
            Fruit? collectedFruit = testPlot.Harvest();

            Assert.True(collectedFruit is RaddishFruit);
            Assert.True(testPlot.IsEmpty);
        }

        [Fact]
        public void TomatoHarvestsProperly()
        {
            Plot testPlot = new Plot();
            Seed testSeed = new TomatoSeed();
            testPlot.PlantASeed(testSeed);
            const int DAYS_TO_WATER = 16;

            for (int i = 0; i < DAYS_TO_WATER; i++)
            {
                testPlot.Water();
                testPlot.EndDay();
            }
            Fruit? collectedFruit = testPlot.Harvest();

            Assert.True(collectedFruit is TomatoFruit);
            Assert.False(testPlot.IsEmpty);
        }

        [Fact]
        public void UnripeRaddishDoesNotHarvest()
        {
            Plot testPlot = new Plot();
            Seed testSeed = new RaddishSeed();
            testPlot.PlantASeed(testSeed);
            const int DAYS_TO_WATER = 3;
            for (int i = 0; i < DAYS_TO_WATER; i++)
            {
                testPlot.Water();
                testPlot.EndDay();
            }

            Fruit? testFruit = testPlot.Harvest();

            Assert.True(testFruit is null);
            Assert.False(testPlot.IsEmpty);
        }
    }

    public class SeedTests
    {
        [Fact]
        public void RaddishSeedPlantsRaddishPlant()
        {
            Plot testPlot = new Plot();
            Seed testSeed = new RaddishSeed();

            testPlot.PlantASeed(testSeed);

            Assert.True(testPlot.PlantType == typeof(RaddishPlant));
        }
    }

}