// SPDX-FileCopyrightText: Copyright 2026 Robert Bopko
// SPDX-License-Identifier: BSD-2-Clause

namespace Ssc;

public static class Paging
{
    public static async Task<List<T>> CollectAsync<T>(Func<uint, Task<(IReadOnlyList<T> Items, uint Total)>> fetchPage)
    {
        var collected = new List<T>();
        while (true)
        {
            var (items, total) = await fetchPage((uint)collected.Count);
            collected.AddRange(items);
            if (items.Count == 0 || collected.Count >= total)
            {
                return collected;
            }
        }
    }
}
