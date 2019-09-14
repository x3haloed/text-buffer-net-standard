using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace TextBuffer
{
    public class MarkerSnapshot
    {
        public MarkerSnapshot(
            Range range,
            ReadOnlyDictionary<string, object> properties,
            bool reversed,
            bool tailed,
            bool valid,
            string invalidate,
            bool? exclusive,
            Marker marker)
        {
            Range = range;
            Properties = properties;
            Reversed = reversed;
            Tailed = tailed;
            Valid = valid;
            Invalidate = invalidate;
            Exclusive = exclusive;
            Marker = marker;
        }

        public Range Range { get; }
        public ReadOnlyDictionary<string, object> Properties { get; }
        public bool Reversed { get; }
        public bool Tailed { get; }
        public bool Valid { get; }
        public string Invalidate { get; }
        public bool? Exclusive { get; }
        public Marker Marker { get; }

        public MarkerSnapshot MergeOptions(MarkerOptions options)
        {
            var properties = new Dictionary<string, object>(Properties);

            if (options.Properties != null)
            {
                foreach (var key in options.Properties.Keys)
                {
                    properties[key] = options.Properties[key];
                }
            }

            return new MarkerSnapshot(
            options.Range ?? Range,
            new ReadOnlyDictionary<string, object>(properties),
            options.Reversed ?? Reversed,
            options.Tailed ?? Tailed,
            options.Valid ?? Valid,
            options.Invalidate ?? Invalidate,
            options.Exclusive ?? Exclusive,
            options.Marker ?? Marker);
        }
    }
}