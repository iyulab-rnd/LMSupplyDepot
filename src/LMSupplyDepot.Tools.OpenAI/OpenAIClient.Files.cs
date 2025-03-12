using File = LMSupplyDepot.Tools.OpenAI.Models.File;

namespace LMSupplyDepot.Tools.OpenAI;

/// <summary>
/// Client for interacting with the OpenAI API - File operations
/// </summary>
public partial class OpenAIClient
{
    #region Files

    /// <summary>
    /// Uploads a file to OpenAI
    /// </summary>
    public async Task<File> UploadFileAsync(string filePath, string purpose, CancellationToken cancellationToken = default)
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
        return JsonSerializer.Deserialize<File>(responseContent, _jsonOptions);
    }

    /// <summary>
    /// Retrieves a file
    /// </summary>
    public async Task<File> RetrieveFileAsync(string fileId, CancellationToken cancellationToken = default)
    {
        return await SendRequestAsync<File>(HttpMethod.Get, $"/{ApiVersion}/files/{fileId}", null, cancellationToken);
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
    public async Task<ListResponse<File>> ListFilesAsync(string? purpose = null, CancellationToken cancellationToken = default)
    {
        var parameters = new Dictionary<string, string>();
        if (!string.IsNullOrEmpty(purpose)) parameters["purpose"] = purpose;

        var queryString = BuildQueryString(parameters);
        return await SendRequestAsync<ListResponse<File>>(HttpMethod.Get, $"/{ApiVersion}/files{queryString}", null, cancellationToken);
    }

    /// <summary>
    /// Deletes a file
    /// </summary>
    public async Task<DeletionResponse> DeleteFileAsync(string fileId, CancellationToken cancellationToken = default)
    {
        return await SendRequestAsync<DeletionResponse>(HttpMethod.Delete, $"/{ApiVersion}/files/{fileId}", null, cancellationToken);
    }

    #endregion
}