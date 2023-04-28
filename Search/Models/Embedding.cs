using System.ComponentModel;

namespace DataCopilot.Search.Models
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
        order = 3
    }

    public static class EnumerationExtensions
    {
        public static string AsText<T>(this T value) where T : Enum
        {
            return Enum.GetName(typeof(T), value);
        }
    }

    public class VectorSearchResult
    {
        public string documentId { get; set; }
        public string partitionKey { get; set; }
        public string containerName { get; set; }

        public VectorSearchResult(string documentId, string partitionKey, string containerName)
        {
            this.documentId = documentId;
            this.partitionKey = partitionKey;
            this.containerName = containerName;
        }
    }
}
