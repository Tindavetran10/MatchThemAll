namespace Match_Them_All.Scripts.Power_Ups
{
    /// <summary>
    /// Currencies a power-up can cost. Coins now; future shop types (Gems, etc.) extend this.
    /// </summary>
    public enum ECurrency
    {
        Coins
        // ponytail: future currencies (Gems, …) added here — SaveManager.Spend dispatches per value.
    }
}
