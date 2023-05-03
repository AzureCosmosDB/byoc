namespace DataCopilot.Search.Models
{
    public record VectorSearchResult
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
