namespace CargenDss.Models
{
    public class Root
    {
        public string docGuid { get; set; }
        public string title { get; set; }
        public string docDate { get; set; }
        public object signedCompletedDate { get; set; }
        public bool signedComplete { get; set; }
        public bool declined { get; set; }
        public bool selfSign { get; set; }
        public object declinedBy { get; set; }
        public object owner { get; set; }
        public int userid { get; set; }
        public object vaultGuid { get; set; }
        public bool downloaded { get; set; }
    }
}
