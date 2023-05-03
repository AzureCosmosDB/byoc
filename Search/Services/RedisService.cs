using StackExchange.Redis;
using DataCopilot.Search.Utilities;
using DataCopilot.Search.Models;
using DataCopilot.Search.Constants;

namespace DataCopilot.Search.Services
{
    public class RedisService
    {
        private readonly ConnectionMultiplexer _connectionMultiplexer;
        private readonly IDatabase _database;
        private readonly ILogger _logger;
        private string? _errorMessage;
        private List<string> _statusMessages = new();

        public RedisService(string connection, ILogger logger)
        {
            ArgumentException.ThrowIfNullOrEmpty(connection);


            _connectionMultiplexer = ConnectionMultiplexer.Connect(connection);
            _database = _connectionMultiplexer.GetDatabase();

            _logger = logger;
        }

        public IDatabase GetDatabase()
        {
            return _database;
        }

        void ClearState()
        {
            _errorMessage = "";
            _statusMessages.Clear();
        }

        public async Task CreateRedisIndexAsync()
        {
            ClearState();

            try
            {
                _logger.LogInformation("Checking if Redis index exists...");
                var db = _connectionMultiplexer.GetDatabase();

                RedisResult? index = null;

                try
                {
                    index = await db.ExecuteAsync("FT.INFO", "embeddingIndex");
                }
                catch (RedisServerException redisX)
                {
                    _logger.LogInformation("Exception while checking embedding index:" + redisX.Message);
                    //not returning - index most likely doesn't exist
                }
                if (index != null)
                {
                    _logger.LogInformation("Redis index for embeddings already exists. Skipping...");
                    return;
                }

                _logger.LogInformation("Creating Redis index...");

                var _ = await db.ExecuteAsync("FT.CREATE",
                    "embeddingIndex", "SCHEMA", "vector", "VECTOR", "HNSW", "6", "TYPE", "FLOAT32", "DISTANCE_METRIC", "COSINE", "DIM", "1536");

                _logger.LogInformation("Created Redis index for embeddings");
            }
            catch (Exception e)
            {
                _errorMessage = e.ToString();
                _logger.LogError(_errorMessage);
            }
        }

        public async Task<List<VectorSearchResult>> VectorSearchAsync(float[] embeddings)
        {

            List<VectorSearchResult> retDocs = new List<VectorSearchResult>();

            QueryModel query = new();

            var resultList = new List<string>(query.ResultsToShow);
            query.ResetContext = false;
            _errorMessage = "";


            var memory = new ReadOnlyMemory<float>(embeddings);

            //Search Redis for similar embeddings
            var res = await _database.ExecuteAsync("FT.SEARCH",
                "embeddingIndex",
                $"*=>[KNN {query.ResultsToShow} @vector $BLOB]",
                "PARAMS",
                "2",
                "BLOB",
                memory.AsBytes(),
                "SORTBY",
                "__vector_score",
                "DIALECT",
                "2");

            if (res.Type == ResultType.MultiBulk)
            {
                var results = (RedisResult[])res;
                var count = (int)results[0];

                if ((2 * count + 1) != results.Length)
                {
                    throw new NotSupportedException($"Unexpected entries is Redis result, '{results.Length}' results for count of '{count}'");
                }


                EmbeddingType embeddingType = EmbeddingType.unknown;

                for (var i = 0; i < count; i++)
                {
                    //fetch the RedisResult
                    RedisResult[] result = (RedisResult[])results[2 * i + 1 + 1];

                    if (result == null)
                        continue;

                    string originalId = "", partitionKey = "";

                    for (int j = 0; j < result.Length; j += 2)
                    {
                        var key = (string)result[j];
                        switch (key)
                        {
                            case "pk":
                                partitionKey = (string)result[j + 1];
                                break;

                            case "originalId":
                                originalId = (string)result[j + 1];
                                break;

                            case "type":
                                embeddingType = (EmbeddingType)ushort.Parse((string)result[j + 1]);
                                break;
                        }
                    }

                    //Enum to string
                    string containerName = EnumerationExtensions.AsText(embeddingType);
                    
                    //Resolve mapping customer and order entities to customer container
                    containerName = (containerName == "product" ? "product" : "customer");

                    retDocs.Add(new VectorSearchResult(documentId: originalId, partitionKey: partitionKey, containerName: containerName));

                }

            }
            else
            {
                throw new NotSupportedException($"Unexpected query result type {res.Type}");
            }

            return retDocs;
        }

    }
}
