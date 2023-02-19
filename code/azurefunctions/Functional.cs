using LanguageExt;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace azurefunctions;

internal static class IAsyncEnumerableExtensions
{
    public static async ValueTask<Unit> Iter<T>(this IAsyncEnumerable<T> enumerable, Func<T, ValueTask> action, CancellationToken cancellationToken)
    {
        await Parallel.ForEachAsync(enumerable, cancellationToken, async (t, token) => await action(t));
        return Prelude.unit;
    }
}
