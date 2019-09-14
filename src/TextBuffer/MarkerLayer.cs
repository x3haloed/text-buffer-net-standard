using System;
using System.Collections.Generic;
using System.Text;

namespace TextBuffer
{
    /// <summary>
    /// *Experimental:* A container for a related set of markers.
    /// </summary>
    /// <remarks>This API is experimental and subject to change on any release.</remarks>
    public class MarkerLayer
    {
        #region Construction

        public MarkerLayer(
            object delegateObject,
            object id,
            bool maintainHistory = false,
            bool destroyInvalidatedMarkers = false,
            string role = null,
            bool persistent = false)
        {
            Id = id;
            Layer = layer;
            Tailed = tailed;
            Reversed = reversed;
            Valid = valid;
            Invalidate = invalidate;
            Exclusive = exclusive;
            Properties = properties == null
                ? new ReadOnlyDictionary<string, object>(new Dictionary<string, object>())
                : new ReadOnlyDictionary<string, object>(properties);
            HasChangeObservers = false;
            if (!exclusivitySet)
            {
                Layer.SetMarkerIsExclusive(id, IsExclusive());
            }
            PreviousEventState = GetSnapshot(GetRange());
        }

        #endregion Construction

        #region Static Members

        public const int SerializationVersion = 2;

        #endregion Static Members
    }
}
