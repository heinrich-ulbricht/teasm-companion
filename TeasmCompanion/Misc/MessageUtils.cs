#nullable enable

namespace TeasmCompanion.Misc
{
    public class MessageUtils
    {
        /// <summary>
        /// Replace well known emoji URLs with ASCII counterparts.
        /// </summary>
        /// <param name="url">URL from Teams chat</param>
        /// <returns>ASCII representation of emoji, or null if there is none</returns>
        public static string? GetTextReplacementForImageUrl(string url)
        {
            if (string.IsNullOrWhiteSpace(url))
            {
                return null;
            }

            url = url.ToLowerInvariant();
            if (url.Contains("/wink/"))
            {
                return ";)";
            }
            else if (url.Contains("/surprised/"))
            {
                return ":-O";
            }
            else if (url.Contains("/laugh/"))
            {
                return ":D";
            }
            else if (url.Contains("/smile/"))
            {
                return ":)";
            }
            else if (url.Contains("/cool/"))
            {
                return "(⌐■_■)";
            }
            else if (url.Contains("/yes/"))
            {
                return "(^^)ｂ";
            }
            else if (url.Contains("/sad/"))
            {
                return ":(";
            }
            else if (url.Contains("/kiss/"))
            {
                return ":-*";
            }
            else if (url.Contains("/speechless/"))
            {
                return ":-|";
            }
            else if (url.Contains("1f615.png"))
            {
                return ":(";
            }
            else if (url.Contains("/heart/"))
            {
                return "♥";
            }
            else if (url.Contains("/facepalm/"))
            {
                return "(－‸ლ)";
            }
            else if (url.Contains("/tongueout/"))
            {
                return ":P";
            }
            return null;
        }
    }
}
