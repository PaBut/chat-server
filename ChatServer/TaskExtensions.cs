namespace ChatServer;

public class TaskExtensions
{
    public static async Task WaitForAllWithCancellationSupport(IEnumerable<Task> tasks)
    {
        bool leftToDispose = true;
        while (leftToDispose)
        {
            leftToDispose = false;
            try
            {
                await Task.WhenAll(tasks.Where(t => !t.IsCompleted));
            }
            catch (OperationCanceledException)
            {
                leftToDispose = true;
            }   
        }
    }
}