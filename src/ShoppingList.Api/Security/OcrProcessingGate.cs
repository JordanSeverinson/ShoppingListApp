namespace ShoppingList.Api.Security;

public sealed class OcrProcessingGate
{
    private readonly SemaphoreSlim _semaphore = new(2, 2);

    public async Task<T> RunAsync<T>(Func<Task<T>> work, CancellationToken cancellationToken)
    {
        await _semaphore.WaitAsync(cancellationToken);
        try
        {
            return await work();
        }
        finally
        {
            _semaphore.Release();
        }
    }
}
