using ZenFiles.Models;

namespace Zenfiles.Models.linked
{
    public class LinkedObjectmodel
    {
        public string? PropertyName { get; set; }
        public List<Objectsearchresponse> items { get; set; }
    }
}
