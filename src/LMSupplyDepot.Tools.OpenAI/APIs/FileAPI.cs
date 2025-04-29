using OpenAI;
using OpenAI.Files;

namespace LMSupplyDepot.Tools.OpenAI.APIs;

/// <summary>
/// Manages file operations with OpenAI
/// </summary>
public class FileAPI
{
    private readonly OpenAIFileClient _fileClient;

    /// <summary>
    /// Initializes a new instance of the FileManager class
    /// </summary>
    /// <param name="client">The OpenAI client</param>
    public FileAPI(OpenAIClient client)
    {
        _fileClient = client.GetOpenAIFileClient();
    }

    /// <summary>
    /// Uploads a file to OpenAI
    /// </summary>
    /// <param name="filePath">Path to the local file</param>
    /// <returns>The uploaded file information</returns>
    public async Task<OpenAIFile> UploadFileAsync(string filePath)
    {
        Console.WriteLine($"Uploading file: {Path.GetFileName(filePath)}");

        try
        {
            var fileResult = await _fileClient.UploadFileAsync(
                filePath,
                FileUploadPurpose.Assistants);

            // Access the Value property to get the actual OpenAIFile
            OpenAIFile file = fileResult.Value;

            Console.WriteLine($"File uploaded successfully. ID: {file.Id}");
            return file;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error uploading file: {ex.Message}");
            throw;
        }
    }

    /// <summary>
    /// Uploads multiple files to OpenAI
    /// </summary>
    /// <param name="filePaths">Paths to the local files</param>
    /// <returns>Collection of uploaded file information</returns>
    public async Task<List<OpenAIFile>> UploadFilesAsync(IEnumerable<string> filePaths)
    {
        var uploadedFiles = new List<OpenAIFile>();

        foreach (var filePath in filePaths)
        {
            if (!File.Exists(filePath))
            {
                Console.WriteLine($"File not found: {filePath}");
                continue;
            }

            var file = await UploadFileAsync(filePath);
            uploadedFiles.Add(file);
        }

        return uploadedFiles;
    }

    /// <summary>
    /// Lists all uploaded files
    /// </summary>
    /// <returns>Collection of file information</returns>
    public async Task<List<OpenAIFile>> ListFilesAsync()
    {
        try
        {
            // Use the GetFiles method which is public
            var filesResult = await _fileClient.GetFilesAsync();
            var files = filesResult.Value;
            return files.ToList();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error listing files: {ex.Message}");
            throw;
        }
    }

    /// <summary>
    /// Deletes a file from OpenAI
    /// </summary>
    /// <param name="fileId">ID of the file to delete</param>
    /// <returns>True if deletion was successful</returns>
    public async Task<bool> DeleteFileAsync(string fileId)
    {
        try
        {
            await _fileClient.DeleteFileAsync(fileId);
            Console.WriteLine($"File {fileId} deleted successfully");
            return true;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error deleting file {fileId}: {ex.Message}");
            return false;
        }
    }
}