namespace LMSupplyDepot.Tools.OpenAI.Models;

/// <summary>
/// Represents tool resources for assistants, threads, or runs
/// </summary>
public class ToolResources : BaseModel
{
    /// <summary>
    /// FileSearch tool resources
    /// </summary>
    [JsonPropertyName("file_search")]
    public FileSearchToolResources FileSearch { get; set; }

    /// <summary>
    /// CodeInterpreter tool resources
    /// </summary>
    [JsonPropertyName("code_interpreter")]
    public CodeInterpreterToolResources CodeInterpreter { get; set; }

    /// <summary>
    /// Create a ToolResources object for FileSearch with vector store IDs
    /// </summary>
    public static ToolResources CreateForFileSearch(List<string> vectorStoreIds)
    {
        return new ToolResources
        {
            FileSearch = new FileSearchToolResources
            {
                VectorStoreIds = vectorStoreIds
            }
        };
    }

    /// <summary>
    /// Create a ToolResources object for CodeInterpreter with file IDs
    /// </summary>
    public static ToolResources CreateForCodeInterpreter(List<string> fileIds)
    {
        return new ToolResources
        {
            CodeInterpreter = new CodeInterpreterToolResources
            {
                FileIds = fileIds
            }
        };
    }

    /// <summary>
    /// Create a ToolResources object with both FileSearch and CodeInterpreter
    /// </summary>
    public static ToolResources CreateCombined(List<string> vectorStoreIds, List<string> codeInterpreterFileIds)
    {
        var resources = new ToolResources();

        if (vectorStoreIds != null && vectorStoreIds.Count > 0)
        {
            resources.FileSearch = new FileSearchToolResources
            {
                VectorStoreIds = vectorStoreIds
            };
        }

        if (codeInterpreterFileIds != null && codeInterpreterFileIds.Count > 0)
        {
            resources.CodeInterpreter = new CodeInterpreterToolResources
            {
                FileIds = codeInterpreterFileIds
            };
        }

        return resources;
    }
}

/// <summary>
/// Represents FileSearch tool resources
/// </summary>
public class FileSearchToolResources : BaseModel
{
    /// <summary>
    /// The IDs of the vector stores to use for file search
    /// </summary>
    [JsonPropertyName("vector_store_ids")]
    public List<string> VectorStoreIds { get; set; }
}

/// <summary>
/// Represents CodeInterpreter tool resources
/// </summary>
public class CodeInterpreterToolResources : BaseModel
{
    /// <summary>
    /// The IDs of the files to use with code interpreter
    /// </summary>
    [JsonPropertyName("file_ids")]
    public List<string> FileIds { get; set; }
}