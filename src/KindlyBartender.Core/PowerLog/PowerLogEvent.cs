namespace KindlyBartender.Core.PowerLog;

/// <summary>Something in Power.log that matters for phase detection.</summary>
public abstract record PowerLogEvent;

/// <summary>
/// The <c>PowerTaskList</c> copy of <c>CREATE_GAME</c>, with the game type that
/// <c>GameState.DebugPrintGame()</c> printed before it, or null if none was printed since the last game.
/// </summary>
public sealed record GameCreatedEvent(string? GameType) : PowerLogEvent;

/// <summary>A game entity tag printed inside a <c>CREATE_GAME</c> block; it sets state without a transition.</summary>
public sealed record StartingTagEvent(GameTag Tag, string Value) : PowerLogEvent;

/// <summary>A <c>TAG_CHANGE</c> on the game entity during play.</summary>
public sealed record TagChangedEvent(GameTag Tag, string Value) : PowerLogEvent;

/// <summary>Hearthstone started showing another player's game.</summary>
public sealed record SpectatingStartedEvent : PowerLogEvent;

/// <summary>Hearthstone stopped showing another player's game.</summary>
public sealed record SpectatingEndedEvent : PowerLogEvent;

/// <summary>Hearthstone stopped writing Power.log because it reached its size limit.</summary>
public sealed record LogCappedEvent : PowerLogEvent;
