public class QueueCreator
{
    private readonly Queue<Func<Task>> queue = new();
    private readonly SemaphoreSlim signal = new(0);
    private readonly SemaphoreSlim active;
    private bool disposed = false;

    public QueueCreator(int maxActive)
    {
        active = new SemaphoreSlim(maxActive);
        Task.Run(StartLoop);
    }

    public void AddToQueue(Func<Task> newTask)
    {
        lock (queue)
        {
            queue.Enqueue(newTask);
        }

        signal.Release();
    }

    private async Task StartLoop()
    {
        while (!disposed)
        {
            await signal.WaitAsync();
            Func<Task> execTask;

            lock (queue) { execTask = queue.Dequeue(); }
            await active.WaitAsync();
            _ = Task.Run(async () =>
            {
                try
                {
                    await execTask();
                }
                finally
                {
                    active.Release();
                }
            });
        }
    }

    public void Dispose()
    {
        disposed = true;
        signal.Dispose();
        active.Dispose();
    }
}