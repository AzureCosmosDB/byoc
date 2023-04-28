using System.ComponentModel;

namespace Vectorize.Models
{
    public class Embedding : DocModel
    {
        public string id { get; set; }
        public EmbeddingType type { get; set; }
        public string originalId { get; set; }
        public string partitionKey { get; set; }
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
        order = 3,
        [Description("Embedding")]
        embedding = 4
    }

    public static class EnumerationExtensions
    {
        public static string AsText<T>(this T value) where T : Enum
        {
            return Enum.GetName(typeof(T), value);
        }
    }
}
