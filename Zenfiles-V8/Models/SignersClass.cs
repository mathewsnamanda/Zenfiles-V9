namespace CargenDss.Models
{
    public class SignersClass
    {
        public string? documentid { get; set; }
        public List<signers>? signers { get; set; }
    }
    public class signers
    {
        public Boolean Authenticate { get; set; }
        public string? email { get; set; }
        public string? phone { get; set; }
    }
}
