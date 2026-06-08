namespace Zenfiles.Models
{
    public class DSSPostResp
    {
        public string uid { get; set; }
        public string email { get; set; }
        public object phone { get; set; }
        public bool currentsigner { get; set; }
        public bool signed { get; set; }
        public DateTime signedtimestamp { get; set; }
        public object ipaddress { get; set; }
        public string documentid { get; set; }
    }
}
