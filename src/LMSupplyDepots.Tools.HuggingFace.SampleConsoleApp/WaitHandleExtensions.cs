namespace LMSupplyDepots.Tools.HuggingFace.SampleConsoleApp;

// WaitOneAsync 확장 메서드
public static class WaitHandleExtensions
{
    public static async Task<bool> WaitOneAsync(this WaitHandle handle, CancellationToken cancellationToken)
    {
        var tcs = new TaskCompletionSource<bool>();
        using var registration = cancellationToken.Register(() => tcs.TrySetCanceled());

        var waitHandle = ThreadPool.RegisterWaitForSingleObject(
            handle,
            (state, timedOut) => ((TaskCompletionSource<bool>)state!).TrySetResult(!timedOut),
            tcs,
            -1,
            true);

        try
        {
            return await tcs.Task;
        }
        finally
        {
            waitHandle.Unregister(null);
        }
    }
}