using System;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using LMSupplyDepot.Tools.OpenAI.Models;

namespace LMSupplyDepot.Tools.OpenAI;

/// <summary>
/// Client for interacting with the OpenAI Vector Stores API
/// </summary>
public class VectorStoreClient : OpenAIBaseClient
{
    /// <summary>
    /// Initializes a new instance of the <see cref="VectorStoreClient"/> class
    /// </summary>
    public VectorStoreClient(string apiKey, HttpClient httpClient = null)
        : base(apiKey, httpClient)
    {
        SetupAssistantsApiHeader();
    }

    #region Vector Stores

    /// <summary>
    /// Creates a new vector store
    /// </summary>
    public async Task<VectorStore> CreateVectorStoreAsync(CreateVectorStoreRequest request, CancellationToken cancellationToken = default)
    {
        return await SendRequestAsync<VectorStore>(HttpMethod.Post, $"/{ApiVersion}/vector_stores", request, cancellationToken);
    }

    /// <summary>
    /// Creates a new vector store with a name
    /// </summary>
    public async Task<VectorStore> CreateVectorStoreAsync(string name, CancellationToken cancellationToken = default)
    {
        var request = CreateVectorStoreRequest.Create(name);
        return await CreateVectorStoreAsync(request, cancellationToken);
    }

    /// <summary>
    /// Creates a new vector store and waits for it to be fully processed
    /// </summary>
    public async Task<VectorStore> CreateAndPollVectorStoreAsync(CreateVectorStoreRequest request, int pollIntervalMs = 1000, int maxAttempts = 60, CancellationToken cancellationToken = default)
    {
        var vectorStore = await CreateVectorStoreAsync(request, cancellationToken);
        return await PollVectorStoreUntilCompletedAsync(vectorStore.Id, pollIntervalMs, maxAttempts, cancellationToken);
    }

    /// <summary>
    /// Retrieves a vector store
    /// </summary>
    public async Task<VectorStore> RetrieveVectorStoreAsync(string vectorStoreId, CancellationToken cancellationToken = default)
    {
        return await SendRequestAsync<VectorStore>(HttpMethod.Get, $"/{ApiVersion}/vector_stores/{vectorStoreId}", null, cancellationToken);
    }

    /// <summary>
    /// Polls a vector store until it is in the completed state
    /// </summary>
    public async Task<VectorStore> PollVectorStoreUntilCompletedAsync(string vectorStoreId, int pollIntervalMs = 1000, int maxAttempts = 60, CancellationToken cancellationToken = default)
    {
        var attempts = 0;
        while (maxAttempts == 0 || attempts < maxAttempts)
        {
            var vectorStore = await RetrieveVectorStoreAsync(vectorStoreId, cancellationToken);

            if (vectorStore.Status == VectorStoreStatus.Completed)
            {
                return vectorStore;
            }
            else if (vectorStore.Status == VectorStoreStatus.Expired)
            {
                throw new OpenAIException($"Vector store {vectorStoreId} has expired", System.Net.HttpStatusCode.BadRequest, "vector_store_expired");
            }
            else
            {
                // Check the file processing status instead of directly accessing FileCounts
                var fileCounts = vectorStore.GetFileCounts();
                if (fileCounts != null &&
                    fileCounts.InProgress == 0 &&
                    fileCounts.Failed == 0 &&
                    fileCounts.Cancelled == 0 &&
                    fileCounts.Completed == fileCounts.Total)
                {
                    // All files processed
                    return vectorStore;
                }
            }

            attempts++;
            await Task.Delay(pollIntervalMs, cancellationToken);
        }

        throw new TimeoutException($"Vector store {vectorStoreId} did not complete after {maxAttempts} polling attempts");
    }

    /// <summary>
    /// Updates a vector store
    /// </summary>
    public async Task<VectorStore> UpdateVectorStoreAsync(string vectorStoreId, UpdateVectorStoreRequest request, CancellationToken cancellationToken = default)
    {
        return await SendRequestAsync<VectorStore>(HttpMethod.Post, $"/{ApiVersion}/vector_stores/{vectorStoreId}", request, cancellationToken);
    }

