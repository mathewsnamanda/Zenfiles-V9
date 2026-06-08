namespace ZenFiles.Models
{
    public class ObjClasPropResp
    {
            public string? objectName { get; set; }
            public int objectID { get; set; }
            public int classID { get; set; }
            public List<Property>? properties { get; set; }
    }
}
