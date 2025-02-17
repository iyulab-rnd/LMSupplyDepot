using LLama.Common;
using Microsoft.Extensions.Logging;
using System.Runtime.InteropServices;

namespace LMSupplyDepots.LLamaEngine.Services;

public interface ILLamaBackendService
{
    bool IsCudaAvailable { get; }
    bool IsVulkanAvailable { get; }
    ModelParams GetOptimalModelParams(string modelPath);
}

public class LLamaBackendService : ILLamaBackendService
{
    private readonly ILogger<LLamaBackendService> _logger;
    private ModelParams? _cachedParams;

    public bool IsCudaAvailable { get; private set; }
    public bool IsVulkanAvailable { get; private set; }

    public LLamaBackendService(ILogger<LLamaBackendService> logger)
    {
        _logger = logger;
        DetectAvailableBackends();
    }

    private void DetectAvailableBackends()
    {
        try
        {
            IsCudaAvailable = DetectCuda();
            IsVulkanAvailable = DetectVulkan();
            _logger.LogInformation("Backend detection complete - CUDA: {IsCudaAvailable}, Vulkan: {IsVulkanAvailable}",
                IsCudaAvailable, IsVulkanAvailable);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error detecting available backends");
            IsCudaAvailable = false;
            IsVulkanAvailable = false;
        }
    }

    private bool DetectCuda()
    {
        try
        {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                return File.Exists(Path.Combine(Environment.SystemDirectory, "nvcuda.dll"));
            }
            else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
            {
                return File.Exists("/usr/lib/x86_64-linux-gnu/libcuda.so") ||
                       File.Exists("/usr/lib/libcuda.so");
            }
            return false;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error detecting CUDA");
            return false;
        }
    }

    private bool DetectVulkan()
    {
        try
        {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                return File.Exists(Path.Combine(Environment.SystemDirectory, "vulkan-1.dll"));
            }
            else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
            {
                return File.Exists("/usr/lib/x86_64-linux-gnu/libvulkan.so") ||
                       File.Exists("/usr/lib/libvulkan.so");
            }
            return false;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error detecting Vulkan");
            return false;
        }
    }

    public ModelParams GetOptimalModelParams(string modelPath)
    {
        if (_cachedParams != null)
        {
            _cachedParams.ModelPath = modelPath;
            return _cachedParams;
        }

        var threads = Environment.ProcessorCount;
        var gpuLayers = 0;

        if (IsCudaAvailable)
        {
            try
            {
                if (TryLoadBackend("LLamaSharp.Backend.Cuda12") || TryLoadBackend("LLamaSharp.Backend.Cuda11"))
                {
                    gpuLayers = 20; // Conservative default
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to initialize CUDA backend");
            }
        }
        else if (IsVulkanAvailable)
        {
            try
            {
                if (TryLoadBackend("LLamaSharp.Backend.Vulkan"))
                {
                    gpuLayers = 20; // Conservative default
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to initialize Vulkan backend");
            }
        }

        _cachedParams = new ModelParams(modelPath)
        {
            ContextSize = 2048,
            BatchSize = 512,
            Threads = threads,
            GpuLayerCount = gpuLayers,
            MainGpu = 0
        };

        return _cachedParams;
    }

    private bool TryLoadBackend(string assemblyName)
    {
        try
        {
            System.Runtime.Loader.AssemblyLoadContext.Default.LoadFromAssemblyName(
                new System.Reflection.AssemblyName(assemblyName));
            return true;
        }
        catch
        {
            return false;
        }
    }
}
