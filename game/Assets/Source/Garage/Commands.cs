using Kari.Plugins.Terminal;
using UnityEngine;

namespace Race.Garage
{
    public static class Commands
    {
        // Currently, commands must be static.
        // Also, they are active in any scene.
        // I'm going to add more functionality to the command terminal module, once I know
        // what exactly I want or need.
        
        // TODO:
        // this is a hack; add the possibility of calling instance methods,
        // or make the corresponding objects into singleton objects.
        private static CarProperties CarProperties => GameObject.FindObjectOfType<CarProperties>();
        private static UserProperties UserProperties => GameObject.FindObjectOfType<UserProperties>();


        [Command("add_coins", "Adds the specified amount of coins to the user.")]
        public static void AddCoins(
            [Argument("The amount to add")] int amount)
        {
            var user = UserProperties;
            if (user == null)
            {
                Debug.LogError("Could not find UserProperties.");
                return;
            }
            user.DataModel.currency.coins += amount;
            user.TriggerCurrencyChanged();
        }

        [Command("add_rubies", "Adds the specified amount of rubies to the user.")]
        public static void AddRubies(
            [Argument("The amount to add")] int amount)
        {
            var user = UserProperties;
            if (user == null)
            {
                Debug.LogError("Could not find UserProperties.");
                return;
            }
            user.DataModel.currency.rubies += amount;
            user.TriggerCurrencyChanged();
        }
        
        [Command("add_statvalue", "Adds the specified stat value to the current car.")]
        public static void AddStatValue(
            [Argument("The amount to add")] int amount)
        {
            var car = CarProperties;
            if (car == null)
            {
                Debug.LogError("Could not find CarProperties.");
                return;
            }
            if (!car.IsAnyCarSelected)
            {
                Debug.LogError("No car selected currently.");
                return;
            }

            ref var statsInfo = ref car.CurrentCarInfo.dataModel.statsInfo;
            statsInfo.additionalStatValue += amount;
            statsInfo.ComputeNonSerializedProperties();

            car.TriggerStatsChangedEvent();
        }


        [Command("reset_currency", "Sets the currency of the user to 0 coins and 0 gems.")]
        public static void ResetCurrency()
        {
            var user = UserProperties;
            if (user == null)
            {
                Debug.LogError("Could not find UserProperties.");
                return;
            }
            user.DataModel.currency = Currency.Zero;
            user.TriggerCurrencyChanged();
        }

        [Command("reset_stats", "Sets the stats of the current car to their base stats, and sets the additional stat value to 0.")]
        public static void ResetStats()
        {
            var car = CarProperties;
            if (car == null)
            {
                Debug.LogError("Could not find CarProperties.");
                return;
            }
            if (!car.IsAnyCarSelected)
            {
                Debug.LogError("No car selected currently.");
                return;
            }
            ref var statsInfo = ref car.CurrentCarInfo.dataModel.statsInfo;
            statsInfo.additionalStatValue = 0;
            statsInfo.currentStats.Sync(statsInfo.baseStats);
            statsInfo.ComputeNonSerializedProperties();
            car.TriggerStatsChangedEvent();
        }

    }
}