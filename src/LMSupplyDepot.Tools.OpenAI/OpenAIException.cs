using System;
using System.Net;

namespace LMSupplyDepot.Tools.OpenAI;

/// <summary>
/// Exception thrown when an error occurs while communicating with the OpenAI API
/// </summary>
public class OpenAIException : Exception
{
    /// <summary>
    /// The HTTP status code returned by the API
    /// </summary>
    public HttpStatusCode StatusCode { get; }

    /// <summary>
    /// The type of error returned by the API
    /// </summary>
    public string ErrorType { get; }

    /// <summary>
    /// The error code returned by the API
    /// </summary>
    public string ErrorCode { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="OpenAIException"/> class
    /// </summary>
    public OpenAIException(string message, HttpStatusCode statusCode, string? errorType = null, string? errorCode = null)
        : base(message)
    {
        StatusCode = statusCode;
        ErrorType = errorType;
        ErrorCode = errorCode;
    }

    /// <summary>
    /// Determines if the exception was caused by a rate limit being exceeded
    /// </summary>
    public bool IsRateLimitError =>
        StatusCode == HttpStatusCode.TooManyRequests ||
        ErrorType == "rate_limit_exceeded";

    /// <summary>
    /// Determines if the exception was caused by an authentication error
    /// </summary>
    public bool IsAuthenticationError =>
        StatusCode == HttpStatusCode.Unauthorized ||
        ErrorType == "invalid_request_error" && ErrorCode == "invalid_api_key";

    /// <summary>
    /// Determines if the exception was caused by a resource not being found
    /// </summary>
    public bool IsNotFoundError =>
        StatusCode == HttpStatusCode.NotFound;

    /// <summary>
    /// Determines if the exception was caused by a server error
    /// </summary>
    public bool IsServerError =>
        (int)StatusCode >= 500;
}