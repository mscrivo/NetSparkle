﻿namespace NetSparkle;

/// <summary>
///     Every time when NetSparkle detects an update the
///     consumer can decide what should happen as next with the help
///     of the UpdateDetected event
/// </summary>
public enum NextUpdateAction
{
    /// <summary>
    ///     Show the user interface
    /// </summary>
    ShowStandardUserInterface = 1,

    /// <summary>
    ///     Perform an unattended install
    /// </summary>
    PerformUpdateUnattended = 2,

    /// <summary>
    ///     Prohibit the update
    /// </summary>
    ProhibitUpdate = 3
}
