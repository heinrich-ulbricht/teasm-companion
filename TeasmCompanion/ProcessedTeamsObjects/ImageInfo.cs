#nullable enable

namespace TeasmCompanion.ProcessedTeamsObjects
{
    public class ImageInfo
    {
        public string Url { get; set; }
        // set for Teams urls that need authentication; (maybe more? we'll see)
        public string CacheKey { get; set; }
        public ImageType ImageType { get; set; }

        public ImageInfo(string url, string cacheKey, ImageType imageType)
        {
            Url = url;
            CacheKey = cacheKey;
            ImageType = imageType;
        }
    }
}
