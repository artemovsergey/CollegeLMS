using System.Text.Json.Serialization;

namespace CollegeLMS.API.Response;

public class PagedResponse<T>
{
    [JsonInclude]
    public List<T> Items { get; private set; } = [];

    [JsonInclude]
    public int TotalCount { get; private set; }

    [JsonInclude]
    public int Page { get; private set; }

    [JsonInclude]
    public int PageSize { get; private set; }

    [JsonInclude]
    public int TotalPages => (int)Math.Ceiling((double)TotalCount / PageSize);

    public PagedResponse() { }

    public PagedResponse(List<T> items, int totalCount, int page, int pageSize)
    {
        Items = items;
        TotalCount = totalCount;
        Page = page;
        PageSize = pageSize;
    }
}
