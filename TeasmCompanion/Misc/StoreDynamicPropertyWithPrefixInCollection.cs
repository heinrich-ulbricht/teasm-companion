using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;

namespace TeasmCompanion.Misc
{
    public class CollectionDictForPrefix : Attribute
    {
        public CollectionDictForPrefix(string jsonPropertyPrefix) : base()
        {
            JsonPropertyPrefix = jsonPropertyPrefix;
        }

        public string JsonPropertyPrefix { get; }
    }

    /// <summary>
    /// This class allows to collect values of properties whose keys change at runtime but share a known prefix.
    /// 
    /// A sample would be the "tab::GUID" property where the GUID is not known beforehand but the "tab::" prefix is always the same. The values of those properties 
    /// can be collected in a dictionary marked with the CollectionDictForPrefix("tab::") attribute.
    /// </summary>
    public class StoreDynamicPropertyWithPrefixInCollection : JsonConverter
    {
        private readonly string jsonPropertyPrefixesSeparatedByComma;

        public StoreDynamicPropertyWithPrefixInCollection(string jsonPropertyPrefixesSeparatedByComma) : base()
        {
            this.jsonPropertyPrefixesSeparatedByComma = jsonPropertyPrefixesSeparatedByComma;
        }

        public override bool CanWrite => false;

        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
        {
            throw new NotImplementedException();
        }

        public override bool CanConvert(Type objectType)
        {
            return objectType.GetTypeInfo().IsClass;
        }

        public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
        {
            if (reader.TokenType == JsonToken.Null)
                return null;

            var prefixes = jsonPropertyPrefixesSeparatedByComma.Split(',', StringSplitOptions.RemoveEmptyEntries);
            List<JProperty> prefixedPropertiesToHandle = new List<JProperty>();
            JObject jo = JObject.Load(reader);
            bool haveToRemovePrefixedProperties;
            // remove all properties that would trigger a member deserialization error and which will be handled by us
            do
            {
                haveToRemovePrefixedProperties = false;
                foreach (JProperty jp in jo.Properties())
                {
                    if (jp.Name.StartsWithAny(prefixes, StringComparison.InvariantCultureIgnoreCase) != null)
                    {
                        prefixedPropertiesToHandle.Add(jp);
                        jo.Remove(jp.Name);
                        haveToRemovePrefixedProperties = true;
                        break;
                    }
                }
            } while (haveToRemovePrefixedProperties);

            // first apply default handling
            // note: this also makes sure that other attributes like the EmbeddedLiteralConverter are properly executed
            existingValue = existingValue ?? serializer.ContractResolver.ResolveContract(objectType).DefaultCreator();
            serializer.Populate(jo.CreateReader(), existingValue);

            // now check the properties for anything that can't be mapped
            var objectProps = objectType.GetTypeInfo().DeclaredProperties.ToList();
            // iterate all properties of the decorated class and search for properties with the prefix for special handling
            foreach (JProperty jp in prefixedPropertiesToHandle)
            {
                var jsonPropName = jp.Name;
                PropertyInfo objectProp = objectProps.FirstOrDefault(pi =>
                    pi.CanWrite && (pi.GetCustomAttribute<JsonPropertyAttribute>()?.PropertyName ?? pi.Name) == jsonPropName);

                if (objectProp == null)
                {
                    // when we are here then we've found a JSON property that doesn't match any object property 

                    // now check if the property is the one with the given prefix
                    var currentPrefix = jp.Name.StartsWithAny(prefixes, StringComparison.InvariantCultureIgnoreCase);
                    if (currentPrefix != null)
                    {
                        // find collection dict with matching prefix
                        var collectionDictProp = objectProps.FirstOrDefault(pi =>
                            pi.CanWrite && (pi.GetCustomAttribute<CollectionDictForPrefix>()?.JsonPropertyPrefix.Equals(currentPrefix, StringComparison.InvariantCultureIgnoreCase) ?? false));

                        // we expect something like Dictionary<string,T> for the deserialized JSON property values of type T to store in
                        if (!(collectionDictProp.PropertyType.IsGenericType && collectionDictProp.PropertyType.GetGenericTypeDefinition() == typeof(Dictionary<,>)))
                        {
                            // collection property ist not a generic dict? this does not work
                            throw new JsonSerializationException($"Collection dictionary needs to be a generic dictionary but is type '{collectionDictProp.PropertyType}'");
                        }

                        var collectionDictKeyType = collectionDictProp.PropertyType.GetGenericArguments()[0];
                        if (collectionDictKeyType != typeof(string)) 
                        {
                            throw new JsonSerializationException($"Collection dictionary key has to be of type string but is '{collectionDictKeyType}'");
                        }


                        var collectionDictValueType = collectionDictProp.PropertyType.GetGenericArguments()[1];
                        // create dict if it doesn't exist yet
                        IDictionary collectionDict = collectionDictProp.GetValue(existingValue) as IDictionary;
                        if (collectionDict == null)
                        {
                            Type t = typeof(Dictionary<,>).MakeGenericType(collectionDictKeyType, collectionDictValueType);
                            IDictionary res = (IDictionary)Activator.CreateInstance(t);

                            collectionDict = res;
                            collectionDictProp.SetValue(existingValue, collectionDict);
                        }

                        object dictValue;
                        if (jp.Value?.Type == JTokenType.String && collectionDictValueType != typeof(string))
                        {
                            // if the JSON value is of type string and the generic dict does NOT contain strings then we've got a nested JSON that we need to deserialize (like EmbeddedLiteralConverter does)
                            var json = (string)jp.Value;
                            using var subReader = new JsonTextReader(new StringReader(json));
                            dictValue = Activator.CreateInstance(collectionDictValueType);
                            serializer.Populate(subReader, dictValue);
                        }
                        else
                        {
                            // not sure about this part; untested ^^
                            dictValue = jp.Value.ToObject(collectionDictValueType, serializer);
                        }
                        collectionDict.Add(jsonPropName, dictValue);
                    }
                    else
                    {
                        // we've got a truly unknown property

                        if (serializer.MissingMemberHandling == MissingMemberHandling.Error)
                        {
                            // Could not find member 'like' on object of type 'Emotions'. Path 'eventMessages[0].resource.annotationsSummary.emotions.like'
                            throw new JsonSerializationException($"Could not find member '{jsonPropName}' on object of type '{objectType}'");
                        }
                    }
                }
                else
                {
                    throw new JsonSerializationException($"Found actual property that is not expected to exist for dynamic JSON property name '{jsonPropName}': '{objectProp.Name}'");
                }
            }

            return existingValue;
        }
    }
}