    /// <summary>
    /// Deletes a vector store
    /// </summary>
    public async Task<bool> DeleteVectorStoreAsync(string vectorStoreId, CancellationToken cancellationToken = default)
    {
        await SendRequestAsync<dynamic>(HttpMethod.Delete, $"/{ApiVersion}/vector_stores/{vectorStoreId}", null, cancellationToken);
        return true;
    }

    /// <summary>
    /// Lists vector stores
    /// </summary>
    public async Task<ListResponse<VectorStore>> ListVectorStoresAsync(int? limit = null, string order = null, string after = null, string before = null, CancellationToken cancellationToken = default)
    {
        var parameters = new Dictionary<string, string>();
        if (limit.HasValue) parameters["limit"] = limit.Value.ToString();
        if (!string.IsNullOrEmpty(order)) parameters["order"] = order;
        if (!string.IsNullOrEmpty(after)) parameters["after"] = after;
        if (!string.IsNullOrEmpty(before)) parameters["before"] = before;

        var queryString = BuildQueryString(parameters);
        return await SendRequestAsync<ListResponse<VectorStore>>(HttpMethod.Get, $"/{ApiVersion}/vector_stores{queryString}", null, cancellationToken);
    }

    #endregion

    #region Vector Store Files

    /// <summary>
    /// Creates a new vector store file
    /// </summary>
    public async Task<VectorStoreFile> CreateVectorStoreFileAsync(string vectorStoreId, CreateVectorStoreFileRequest request, CancellationToken cancellationToken = default)
    {
        return await SendRequestAsync<VectorStoreFile>(HttpMethod.Post, $"/{ApiVersion}/vector_stores/{vectorStoreId}/files", request, cancellationToken);
    }

    /// <summary>
    /// Creates a new vector store file and waits for it to be fully processed
    /// </summary>
    public async Task<VectorStoreFile> CreateAndPollVectorStoreFileAsync(string vectorStoreId, CreateVectorStoreFileRequest request, int pollIntervalMs = 1000, int maxAttempts = 60, CancellationToken cancellationToken = default)
    {
        var file = await CreateVectorStoreFileAsync(vectorStoreId, request, cancellationToken);
        return await PollVectorStoreFileUntilCompletedAsync(vectorStoreId, file.Id, pollIntervalMs, maxAttempts, cancellationToken);
    }

    /// <summary>
    /// Creates a new vector store file with a file ID
    /// </summary>
    public async Task<VectorStoreFile> CreateVectorStoreFileAsync(string vectorStoreId, string fileId, CancellationToken cancellationToken = default)
    {
        var request = CreateVectorStoreFileRequest.Create(fileId);
        return await CreateVectorStoreFileAsync(vectorStoreId, request, cancellationToken);
    }

    /// <summary>
    /// Retrieves a vector store file
    /// </summary>
    public async Task<VectorStoreFile> RetrieveVectorStoreFileAsync(string vectorStoreId, string fileId, CancellationToken cancellationToken = default)
    {
        return await SendRequestAsync<VectorStoreFile>(HttpMethod.Get, $"/{ApiVersion}/vector_stores/{vectorStoreId}/files/{fileId}", null, cancellationToken);
    }

    /// <summary>
    /// Polls a vector store file until it is in the completed state
    /// </summary>
    public async Task<VectorStoreFile> PollVectorStoreFileUntilCompletedAsync(string vectorStoreId, string fileId, int pollIntervalMs = 1000, int maxAttempts = 60, CancellationToken cancellationToken = default)
    {
        var attempts = 0;
        while (maxAttempts == 0 || attempts < maxAttempts)
        {
            var file = await RetrieveVectorStoreFileAsync(vectorStoreId, fileId, cancellationToken);

            if (file.Status == VectorStoreFileStatus.Completed)
            {
                return file;
            }
            else if (file.Status == VectorStoreFileStatus.Failed)
            {
                var error = file.GetLastError();
                throw new OpenAIException($"Vector store file {fileId} failed: {error?.Code} - {error?.Message}",
                    System.Net.HttpStatusCode.BadRequest, "vector_store_file_failed", error?.Code);
            }
            else if (file.Status == VectorStoreFileStatus.Cancelled)
            {
                throw new OpenAIException($"Vector store file {fileId} was cancelled",
                    System.Net.HttpStatusCode.BadRequest, "vector_store_file_cancelled");
            }

            attempts++;
            await Task.Delay(pollIntervalMs, cancellationToken);
        }

        throw new TimeoutException($"Vector store file {fileId} did not complete after {maxAttempts} polling attempts");
    }

