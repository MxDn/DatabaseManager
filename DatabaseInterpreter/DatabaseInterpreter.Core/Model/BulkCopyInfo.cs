﻿using System.Data.Common;
using System.Threading;

namespace DatabaseInterpreter.Model
{
    public class BulkCopyInfo
    {
        public string DestinationTableName { get; set; }
        public int? Timeout { get; set; }
        public int? BatchSize { get; set; }
        public DbTransaction Transaction { get; set; }
        public CancellationToken CancellationToken { get; set; }
    }
}
