using System;
using System.Threading.Tasks;

namespace Anteater.Intercom.Gui.Helpers
{
    public static class TaskExtensions
    {
        public static async Task ContinueWith(this Task task, Func<Task> func)
        {
            if (task.IsCompleted)
            {
                await func();
            } else
            {
                await task.ContinueWith(_ => func());
            }
        }
    }
}
