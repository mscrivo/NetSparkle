using System;

namespace NetSparkle;

/// <summary>
///     Contains all information for the update detected event
/// </summary>
public class UpdateDetectedEventArgs : EventArgs
{
    /// <summary>
    ///     The next action
    /// </summary>
    public NextUpdateAction NextAction { get; init; }

    /// <summary>
    ///     The latest available version
    /// </summary>
    public NetSparkleAppCastItem? LatestVersion { get; init; }
}