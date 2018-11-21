using System;
using System.Collections.Generic;
using System.Net;

namespace Foundation.Sdk
{
    /// <summary>
    /// Class representing a result from deleting an item from the FDNS Object microservice
    /// </summary>
    public sealed class DeleteResult
    {
        /// <summary>
        /// The number of items deleted
        /// </summary>
        public int Deleted { get; set; }

        /// <summary>
        /// Whether the deletion was a success
        /// </summary>
        public bool Success { get; set; }
    }
}