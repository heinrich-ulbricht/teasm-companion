namespace TeasmCompanion.TeamsInternal.TeamsInternalApi.v2.users.me.endpoints
{
    public class Imagedata
    {
        public Origimage origImage { get; set; }
        public Croppedimage croppedImage { get; set; }
    }

    public class Origimage
    {
        public string src { get; set; }
        public int imageWidth { get; set; }
        public int imageHeight { get; set; }
        public int adjWidth { get; set; }
        public float adjHeight { get; set; }
        public int newLeft { get; set; }
        public float newTop { get; set; }
        public string type { get; set; }
        public Loaddata loadData { get; set; }
        public string id { get; set; }
    }

    public class Loaddata
    {
        public string imageId { get; set; }
        public string blobUrl { get; set; }
        public bool uploaded { get; set; }
        public string type { get; set; }
    }

    public class Croppedimage
    {
        public string src { get; set; }
        public Loaddata loadData { get; set; }
        public string id { get; set; }
    }
}
