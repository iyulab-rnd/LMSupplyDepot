using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;

namespace LMSupplyDepots.Tools.HuggingFace.Models;

/// <summary>
/// Represents a file in the model repository
/// </summary>
public class Sibling
{
    [JsonPropertyName("rfilename")]
    public string Filename { get; set; } = string.Empty;
}

/// <summary>
/// Represents a Hugging Face model with both strongly-typed properties and dynamic JSON handling.
/// </summary>
[JsonConverter(typeof(HuggingFaceModelConverter))]
public class HuggingFaceModel
{
    // Essential strongly-typed properties
    [JsonPropertyName("_id")]
    public string Id { get; set; } = string.Empty;

    [JsonPropertyName("modelId")]
    public string ModelId { get; set; } = string.Empty;

    [JsonPropertyName("author")]
    public string Author { get; set; } = string.Empty;

    [JsonPropertyName("downloads")]
    public int Downloads { get; set; }

    [JsonPropertyName("likes")]
    public int Likes { get; set; }

    [JsonPropertyName("lastModified")]
    public DateTimeOffset LastModified { get; set; }

    [JsonPropertyName("private")]
    public bool IsPrivate { get; set; }

    [JsonPropertyName("tags")]
    public List<string> Tags { get; set; } = new();

    [JsonPropertyName("pipeline_tag")]
    public string PipelineTag { get; set; } = string.Empty;

    [JsonPropertyName("createdAt")]
    public DateTimeOffset CreatedAt { get; set; }

    [JsonPropertyName("library_name")]
    public string LibraryName { get; set; } = string.Empty;

    // Config storage as dictionary
    [JsonPropertyName("config")]
    public Dictionary<string, JsonElement> Config { get; set; } = new();

    // Dynamic JSON storage
    private readonly JsonDocument? _rawJson;

    [JsonIgnore]
    private readonly Dictionary<string, JsonElement> _additionalProperties = new();

    public HuggingFaceModel()
    {
    }

    [JsonConstructor]
    public HuggingFaceModel(JsonDocument? rawJson = null)
    {
        _rawJson = rawJson;
        if (_rawJson != null)
        {
            // Store all properties in the dictionary
            foreach (var property in _rawJson.RootElement.EnumerateObject())
            {
                var propertyName = property.Name;
                // Skip properties that are already strongly typed
                if (propertyName != "_id" &&
                    propertyName != "modelId" &&
                    propertyName != "author" &&
                    propertyName != "downloads" &&
                    propertyName != "likes" &&
                    propertyName != "lastModified" &&
                    propertyName != "private" &&
                    propertyName != "tags" &&
                    propertyName != "config")
                {
                    _additionalProperties[propertyName] = property.Value.Clone();
                }
            }
        }
    }

    /// <summary>
    /// Gets a strongly-typed value for a specific property.
    /// </summary>
    public T? GetProperty<T>(string propertyName)
    {
        if (_additionalProperties.TryGetValue(propertyName, out var element))
        {
            try
            {
                return element.Deserialize<T>();
            }
            catch (JsonException)
            {
                return default;
            }
        }
        return default;
    }

    /// <summary>
    /// Gets the raw JsonElement for a property.
    /// </summary>
    public JsonElement? GetRawProperty(string propertyName)
    {
        return _additionalProperties.TryGetValue(propertyName, out var element) ? element : null;
    }

    /// <summary>
    /// Gets all file paths from the siblings property, optionally filtered by a regex pattern.
    /// </summary>
    public string[] GetFilePaths(Regex? pattern = null)
    {
        var siblings = GetProperty<List<Sibling>>("siblings");
        if (siblings == null || siblings.Count == 0)
            return [];

        var paths = siblings
            .Select(s => s.Filename)
            .Where(f => !string.IsNullOrEmpty(f));

        if (pattern != null)
            return paths.Where(p => pattern.IsMatch(p)).ToArray();

        return paths.ToArray();
    }

    /// <summary>
    /// Checks if a property exists in the model.
    /// </summary>
    public bool HasProperty(string propertyName)
    {
        return _additionalProperties.ContainsKey(propertyName);
    }

    /// <summary>
    /// Gets all property names available in the model.
    /// </summary>
    public IEnumerable<string> GetAvailableProperties()
    {
        return _additionalProperties.Keys;
    }

    /// <summary>
    /// Custom JSON converter for HuggingFaceModel
    /// </summary>
    public class HuggingFaceModelConverter : JsonConverter<HuggingFaceModel>
    {
        public override HuggingFaceModel Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            using var document = JsonDocument.ParseValue(ref reader);
            var model = new HuggingFaceModel(document);

