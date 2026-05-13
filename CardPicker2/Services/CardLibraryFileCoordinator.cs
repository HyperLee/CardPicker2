namespace CardPicker2.Services;

/// <summary>
/// Serializes same-process card-library read-modify-write critical sections.
/// </summary>
/// <example>
/// <code>
/// var result = await coordinator.RunExclusiveAsync(
///     async cancellationToken => await SaveDocumentAsync(cancellationToken),
///     cancellationToken);
/// </code>
/// </example>
public sealed class CardLibraryFileCoordinator
{
    private readonly SemaphoreSlim _semaphore = new(1, 1);

    /// <summary>
    /// Runs an asynchronous operation while holding the card-library file gate.
    /// </summary>
    /// <typeparam name="T">The operation result type.</typeparam>
    /// <param name="operation">The operation to run exclusively.</param>
    /// <param name="cancellationToken">A token that cancels the wait or operation.</param>
    /// <returns>The operation result.</returns>
    public async Task<T> RunExclusiveAsync<T>(
        Func<CancellationToken, Task<T>> operation,
        CancellationToken cancellationToken = default)
    {
        await _semaphore.WaitAsync(cancellationToken);
        try
        {
            return await operation(cancellationToken);
        }
        finally
        {
            _semaphore.Release();
        }
    }

    /// <summary>
    /// Runs an asynchronous operation while holding the card-library file gate.
    /// </summary>
    /// <param name="operation">The operation to run exclusively.</param>
    /// <param name="cancellationToken">A token that cancels the wait or operation.</param>
    /// <returns>A task that completes after the operation releases the gate.</returns>
    public async Task RunExclusiveAsync(
        Func<CancellationToken, Task> operation,
        CancellationToken cancellationToken = default)
    {
        await RunExclusiveAsync(
            async innerCancellationToken =>
            {
                await operation(innerCancellationToken);
                return true;
            },
            cancellationToken);
    }
}
