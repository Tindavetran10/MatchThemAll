namespace MatchThemAll.Scripts.Power_Ups
{
    /// <summary>
    /// Currencies a power-up can cost. Coins now; future shop types (Gems, etc.) extend this.
    /// </summary>
    public enum ECurrency
    {
        Coins,
        Gems
        // ponytail: future currencies added here — SaveManager.Spend/GetCurrency dispatch per value.
    }
}
