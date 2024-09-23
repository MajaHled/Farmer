namespace FarmerLibrary
{
    public abstract class Tool
    {
        public void Use(GameState state, IToolAcceptor target)
        {
            if (!state.CanWork())
                return;

            bool expended = UseInternal(state, target);
            if (expended)
                state.DoLabor();
        }

        public abstract bool UseInternal(GameState state, IToolAcceptor target);
    }

    #region Tool classes

    public sealed class Hand : Tool
    {
        public override bool UseInternal(GameState state, IToolAcceptor target)
        {
            if (target is Plot plot)
            {
                Fruit? harvest = plot.Harvest();
                if (harvest is Fruit f)
                {
                    state.HeldProduct = f;
                    state.CurrentTool = null;
                    return true;
                }
            }

            if (target is EggSpot spot)
            {
                Egg? collected = spot.Collect();
                if (collected is Egg e)
                {
                    state.HeldProduct = e;
                    state.CurrentTool = null;
                    return true;
                }

            }

            return false;
        }
    }

    public sealed class Pail : Tool
    {
        public override bool UseInternal(GameState state, IToolAcceptor target)
        {
            if (target is Plot plot)
            {
                return plot.Water();
            }
            return false;
        }
    }

    public sealed class Bag : Tool
    {
        public override bool UseInternal(GameState state, IToolAcceptor target)
        {
            if (target is Plot plot)
            {
                return plot.Fertilize();
                // TODO subtract fertilizer
            }

            if (target is ChickenFeeder feeder)
            {
                return feeder.AddFeed();
                // TODO subtract feed
            }

            return false;
        }
    }

    public sealed class Bottle : Tool
    {
        public override bool UseInternal(GameState state, IToolAcceptor target)
        {
            if (target is Plot plot)
                return plot.BugSpray();
            return false;
        }
    }

    public sealed class Scythe : Tool
    {
        public override bool UseInternal(GameState state, IToolAcceptor target)
        {
            if (target is Plot plot)
                return plot.DestroyPlant();
            return false;
        }
    }

    #endregion
}
