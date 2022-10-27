using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Anteater.Intercom.Gui.Helpers;

public static class LinqExtensions
{
    public static IEnumerable<T> Where<T>(this T value, Func<T, bool> filter)
    {
        return new[] { value }.Where(filter);
    }

    public static Task Do<T>(this IEnumerable<T> value, Action<T> func)
    {
        foreach (var item in value)
        {
            func(item);
        }

        return Task.CompletedTask;
    }

    public static async Task DoAsync<T>(this IEnumerable<T> value, Func<T, Task> func)
    {
        await Task.WhenAll(value.Select(x => func(x)));
    }
}
