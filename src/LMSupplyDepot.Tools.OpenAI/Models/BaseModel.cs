using System;
using System.Text.Json;

namespace LMSupplyDepot.Tools.OpenAI.Models;

/// <summary>
/// Base model that supports both strongly-typed properties and dynamic property access
/// </summary>
public class BaseModel
{
    /// <summary>
    /// Internal dictionary to store additional properties that are not explicitly defined in the model
    /// </summary>
    [JsonExtensionData]
    public Dictionary<string, JsonElement> AdditionalProperties { get; set; } = new Dictionary<string, JsonElement>();

    /// <summary>
    /// Gets a value from the additional properties
    /// </summary>
    /// <typeparam name="T">The type to convert the value to</typeparam>
    public T GetValue<T>(string key)
    {
        if (AdditionalProperties.TryGetValue(key, out JsonElement element))
        {
            return JsonSerializer.Deserialize<T>(element.GetRawText());
        }
        return default;
    }

    /// <summary>
    /// Sets a value in the additional properties
    /// </summary>
    public void SetValue(string key, object value)
    {
        var json = JsonSerializer.Serialize(value);
        var element = JsonSerializer.Deserialize<JsonElement>(json);
        AdditionalProperties[key] = element;
    }

    /// <summary>
    /// Checks if a property exists
    /// </summary>
    public bool HasProperty(string key)
    {
        return AdditionalProperties.ContainsKey(key);
    }

    /// <summary>
    /// Removes a property
    /// </summary>
    public bool RemoveProperty(string key)
    {
        return AdditionalProperties.Remove(key);
    }

    /// <summary>
    /// Tries to get a property value and convert it to the specified type.
    /// </summary>
    public bool TryGetProperty<T>(string key, out T value)
    {
        if (AdditionalProperties.TryGetValue(key, out JsonElement element))
        {
            try
            {
                value = JsonSerializer.Deserialize<T>(element.GetRawText());
                return true;
            }
            catch
            {
                value = default;
                return false;
            }
        }

        value = default;
        return false;
    }
}