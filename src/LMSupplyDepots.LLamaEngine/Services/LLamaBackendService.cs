using LLama.Common;
using Microsoft.Extensions.Logging;
using System.Collections.Concurrent;
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
    private readonly ConcurrentDictionary<string, ModelParams> _modelParams = new();
    private int? _gpuLayers;
    private bool _initialized;
    private readonly object _initLock = new();

    public bool IsCudaAvailable { get; private set; }
    public bool IsVulkanAvailable { get; private set; }

    public LLamaBackendService(ILogger<LLamaBackendService> logger)
    {
        _logger = logger;
    }

    private void EnsureInitialized()
    {
        if (_initialized) return;

        lock (_initLock)
        {
            if (_initialized) return;
            DetectAvailableBackends();
            _initialized = true;
        }
    }

    private void DetectAvailableBackends()
    {
        try
        {
            IsCudaAvailable = DetectCuda();
            IsVulkanAvailable = DetectVulkan();

            var backendInfo = new List<string>();
            if (IsCudaAvailable) backendInfo.Add("CUDA");
            if (IsVulkanAvailable) backendInfo.Add("Vulkan");

            _logger.LogInformation("Detected hardware acceleration: {Backends}",
                backendInfo.Count > 0 ? string.Join(", ", backendInfo) : "None");

            // Try to initialize backends in order of preference
            if (IsCudaAvailable && TryInitializeCuda())
            {
                _logger.LogInformation("Successfully initialized CUDA backend");
            }
            else if (IsVulkanAvailable && TryInitializeVulkan())
            {
                _logger.LogInformation("Successfully initialized Vulkan backend");
            }
            else
            {
                _logger.LogWarning("No hardware acceleration available, falling back to CPU");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error detecting available backends");
            IsCudaAvailable = false;
            IsVulkanAvailable = false;
        }
    }

    public ModelParams GetOptimalModelParams(string modelPath)
    {
        EnsureInitialized();

        return _modelParams.GetOrAdd(modelPath, path =>
        {
            var gpuLayers = _gpuLayers ?? DetectGpuLayers();
            _gpuLayers = gpuLayers;

            var threads = Environment.ProcessorCount;
            var modelParams = new ModelParams(path)
            {
                ContextSize = 2048,
                BatchSize = 512,
                Threads = threads,
                GpuLayerCount = gpuLayers,
                MainGpu = 0
            };

            // Tune context size based on available memory
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                try
                {
                    var memInfo = GC.GetGCMemoryInfo();
                    var totalMemoryGB = memInfo.TotalAvailableMemoryBytes / (1024.0 * 1024.0 * 1024.0);

                    if (totalMemoryGB > 16)
                    {
                        modelParams.ContextSize = 4096;
                        modelParams.BatchSize = 1024;
                    }
                    else if (totalMemoryGB > 8)
                    {
                        modelParams.ContextSize = 2048;
                        modelParams.BatchSize = 512;
                    }
                    else
                    {
                        modelParams.ContextSize = 1024;
                        modelParams.BatchSize = 256;
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Error detecting memory size, using default parameters");
                }
            }

            return modelParams;
        });
    }

    private int DetectGpuLayers()
    {
        if (IsCudaAvailable)
        {
            try
            {
                if (TryInitializeCuda())
                {
                    var memInfo = GetGpuMemoryInfo();
                    return CalculateOptimalGpuLayers(memInfo);
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
                if (TryInitializeVulkan())
                {
                    return 20; // Conservative default for Vulkan
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to initialize Vulkan backend");
            }
        }

        return 0;
    }

    private bool TryInitializeCuda()
    {
        try
        {
            return TryLoadBackend("LLamaSharp.Backend.Cuda12") ||
                   TryLoadBackend("LLamaSharp.Backend.Cuda11");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error initializing CUDA backend");
            return false;
        }
    }

    private bool TryInitializeVulkan()
    {
        try
        {
            return TryLoadBackend("LLamaSharp.Backend.Vulkan");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error initializing Vulkan backend");
            return false;
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

    private long GetGpuMemoryInfo()
    {
        // 실제 구현에서는 CUDA/Vulkan API를 통해 GPU 메모리 정보를 가져와야 함
        // 여기서는 기본값 반환
        return 4L * 1024 * 1024 * 1024; // 4GB
    }

    private int CalculateOptimalGpuLayers(long gpuMemoryBytes)
    {
        // 모델 크기와 GPU 메모리에 따라 최적의 레이어 수 계산
        var gpuMemoryGB = gpuMemoryBytes / (1024.0 * 1024.0 * 1024.0);

        if (gpuMemoryGB >= 8)
            return 32;
        else if (gpuMemoryGB >= 6)
            return 24;
        else if (gpuMemoryGB >= 4)
            return 20;
        else if (gpuMemoryGB >= 2)
            return 16;
        else
            return 8;
    }
}