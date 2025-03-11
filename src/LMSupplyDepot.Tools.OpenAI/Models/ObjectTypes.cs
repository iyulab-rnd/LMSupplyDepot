namespace LMSupplyDepot.Tools.OpenAI.Models;

/// <summary>
/// Constants for object types in the Assistants API
/// </summary>
public static class ObjectTypes
{
    public const string Assistant = "assistant";
    public const string Thread = "thread";
    public const string Message = "thread.message";
    public const string Run = "thread.run";
    public const string RunStep = "thread.run.step";
    public const string List = "list";
    public const string VectorStore = "vector_store";
}