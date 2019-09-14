using System.Collections.ObjectModel;

namespace TextBuffer
{
    public class MarkerOptions
    {
        public Range Range { get; set; }
        public ReadOnlyDictionary<string, object> Properties { get; set; }
        public bool? Reversed { get; set; }
        public bool? Tailed { get; set; }
        public bool? Valid { get; set; }
        public string Invalidate { get; set; }
        public bool? Exclusive { get; set; }
        public Marker Marker { get; set; }
    }
}