    /// <summary>
    /// Deletes a vector store file
    /// </summary>
    public async Task<bool> DeleteVectorStoreFileAsync(string vectorStoreId, string fileId, CancellationToken cancellationToken = default)
    {
        await SendRequestAsync<dynamic>(HttpMethod.Delete, $"/{ApiVersion}/vector_stores/{vectorStoreId}/files/{fileId}", null, cancellationToken);
        return true;
    }

    /// <summary>
    /// Lists vector store files
    /// </summary>
    public async Task<ListResponse<VectorStoreFile>> ListVectorStoreFilesAsync(string vectorStoreId, int? limit = null, string order = null, string after = null, string before = null, string filter = null, CancellationToken cancellationToken = default)
    {
        var parameters = new Dictionary<string, string>();
        if (limit.HasValue) parameters["limit"] = limit.Value.ToString();
        if (!string.IsNullOrEmpty(order)) parameters["order"] = order;
        if (!string.IsNullOrEmpty(after)) parameters["after"] = after;
        if (!string.IsNullOrEmpty(before)) parameters["before"] = before;
        if (!string.IsNullOrEmpty(filter)) parameters["filter"] = filter;

        var queryString = BuildQueryString(parameters);
        return await SendRequestAsync<ListResponse<VectorStoreFile>>(HttpMethod.Get, $"/{ApiVersion}/vector_stores/{vectorStoreId}/files{queryString}", null, cancellationToken);
    }

    #endregion

    #region Vector Store File Batches

    /// <summary>
    /// Creates a new vector store file batch
    /// </summary>
    public async Task<VectorStoreFileBatch> CreateVectorStoreFileBatchAsync(string vectorStoreId, CreateVectorStoreFileBatchRequest request, CancellationToken cancellationToken = default)
    {
        return await SendRequestAsync<VectorStoreFileBatch>(HttpMethod.Post, $"/{ApiVersion}/vector_stores/{vectorStoreId}/file_batches", request, cancellationToken);
    }

    /// <summary>
    /// Creates a new vector store file batch and waits for it to be fully processed
    /// </summary>
    public async Task<VectorStoreFileBatch> CreateAndPollVectorStoreFileBatchAsync(string vectorStoreId, CreateVectorStoreFileBatchRequest request, int pollIntervalMs = 1000, int maxAttempts = 60, CancellationToken cancellationToken = default)
    {
        var batch = await CreateVectorStoreFileBatchAsync(vectorStoreId, request, cancellationToken);
        return await PollVectorStoreFileBatchUntilCompletedAsync(vectorStoreId, batch.Id, pollIntervalMs, maxAttempts, cancellationToken);
    }

    /// <summary>
    /// Creates a new vector store file batch with file IDs
    /// </summary>
    public async Task<VectorStoreFileBatch> CreateVectorStoreFileBatchAsync(string vectorStoreId, List<string> fileIds, CancellationToken cancellationToken = default)
    {
        var request = CreateVectorStoreFileBatchRequest.Create(fileIds);
        return await CreateVectorStoreFileBatchAsync(vectorStoreId, request, cancellationToken);
    }

    /// <summary>
    /// Retrieves a vector store file batch
    /// </summary>
    public async Task<VectorStoreFileBatch> RetrieveVectorStoreFileBatchAsync(string vectorStoreId, string batchId, CancellationToken cancellationToken = default)
    {
        return await SendRequestAsync<VectorStoreFileBatch>(HttpMethod.Get, $"/{ApiVersion}/vector_stores/{vectorStoreId}/file_batches/{batchId}", null, cancellationToken);
    }

