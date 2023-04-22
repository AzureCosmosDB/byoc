using Azure;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DataCopilot.Models
{
    public class Embedding : DocModel
    {   
        public string id { get; set; }
        public EmbeddingType type { get; set; }
        public float[] embeddings { get; set; }
    }

    public enum EmbeddingType : ushort
    {
        Unknown = 0,
        Product = 1,
        Customer = 2,
        Order = 3
    }
}
