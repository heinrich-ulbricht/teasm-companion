using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json.Serialization;
using System;
using System.IO;

namespace TeasmCompanion
{
    /// <summary>
    /// Handle embedded JSON literals, i.e. string values that are escaped JSON objects.
    /// 
    /// This class is based on https://stackoverflow.com/a/39154630 with additional "null" handling.
    /// </summary>
    /// <typeparam name="T">Type of the embedded JSON object</typeparam>
    public class EmbeddedLiteralConverter<T> : JsonConverter
    {
        public override bool CanConvert(Type objectType)
        {
            return typeof(T).IsAssignableFrom(objectType);
        }

        public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
        {
            if (reader.TokenType == JsonToken.Null)
                return null;
            var contract = serializer.ContractResolver.ResolveContract(objectType);
            if (contract is JsonPrimitiveContract)
                throw new JsonSerializationException("Invalid type: " + objectType);
            if (existingValue == null && contract.DefaultCreator != null)
                existingValue = contract.DefaultCreator();
            if (reader.TokenType == JsonToken.String)
            {
                var json = (string)JToken.Load(reader);

                // what if the value given to us is "null"? - then return the empty default instance e.g. empty list
                if (json.Equals("null", StringComparison.InvariantCultureIgnoreCase))
                    return existingValue;
                using (var subReader = new JsonTextReader(new StringReader(json)))
                {
                    // By populating a pre-allocated instance we avoid an infinite recursion in EmbeddedLiteralConverter<T>.ReadJson()
                    // Re-use the existing serializer to preserve settings.
                    serializer.Populate(subReader, existingValue);
                }
            }
            else
            {
                serializer.Populate(reader, existingValue);
            }
            return existingValue;
        }

        public override bool CanWrite { get { return false; } }

        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
        {
            throw new NotImplementedException();
        }
    }
}
