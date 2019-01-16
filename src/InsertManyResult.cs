using System.Collections.Generic;

namespace Foundation.Sdk
{
    /// <summary>
    /// Class representing a result of a multi-insert
    /// </summary>
    public class InsertManyResult
    {
        /// <summary>
        /// Gets/sets the total number of objects that were inserted
        /// </summary>
        public int Inserted { get; set; }

        /// <summary>
        /// Gets/sets the Ids of the items that were inserted
        /// </summary>
        public List<string> Ids { get; set; } = new List<string>();
    }
}