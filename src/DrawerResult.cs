using System;
using System.Collections.Generic;
using System.Net;

namespace Foundation.Sdk
{
    /// <summary>
    /// Class representing a result from operating on a drawer on the FDNS Storage microservice
    /// </summary>
    public sealed class DrawerResult
    {
        public string Drawer { get; set; }
        public string Method { get; set; }
        public string Message { get; set; }
        public bool Success { get; set; }
        public string Name { get; set; }
        public DateTime? Created { get; set; }
        public DrawerOwner Owner { get; set; }
    }
}