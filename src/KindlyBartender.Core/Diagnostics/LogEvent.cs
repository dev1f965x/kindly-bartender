namespace KindlyBartender.Core.Diagnostics;

/// <summary>
/// The only things the diagnostic log can say. Entries carry an event, optionally a number or an enum value, and
/// for errors the exception type and HResult; never free text, which could hold paths with the Windows user name.
/// </summary>
public enum LogEvent
{
    AppStarted,
    AppExiting,
    AnotherCopyDidNotAnswer,
    UnexpectedError,
    BackgroundThreadError,
    UnobservedTaskError,
    SettingsNotLoaded,
    SettingsNotSaved,
    SettingsSaveFailed,
    StartupEntryFailed,
    ListenForOtherCopyFailed,
    HearthstoneStarted,
    HearthstoneExited,
    FindLogFolderFailed,
    FindLastGameFailed,
    ReadPowerLogFailed,
    LinesSkipped,
    PollFailed,
    PhaseStarted,
    DetectionFailing,
    NotifyFailed,
    ShowNotificationFailed,
    NotificationSelectionFailed,
    PlaySoundFailed,
    ShowInFrontFailed,
    BringForwardFailed,
    ReadNotificationModeFailed,
    SetupFinished,
    SetupFailed,
    OpenLinkFailed,
    UpdateCheckFailed,
    UpdateAvailable,
    UninstallCleanupFailed,

    /// <summary>The day's file reached its size limit; nothing more is written that day.</summary>
    LogFull,
}