            // Set essential properties
            if (document.RootElement.TryGetProperty("_id", out var idElement))
            {
                model.Id = idElement.GetString() ?? string.Empty;
            }

            if (document.RootElement.TryGetProperty("modelId", out var modelIdElement))
            {
                model.ModelId = modelIdElement.GetString() ?? string.Empty;
            }

            if (document.RootElement.TryGetProperty("author", out var authorElement))
            {
                model.Author = authorElement.GetString() ?? string.Empty;
            }

            if (document.RootElement.TryGetProperty("downloads", out var downloadsElement))
            {
                model.Downloads = downloadsElement.GetInt32();
            }

            if (document.RootElement.TryGetProperty("likes", out var likesElement))
            {
                model.Likes = likesElement.GetInt32();
            }

            if (document.RootElement.TryGetProperty("lastModified", out var lastModifiedElement))
            {
                model.LastModified = lastModifiedElement.GetDateTimeOffset();
            }

            if (document.RootElement.TryGetProperty("private", out var privateElement))
            {
                model.IsPrivate = privateElement.GetBoolean();
            }

            if (document.RootElement.TryGetProperty("tags", out var tagsElement))
            {
                model.Tags = tagsElement.Deserialize<List<string>>(options) ?? new List<string>();
            }

            if (document.RootElement.TryGetProperty("pipeline_tag", out var pipelineTagElement))
            {
                model.PipelineTag = pipelineTagElement.GetString() ?? string.Empty;
            }

            if (document.RootElement.TryGetProperty("createdAt", out var createdAtElement))
            {
                model.CreatedAt = createdAtElement.GetDateTimeOffset();
            }

            if (document.RootElement.TryGetProperty("library_name", out var libraryNameElement))
            {
                model.LibraryName = libraryNameElement.GetString() ?? string.Empty;
            }

            if (document.RootElement.TryGetProperty("config", out var configElement))
            {
                model.Config = configElement.Deserialize<Dictionary<string, JsonElement>>(options) ?? new();
            }

            return model;
        }

        public override void Write(Utf8JsonWriter writer, HuggingFaceModel value, JsonSerializerOptions options)
        {
            writer.WriteStartObject();

            // Write strongly-typed properties
            writer.WriteString("_id", value.Id);
            writer.WriteString("modelId", value.ModelId);
            writer.WriteString("author", value.Author);
            writer.WriteNumber("downloads", value.Downloads);
            writer.WriteNumber("likes", value.Likes);
            writer.WriteString("lastModified", value.LastModified);
            writer.WriteBoolean("private", value.IsPrivate);

            writer.WritePropertyName("tags");
            JsonSerializer.Serialize(writer, value.Tags, options);

            writer.WriteString("pipeline_tag", value.PipelineTag);
            writer.WriteString("createdAt", value.CreatedAt);
            writer.WriteString("library_name", value.LibraryName);

            // Write additional properties
            foreach (var prop in value._additionalProperties)
            {
                writer.WritePropertyName(prop.Key);
                prop.Value.WriteTo(writer);
            }

            writer.WriteEndObject();
        }
    }
}

/// <summary>
/// Extension methods for working with HuggingFaceModel properties
/// </summary>
public static class HuggingFaceModelExtensions
{
    /// <summary>
    /// Gets a boolean property value with a fallback.
    /// </summary>
    public static bool GetBooleanProperty(this HuggingFaceModel model, string propertyName, bool defaultValue = false)
    {
        var value = model.GetProperty<bool?>(propertyName);
        return value.GetValueOrDefault(defaultValue);
    }

    /// <summary>
    /// Gets an integer property value with a fallback.
    /// </summary>
    public static int GetIntegerProperty(this HuggingFaceModel model, string propertyName, int defaultValue = 0)
    {
        var value = model.GetProperty<int?>(propertyName);
        return value.GetValueOrDefault(defaultValue);
    }

    /// <summary>
    /// Gets a string property value with a fallback.
    /// </summary>
    public static string GetStringProperty(this HuggingFaceModel model, string propertyName, string defaultValue = "")
    {
        return model.GetProperty<string>(propertyName) ?? defaultValue;
    }

    /// <summary>
    /// Gets a list property value with a fallback.
    /// </summary>
    public static List<T> GetListProperty<T>(this HuggingFaceModel model, string propertyName)
    {
        return model.GetProperty<List<T>>(propertyName) ?? new List<T>();
    }
}