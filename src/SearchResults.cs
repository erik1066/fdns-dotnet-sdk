using System.Collections.Generic;

namespace Foundation.Sdk
{
    /// <summary>
    /// Class representing a result set of T
    /// </summary>
    public class SearchResults<T>
    {
        /// <summary>
        /// Gets/sets the total number of objects in the collection
        /// </summary>
        public int Total { get; set; }

        /// <summary>
        /// Gets/sets the total number of objects returned in the result set
        /// </summary>
        public int Size { get; set; }

        /// <summary>
        /// Gets/sets the starting value from which the result set was taken
        /// </summary>
        /// <remarks>
        /// For example, if the search results are used for pagination in a UI
        /// and this result set was for page 2 (and where each peage shows 10
        /// items), then this value would be 11.
        /// </remarks>
        public int From { get; set; }

        /// <summary>
        /// Gets/sets the instances of T that were returned in the result set
        /// </summary>
        public List<T> Items { get; set; } = new List<T>();
    }
}