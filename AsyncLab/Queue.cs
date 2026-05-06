using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

public class AsyncDictionaryProcessor<TKey, TValue, TResult>
{
    private readonly Dictionary<TKey, TValue> _dict;

    public AsyncDictionaryProcessor(Dictionary<TKey, TValue> dict)
    {
        _dict = dict;
    }

    public async Task<Dictionary<TKey, TResult>> ProcessAsync(
        Func<TKey, TValue, Task<TResult>> asyncFunc,
        int maxDegreeOfParallelism = 4)
    {
        var semaphore = new SemaphoreSlim(maxDegreeOfParallelism);
        var tasks = new List<Task>();
        var results = new Dictionary<TKey, TResult>();
        var lockObj = new object();

        foreach (var kvp in _dict)
        {
            await semaphore.WaitAsync();

            tasks.Add(Task.Run(async () =>
            {
                try
                {
                    var result = await asyncFunc(kvp.Key, kvp.Value);
                    lock (lockObj)
                    {
                        results[kvp.Key] = result;
                    }
                }
                finally
                {
                    semaphore.Release();
                }
            }));
        }

        await Task.WhenAll(tasks);
        return results;
    }
}