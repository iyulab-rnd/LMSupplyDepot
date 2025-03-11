namespace LMSupplyDepot.Tools.OpenAI.Models;

/// <summary>
/// Stream event handler delegate
/// </summary>
public delegate void StreamEventHandler(StreamEvent streamEvent);

/// <summary>
/// Message delta handler delegate
/// </summary>
public delegate void MessageDeltaHandler(MessageDelta messageDelta);

/// <summary>
/// Run Step delta handler delegate
/// </summary>
public delegate void RunStepDeltaHandler(RunStepDelta runStepDelta);

/// <summary>
/// Error event handler delegate
/// </summary>
public delegate void ErrorEventHandler(ErrorEvent errorEvent);

/// <summary>
/// Text content update handler delegate
/// </summary>
public delegate void TextContentHandler(string text);