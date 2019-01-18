using System;
using System.ComponentModel;

namespace Foundation.Sdk
{
    /// <summary>
    /// Class representing inputs for a find or search operation
    /// </summary>
    public sealed class FindCriteria
    {
        /// <summary>
        /// The index within the find results at which to start filtering
        /// </summary>
        public int Start { get; set; } = 0;

        /// <summary>
        /// The number of items within the find results to limit the result set to
        /// </summary>
        public int Limit { get; set; } = -1;

        /// <summary>
        /// The Json property name of the object on which to sort
        /// </summary>
        public string SortFieldName { get; set; } = string.Empty;

        /// <summary>
        /// The sort direction
        /// </summary>
        public ListSortDirection SortDirection { get; set; } = ListSortDirection.Descending;

        /// <summary>
        /// Gets the numeric (1 or -1) representation of the sort direction property
        /// </summary>
        /// <returns>1 or -1</returns>
        public int GetNumericSortDirection() => SortDirection == ListSortDirection.Descending ? 1 : -1;
    }
}