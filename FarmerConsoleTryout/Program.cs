using FarmerLibrary;

Seed rs = new RaddishSeed();
Seed ts = new TomatoSeed();
Console.WriteLine($"Seed: {rs}, buy price: {rs.BuyPrice}");
Console.WriteLine($"Seed: {ts}, buy price: {ts.BuyPrice}");

Plot[] plots = { new(), new() };
plots[0].PlantASeed(rs);
plots[1].PlantASeed(ts);

int days = 16;
Console.WriteLine($"Watering for {days} days...");
for (int i = 0; i < days; i++)
{
    foreach (var plot in plots)
    {
        plot.Water();
        plot.EndDay();
    }
}

Console.WriteLine($"Plant: {plots[0].PlantType}, state: {plots[0].State}");
Console.WriteLine($"Plant: {plots[1].PlantType}, state: {plots[1].State}");

Console.WriteLine("Harvesting...");
Fruit?[] fruits = { plots[0].Harvest(), plots[1].Harvest() };

Console.WriteLine($"Fruit: {fruits[0]}, sell price: {fruits[0]?.SellPrice}");
Console.WriteLine($"Fruit: {fruits[1]}, sell price: {fruits[1]?.SellPrice}");
Console.WriteLine($"Plant: {plots[0].PlantType} , state:  {plots[0].State}");
Console.WriteLine($"Plant: {plots[1].PlantType} , state:  {plots[1].State}");