﻿using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using Hl7.Fhir.Model;

namespace Spark.Store.Sql.Model
{
    public class BundleSnapshot
    {
        [Key]
        public int Id { get; set; }
        public string Key { get; set; }

        public int Count { get; set; }

        public Bundle.BundleType BundleType { get; set; }

        public string Uri { get; set; }

        public virtual ICollection<BundleSnapshotResource> Resources { get; set; }
    }
}