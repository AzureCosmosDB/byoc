using System.ComponentModel;

namespace DataCopilot.Search.Constants
{
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
