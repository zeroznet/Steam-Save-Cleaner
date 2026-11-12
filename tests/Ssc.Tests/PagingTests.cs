namespace Ssc.Tests;

public class PagingTests
{
    [Fact]
    public async Task Collect_PagesUntilTotal()
    {
        var starts = new List<uint>();
        var items = await Paging.CollectAsync(start =>
        {
            starts.Add(start);
            var size = (int)Math.Min(500, 1007 - start);
            return Task.FromResult<(IReadOnlyList<int>, uint)>((Enumerable.Range((int)start, size).ToList(), 1007u));
        });
        Assert.Equal(1007, items.Count);
        Assert.Equal([0u, 500u, 1000u], starts);
    }

    [Fact]
    public async Task Collect_StopsOnEmptyPage()
    {
        var calls = 0;
        var items = await Paging.CollectAsync(start =>
        {
            calls++;
            IReadOnlyList<int> page = start == 0 ? [1, 2, 3, 4] : [];
            return Task.FromResult((page, 10u));
        });
        Assert.Equal(4, items.Count);
        Assert.Equal(2, calls);
    }
}
