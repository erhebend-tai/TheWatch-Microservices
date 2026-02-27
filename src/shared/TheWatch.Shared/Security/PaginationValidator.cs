namespace TheWatch.Shared.Security;

/// <summary>
/// Security+ 2.6: Pagination parameter clamping to prevent resource exhaustion.
/// Use in all services that accept page/pageSize query parameters.
/// </summary>
public static class PaginationValidator
{
    public const int DefaultPage = 1;
    public const int DefaultPageSize = 20;
    public const int MaxPageSize = 100;

    /// <summary>
    /// Clamps page and pageSize to safe ranges. Prevents negative values,
    /// zero pages, and excessively large page sizes that could cause DoS.
    /// </summary>
    public static (int Page, int PageSize) Clamp(int? page, int? pageSize, int maxPageSize = MaxPageSize)
    {
        var safePage = Math.Max(DefaultPage, page ?? DefaultPage);
        var safePageSize = Math.Clamp(pageSize ?? DefaultPageSize, 1, maxPageSize);
        return (safePage, safePageSize);
    }
}
