
namespace TmsApi.Application.Dtos;
public record PagedRequest
{
    private const int MaxPagesize = 50;
    private int _pageSize = 20;

    public int Page { get; init;} = 1;
    public int pageSize
    {
        get => _pageSize;
        init => _pageSize = value <1 ? 20 : value > MaxPagesize ? MaxPagesize : value;
    }

    public string? Search { get; init;}
    public string OrderBy { get; init; } = "Title";

    public bool descending { get; init; }

}