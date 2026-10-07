namespace TowerBridge.API.Models
{
    /// <summary>
    /// Direction of travel through Tower Bridge for a bridge lift.
    /// </summary>
    public enum BridgeLiftDirection
    {
        /// <summary>
        /// The direction could not be determined (for example the site did not
        /// publish a direction for the lift).
        /// </summary>
        Unknown = 0,

        /// <summary>
        /// The vessel is travelling up river.
        /// </summary>
        UpRiver = 1,

        /// <summary>
        /// The vessel is travelling down river.
        /// </summary>
        DownRiver = 2
    }
}
