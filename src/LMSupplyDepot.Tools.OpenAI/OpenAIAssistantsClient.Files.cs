namespace LMSupplyDepot.Tools.OpenAI;

/// <summary>
/// Client for interacting with the OpenAI Assistants API - Files functionality
/// </summary>
public partial class OpenAIAssistantsClient
{
    #region Files

    /// <summary>
    /// Uploads a file to OpenAI
    /// </summary>
    public async Task<dynamic> UploadFileAsync(string filePath, string purpose, CancellationToken cancellationToken = default)
    {
        var content = new MultipartFormDataContent();
        content.Add(new StringContent(purpose), "purpose");

        using var fileStream = new FileStream(filePath, FileMode.Open, FileAccess.Read);
        var fileContent = new StreamContent(fileStream);
        var fileName = Path.GetFileName(filePath);
        content.Add(fileContent, "file", fileName);

        var url = $"{BaseUrl}/{ApiVersion}/files";
        var response = await _httpClient.PostAsync(url, content, cancellationToken);
        await EnsureSuccessStatusCodeAsync(response);

        var responseContent = await response.Content.ReadAsStringAsync();
        return JsonSerializer.Deserialize<dynamic>(responseContent, _jsonOptions);
    }

    /// <summary>
    /// Retrieves a file
    /// </summary>
    public async Task<dynamic> RetrieveFileAsync(string fileId, CancellationToken cancellationToken = default)
    {
        return await SendRequestAsync<dynamic>(HttpMethod.Get, $"/{ApiVersion}/files/{fileId}", null, cancellationToken);
    }

    /// <summary>
    /// Retrieves the content of a file
    /// </summary>
    public async Task<Stream> RetrieveFileContentAsync(string fileId, CancellationToken cancellationToken = default)
    {
        var url = $"{BaseUrl}/{ApiVersion}/files/{fileId}/content";
        var response = await _httpClient.GetAsync(url, cancellationToken);
        await EnsureSuccessStatusCodeAsync(response);

        return await response.Content.ReadAsStreamAsync();
    }

    /// <summary>
    /// Lists files
    /// </summary>
    public async Task<dynamic> ListFilesAsync(string? purpose = null, CancellationToken cancellationToken = default)
    {
        var parameters = new Dictionary<string, string>();
        if (!string.IsNullOrEmpty(purpose)) parameters["purpose"] = purpose;

        var queryString = BuildQueryString(parameters);
        return await SendRequestAsync<dynamic>(HttpMethod.Get, $"/{ApiVersion}/files{queryString}", null, cancellationToken);
    }

    /// <summary>
    /// Deletes a file
    /// </summary>
    public async Task<bool> DeleteFileAsync(string fileId, CancellationToken cancellationToken = default)
    {
        await SendRequestAsync<dynamic>(HttpMethod.Delete, $"/{ApiVersion}/files/{fileId}", null, cancellationToken);
        return true;
    }

    #endregion
}