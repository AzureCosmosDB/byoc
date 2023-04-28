using Newtonsoft.Json;

namespace DataCopilot.Vectorize.Models
{
    public abstract class DocModel
    {
        [JsonProperty(PropertyName = "$type")]
        public virtual string Doctype => GetType().Name;

        [JsonProperty(PropertyName = "id")]
        public string? id { get; set; }
    }
}