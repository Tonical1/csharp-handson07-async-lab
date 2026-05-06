public class Processor<TInput, TResult>
{
    private readonly IEnumerable<TInput> items;

    public Processor(IEnumerable<TInput> items)
    {
        this.items = items;
    }

    public async Task<List<TResult>> ProcessListAsync(
        Func<TInput, Task<TResult>> asyncFunc,
        int maxActive = 4)
    {
        var semaphore = new SemaphoreSlim(maxActive);
        var tasks = new List<Task<TResult>>();

        foreach (var item in items)
        {
            await semaphore.WaitAsync();

            tasks.Add(Task.Run(async () =>
            {
                try
                {
                    return await asyncFunc(item);
                }
                finally
                {
                    semaphore.Release();
                }
            }));
        }

        var results = await Task.WhenAll(tasks);
        return results.ToList();
    }
}