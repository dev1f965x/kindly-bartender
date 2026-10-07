namespace KindlyBartender.Core.Detection;

/// <summary>What the tray icon shows. Listed from highest to lowest priority.</summary>
public enum TrayStatus
{
    SetupNeeded,
    RestartNeeded,
    NotWorking,
    Paused,
    WaitingForHearthstone,
    Ready,
}

public sealed record TrayConditions(
    bool SetupNeeded,
    bool RestartNeeded,
    bool DetectionFailing,
    bool Paused,
    bool HearthstoneRunning);

public static class TrayStatusResolver
{
    /// <summary>The tray shows one status at a time: the first that applies, in the order of <see cref="TrayStatus"/>.</summary>
    public static TrayStatus Resolve(TrayConditions conditions) => conditions switch
    {
        { SetupNeeded: true } => TrayStatus.SetupNeeded,
        { RestartNeeded: true } => TrayStatus.RestartNeeded,
        { DetectionFailing: true } => TrayStatus.NotWorking,
        { Paused: true } => TrayStatus.Paused,
        { HearthstoneRunning: false } => TrayStatus.WaitingForHearthstone,
        _ => TrayStatus.Ready,
    };
}