    /// <summary>
    /// Polls a vector store file batch until it is in the completed state
    /// </summary>
    public async Task<VectorStoreFileBatch> PollVectorStoreFileBatchUntilCompletedAsync(string vectorStoreId, string batchId, int pollIntervalMs = 1000, int maxAttempts = 60, CancellationToken cancellationToken = default)
    {
        var attempts = 0;
        while (maxAttempts == 0 || attempts < maxAttempts)
        {
            var batch = await RetrieveVectorStoreFileBatchAsync(vectorStoreId, batchId, cancellationToken);

            if (batch.Status == VectorStoreFileStatus.Completed ||
                (batch.FileCounts.InProgress == 0 && batch.FileCounts.Failed == 0 && batch.FileCounts.Cancelled == 0))
            {
                return batch;
            }
            else if (batch.Status == VectorStoreFileStatus.Failed)
            {
                throw new OpenAIException($"Vector store file batch {batchId} failed",
                    System.Net.HttpStatusCode.BadRequest, "vector_store_file_batch_failed");
            }
            else if (batch.Status == VectorStoreFileStatus.Cancelled)
            {
                throw new OpenAIException($"Vector store file batch {batchId} was cancelled",
                    System.Net.HttpStatusCode.BadRequest, "vector_store_file_batch_cancelled");
            }

            attempts++;
            await Task.Delay(pollIntervalMs, cancellationToken);
        }

        throw new TimeoutException($"Vector store file batch {batchId} did not complete after {maxAttempts} polling attempts");
    }

    /// <summary>
    /// Cancels a vector store file batch
    /// </summary>
    public async Task<VectorStoreFileBatch> CancelVectorStoreFileBatchAsync(string vectorStoreId, string batchId, CancellationToken cancellationToken = default)
    {
        return await SendRequestAsync<VectorStoreFileBatch>(HttpMethod.Post, $"/{ApiVersion}/vector_stores/{vectorStoreId}/file_batches/{batchId}/cancel", null, cancellationToken);
    }

    /// <summary>
    /// Lists vector store files in a batch
    /// </summary>
    public async Task<ListResponse<VectorStoreFile>> ListVectorStoreFilesInBatchAsync(string vectorStoreId, string batchId, int? limit = null, string order = null, string after = null, string before = null, string filter = null, CancellationToken cancellationToken = default)
    {
        var parameters = new Dictionary<string, string>();
        if (limit.HasValue) parameters["limit"] = limit.Value.ToString();
        if (!string.IsNullOrEmpty(order)) parameters["order"] = order;
        if (!string.IsNullOrEmpty(after)) parameters["after"] = after;
        if (!string.IsNullOrEmpty(before)) parameters["before"] = before;
        if (!string.IsNullOrEmpty(filter)) parameters["filter"] = filter;

        var queryString = BuildQueryString(parameters);
        return await SendRequestAsync<ListResponse<VectorStoreFile>>(HttpMethod.Get, $"/{ApiVersion}/vector_stores/{vectorStoreId}/file_batches/{batchId}/files{queryString}", null, cancellationToken);
    }

    #endregion

    #region Convenience Methods

    /// <summary>
    /// Creates a vector store with files in one operation
    /// </summary>
    public async Task<VectorStore> CreateVectorStoreWithFilesAsync(string name, List<string> fileIds, CancellationToken cancellationToken = default)
    {
        var request = CreateVectorStoreRequest.Create(name);
        request = CreateVectorStoreRequestExtensions.WithFileIds(request, fileIds);
        return await CreateAndPollVectorStoreAsync(request, cancellationToken: cancellationToken);
    }

    /// <summary>
    /// Uploads files and creates a vector store with them in one operation using an OpenAIClient
    /// </summary>
    public async Task<VectorStore> UploadFilesAndCreateVectorStoreAsync(
        string name,
        List<string> filePaths,
        OpenAIClient client,
        CancellationToken cancellationToken = default)
    {
        // Upload all files
        var fileIds = new List<string>();
        foreach (var filePath in filePaths)
        {
            var fileResponse = await client.UploadFileAsync(filePath, "assistants", cancellationToken);
            string fileId = JsonSerializer.Deserialize<JsonElement>(fileResponse.ToString()).GetProperty("id").GetString();
            fileIds.Add(fileId);
        }

        // Create the vector store with the files
        return await CreateVectorStoreWithFilesAsync(name, fileIds, cancellationToken);
    }

    #endregion
}