namespace Koralytics.Application.Common
{
    /// <summary>
    /// Generic wrapper returned by paginated service methods.
    /// </summary>
    public class PagedResult<T>
    {
        /// <summary>The items on the current page.</summary>
        public IReadOnlyList<T> Items { get; init; } = [];

        /// <summary>Current 1-based page number.</summary>
        public int Page { get; init; }

        /// <summary>Maximum number of items per page.</summary>
        public int PageSize { get; init; }

        /// <summary>Total number of items across all pages.</summary>
        public int TotalCount { get; init; }

        /// <summary>Total number of pages.</summary>
        public int TotalPages => PageSize > 0 ? (int)Math.Ceiling((double)TotalCount / PageSize) : 0;

        /// <summary>Whether a previous page exists.</summary>
        public bool HasPreviousPage => Page > 1;

        /// <summary>Whether a next page exists.</summary>
        public bool HasNextPage => Page < TotalPages;
    }
}
