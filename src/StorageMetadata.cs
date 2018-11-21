using System;
using System.Collections.Generic;

namespace Foundation.Sdk
{
    /// <summary>
    /// Class representing storage metadata
    /// </summary>
    public class StorageMetadata
    {
        public DrawerOwner Owner { get; set; }
        public int Size { get; set; }
        public string Drawer { get; set; }
        public DateTime Modified { get; set; }
        public Guid Etag { get; set; }
        public Guid Id { get; set; }
        public string Class { get; set; }
    }
}