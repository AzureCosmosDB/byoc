using Azure;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.ComponentModel;

namespace DataCopilot.Models
{
    public class Embedding : DocModel
    {   
        public string id { get; set; }
        public EmbeddingType type { get; set; }
        public string originalId { get; set; }
        public float[] embeddings { get; set; }
    }

    public enum EmbeddingType : ushort
    {
        [Description("unknown")]
        unknown = 0,
        [Description("Product")]
        product = 1,
        [Description("Customer")]
        customer = 2,
        [Description("Order")]
        order = 3
    }

    public static class EnumerationExtensions
    {
        public static string AsText<T>(this T value) where T : Enum
        {
            return Enum.GetName(typeof(T), value);
        }
    }
}
