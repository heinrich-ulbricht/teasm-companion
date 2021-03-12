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
    public class CollectionListForPrefix : Attribute
    {
        public CollectionListForPrefix(string jsonPropertyPrefix) : base()
        {
            JsonPropertyPrefix = jsonPropertyPrefix;
        }

        public string JsonPropertyPrefix { get; }
    }

    /// <summary>
    /// This class allows to collect values of properties whose keys change at runtime but share a known prefix.
    /// 
    /// A sample would be the "tab::GUID" property where the GUID is not known beforehand but the "tab::" prefix is always the same. The values of those properties 
    /// can be collected in a list marked with the CollectionListForPrefix("tab::") attribute.
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
                        // find collection list with matching prefix
                        var collectionListProp = objectProps.FirstOrDefault(pi =>
                            pi.CanWrite && (pi.GetCustomAttribute<CollectionListForPrefix>()?.JsonPropertyPrefix.Equals(currentPrefix, StringComparison.InvariantCultureIgnoreCase) ?? false));

                        // we expect something like List<T> for the deserialized JSON property values of type T to store in
                        if (!(collectionListProp.PropertyType.IsGenericType && collectionListProp.PropertyType.GetGenericTypeDefinition() == typeof(List<>)))
                        {
                            // collection property ist not a generic list? this does not work
                            throw new JsonSerializationException($"Collection list needs to be a generic list but is type '{collectionListProp.PropertyType}'");
                        }

                        var collectionListItemType = collectionListProp.PropertyType.GetGenericArguments()[0];
                        // create list if it doesn't exist yet
                        IList collectionList = collectionListProp.GetValue(existingValue) as IList;
                        if (collectionList == null)
                        {
                            Type t = typeof(List<>).MakeGenericType(collectionListItemType);
                            IList res = (IList)Activator.CreateInstance(t);

                            collectionList = res;
                            collectionListProp.SetValue(existingValue, collectionList);
                        }

                        object listElement;
                        if (jp.Value?.Type == JTokenType.String && collectionListItemType != typeof(string))
                        {
                            // if the JSON value is of type string and the generic list does NOT contain strings then we've got a nested JSON that we need to deserialize (like EmbeddedLiteralConverter does)
                            var json = (string)jp.Value;
                            using var subReader = new JsonTextReader(new StringReader(json));
                            listElement = Activator.CreateInstance(collectionListItemType);
                            serializer.Populate(subReader, listElement);
                        }
                        else
                        {
                            // not sure about this part; untested ^^
                            listElement = jp.Value.ToObject(collectionListItemType, serializer);
                        }
                        collectionList.Add(listElement);
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
