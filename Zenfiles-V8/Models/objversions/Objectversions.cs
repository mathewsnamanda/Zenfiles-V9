using ZenFiles.Models;

namespace Zenfiles.Models.objversions
{
    public class Objectversions
    {
        public List<ObjectFileResp>? ObjectFiles { get; set; }
        public List<updateprop1>? objectprops { get; set; }
        public int versionid { get; set; }
        public string? title { get; set; }
        public bool IsSingleFile { get; set; }
        public string? DisplayID { get; set; }
        public string? Extension { get; set; }
        public string? LastModifiedBy { get; set; }
        public string? LastModifiedUtc { get; set; }
        public int Class { get; set; }
        public string? ClassName { get; set; }

    }
}
