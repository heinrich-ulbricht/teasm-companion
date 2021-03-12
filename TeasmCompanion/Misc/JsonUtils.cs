using Newtonsoft.Json;
using Serilog;
using System.Diagnostics;

#nullable enable

namespace TeasmCompanion.Misc
{
    public class JsonUtils
    {
        /// <summary>
        /// Deserialize JSON object and log the first missing member that is detected.
        /// </summary>
        /// <typeparam name="T">Type of JSON object</typeparam>
        /// <param name="logger">Logger instance to log missing members with</param>
        /// <param name="value">JSON string</param>
        /// <returns></returns>
        public static T DeserializeObject<T>(ILogger? logger, string value)
        {
            try
            {
                JsonSerializerSettings settings = new JsonSerializerSettings
                {
                    MissingMemberHandling = MissingMemberHandling.Error,
                };
#pragma warning disable CS8603 // Possible null reference return.
                return JsonConvert.DeserializeObject<T>(value, settings);
#pragma warning restore CS8603 // Possible null reference return.
            }
            catch (JsonSerializationException e)
            {
                if (logger == null)
                {
                    Debug.WriteLine(e.ToString());
                }
                logger?.Information("## MODEL UPDATE NEEDED ## Fields found we don't know yet in class '{ClassName}'\r\nError message: {ErrorMessage}; value is \r\n{@Value}", typeof(T), e.Message, value);
                return JsonConvert.DeserializeObject<T>(value);
            }
        }
    }
}
