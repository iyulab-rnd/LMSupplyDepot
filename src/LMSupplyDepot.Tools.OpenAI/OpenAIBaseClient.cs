using System.Net.Http.Headers;

namespace LMSupplyDepot.Tools.OpenAI;

/// <summary>
/// Base client for interacting with OpenAI APIs
/// </summary>
public abstract class OpenAIBaseClient
{
    protected readonly HttpClient _httpClient;
    protected readonly JsonSerializerOptions _jsonOptions;
    protected const string ApiVersion = "v1";
    protected const string BaseUrl = "https://api.openai.com";

    /// <summary>
    /// Initializes a new instance of the <see cref="OpenAIBaseClient"/> class
    /// </summary>
    protected OpenAIBaseClient(string apiKey, HttpClient httpClient = null)
    {
        if (string.IsNullOrEmpty(apiKey))
        {
            throw new ArgumentNullException(nameof(apiKey), "API key cannot be null or empty");
        }

        _httpClient = httpClient ?? new HttpClient();
        _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", apiKey);

        _jsonOptions = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
        };
    }

    /// <summary>
    /// Sets up the Assistants API Beta header
    /// </summary>
    protected void SetupAssistantsApiHeader()
    {
        _httpClient.DefaultRequestHeaders.Add("OpenAI-Beta", "assistants=v2");
    }

    /// <summary>
    /// Sends a request to the OpenAI API
    /// </summary>
    /// <typeparam name="T">The type of the response</typeparam>
    protected async Task<T> SendRequestAsync<T>(HttpMethod method, string endpoint, object requestBody = null, CancellationToken cancellationToken = default)
    {
        var url = $"{BaseUrl}{endpoint}";
        var request = new HttpRequestMessage(method, url);

        if (requestBody != null)
        {
            var json = JsonSerializer.Serialize(requestBody, _jsonOptions);
            request.Content = new StringContent(json, Encoding.UTF8, "application/json");
        }

        var response = await _httpClient.SendAsync(request, cancellationToken);
        await EnsureSuccessStatusCodeAsync(response);

        var responseContent = await response.Content.ReadAsStringAsync();
        return JsonSerializer.Deserialize<T>(responseContent, _jsonOptions);
    }

    /// <summary>
    /// Sends a raw HTTP request and returns the HttpResponseMessage
    /// </summary>
    public async Task<HttpResponseMessage> SendRawRequestAsync(HttpRequestMessage request, CancellationToken cancellationToken = default)
    {
        // API 키가 이미 설정되어 있는지 확인
        if (request.Headers.Authorization == null)
        {
            request.Headers.Authorization = _httpClient.DefaultRequestHeaders.Authorization;
        }

        // OpenAI-Beta 헤더가 설정되어 있는지 확인 (Assistants API용)
        if (_httpClient.DefaultRequestHeaders.Contains("OpenAI-Beta") && !request.Headers.Contains("OpenAI-Beta"))
        {
            foreach (var betaHeader in _httpClient.DefaultRequestHeaders.GetValues("OpenAI-Beta"))
            {
                request.Headers.Add("OpenAI-Beta", betaHeader);
            }
        }

        // 요청 전송
        var response = await _httpClient.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, cancellationToken);

        // 오류 확인
        await EnsureSuccessStatusCodeAsync(response);

        return response;
    }

    /// <summary>
    /// Ensures that the response has a success status code, otherwise throws an OpenAIException with details
    /// </summary>
    protected async Task EnsureSuccessStatusCodeAsync(HttpResponseMessage response)
    {
        if (response.IsSuccessStatusCode)
        {
            return;
        }

        var content = await response.Content.ReadAsStringAsync();
        var errorMessage = $"API error: {response.StatusCode}";
        string errorType = null;
        string errorCode = null;

        try
        {
            var errorResponse = JsonSerializer.Deserialize<JsonElement>(content);
            if (errorResponse.TryGetProperty("error", out var error))
            {
                if (error.TryGetProperty("message", out var message))
                {
                    errorMessage = message.GetString();
                }

                if (error.TryGetProperty("type", out var type))
                {
                    errorType = type.GetString();
                }

                if (error.TryGetProperty("code", out var code))
                {
                    errorCode = code.GetString();
                }
            }
        }
        catch
        {
            // If we can't parse the error, just use the status code
        }

        throw new OpenAIException(errorMessage, response.StatusCode, errorType, errorCode);
    }

    /// <summary>
    /// Builds query parameters for a URL
    /// </summary>
    protected string BuildQueryString(Dictionary<string, string> parameters)
    {
        if (parameters == null || parameters.Count == 0)
        {
            return string.Empty;
        }

        var queryParams = new List<string>();
        foreach (var param in parameters)
        {
            if (!string.IsNullOrEmpty(param.Value))
            {
                queryParams.Add($"{param.Key}={Uri.EscapeDataString(param.Value)}");
            }
        }

        return queryParams.Count > 0 ? "?" + string.Join("&", queryParams) : string.Empty;
    }

    /// <summary>
    /// Gets the API key from the authorization header
    /// </summary>
    protected string GetApiKey()
    {
        return _httpClient.DefaultRequestHeaders.Authorization!.Parameter;
    }
}