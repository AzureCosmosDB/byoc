﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Embeddings.Models
{
    public class SalesOrder
    {
        public string id { get; set; }
        public string type { get; set; }
        public string customerId { get; set; }
        public string orderDate { get; set; }
        public string shipDate { get; set; }
        public List<SalesOrderDetails> details { get; set; }
    }

    public class SalesOrderDetails
    {
        public string sku { get; set; }
        public string name { get; set; }
        public double price { get; set; }
        public int quantity { get; set; }
    }
}
