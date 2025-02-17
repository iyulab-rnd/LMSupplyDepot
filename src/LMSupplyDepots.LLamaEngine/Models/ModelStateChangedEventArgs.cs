namespace LMSupplyDepots.LLamaEngine.Models;

public class ModelStateChangedEventArgs : EventArgs
{
    public string ModelIdentifier { get; }
    public LocalModelState OldState { get; }
    public LocalModelState NewState { get; }

    public ModelStateChangedEventArgs(
        string modelIdentifier,
        LocalModelState oldState,
        LocalModelState newState)
    {
        ModelIdentifier = modelIdentifier;
        OldState = oldState;
        NewState = newState;
    }
}