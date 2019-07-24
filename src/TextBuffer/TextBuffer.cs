using System;
using System.Text.RegularExpressions;

namespace TextBuffer
{
    /// <summary>
    /// A mutable text container with undo/redo support and the ability to
    /// annotate logical regions in the text.
    ///
    /// ## Observing Changes
    ///
    /// You can observe changes in a {TextBuffer} using methods like {::onDidChange},
    /// {::onDidStopChanging}, and {::getChangesSinceCheckpoint}. These methods report
    /// aggregated buffer updates as arrays of change objects containing the following
    /// fields: `oldRange`, `newRange`, `oldText`, and `newText`. The `oldText`,
    /// `newText`, and `newRange` fields are self-explanatory, but the interepretation
    /// of `oldRange` is more nuanced:
    ///
    /// The reported `oldRange` is the range of the replaced text in the original
    /// contents of the buffer *irrespective of the spatial impact of any other
    /// reported change*. So, for example, if you wanted to apply all the changes made
    /// in a transaction to a clone of the observed buffer, the easiest approach would
    /// be to apply the changes in reverse:
    ///
    /// ```js
    /// buffer1.onDidChange(({changes}) => {
    ///   for (const {oldRange, newText} of changes.reverse()) {
    ///     buffer2.setTextInRange(oldRange, newText)
    ///   }
    /// })
    /// ```
    ///
    /// If you needed to apply the changes in the forwards order, you would need to
    /// incorporate the impact of preceding changes into the range passed to
    /// {::setTextInRange}, as follows:
    ///
    /// ```js
    /// buffer1.onDidChange(({changes}) => {
    ///   for (const {oldRange, newRange, newText} of changes) {
    ///     const rangeToReplace = Range(
    ///       newRange.start,
    ///       newRange.start.traverse(oldRange.getExtent())
    ///     )
    ///     buffer2.setTextInRange(rangeToReplace, newText)
    ///   }
    /// })
    /// ```
    /// </summary>
    public class TextBuffer
    {
        #region Construction
        /// <summary>
        /// Public: Create a new buffer with the given params.
        ///
        /// * `params` {Object} or {String} of text
        ///   * `text` The initial {String} text of the buffer.
        ///   * `shouldDestroyOnFileDelete` A {Function} that returns a {Boolean}
        ///     indicating whether the buffer should be destroyed if its file is
        ///     deleted.
        /// </summary>
        /// <param name="text">The initial <see cref="string"/> text of the buffer.</param>
        /// <param name="shouldDestroyOnFileDelete">
        ///     A <see cref="Action<bool>"/> that returns a <see cref="bool"/>
        ///     indicating whether the buffer should be destroyed if its file is
        ///     deleted.
        /// </param>
        public TextBuffer(
            string text,
            object maxUndoEntries = null,
            object encoding = null,
            object preferredLineEnding = null,
            Action<bool> shouldDestroyOnFileDelete = null,
            string filePath = null,
            bool? load = null)
        {
            refcount = 0;
            conflict = false;
            file = null;
            fileSubscriptions = null;
            stoppedChangingTimeout = null;
            emitter = new Emitter();
            changesSinceLastStoppedChangingEvent = new object[0];
            changesSinceLastDidChangeTextEvent = new object[0];
            id = crypto.randomBytes(16).toString('hex');
            buffer = new NativeTextBuffer(text);
            debouncedEmitDidStopChangingEvent = debounce(this.emitDidStopChangingEvent.bind(this), this.stoppedChangingDelay);
            this.maxUndoEntries = maxUndoEntries != null ? maxUndoEntries : defaultMaxUndoEntries;
            this.setHistoryProvider(new DefaultHistoryProvider(this));
            languageMode = new NullLanguageMode();
            nextMarkerLayerId = 0;
            nextDisplayLayerId = 0;
            defaultMarkerLayer = new MarkerLayer(this, String(nextMarkerLayerId++));
            displayLayers = new { };
            markerLayers = new { };
            markerLayers[defaultMarkerLayer.id] = defaultMarkerLayer;
            markerLayersWithPendingUpdateEvents = new Set();
            selectionsMarkerLayerIds = new Set();
            nextMarkerId = 1;
            outstandingSaveCount = 0;
            loadCount = 0;
            cachedHasAstral = null;
            _emittedWillChangeEvent = false;

            this.setEncoding(p.encoding);
            this.setPreferredLineEnding(p.preferredLineEnding);

            loaded = false;
            destroyed = false;
            transactCallDepth = 0;
            digestWhenLastPersisted = false;

            shouldDestroyOnFileDelete = p.shouldDestroyOnFileDelete || (() => false);

            if (p.filePath)
            {
                this.setPath(p.filePath);
                if (p.load)
                {
                    Grim.deprecate(
                        'The `load` option to the TextBuffer constructor is deprecated. ' +
                        'Get a loaded buffer using TextBuffer.load(filePath) instead.');
                    this.load(new ('internal': true));
                }
            }

            version = 5;
            newlineRegex = newlineRegex;
            spliceArray = spliceArray;

            Patch = superstring.Patch;

            stoppedChangingDelay = 300;
            fileChangeDelay = 200;
            backwardsScanChunkSize = 8000;
            defaultMaxUndoEntries = 10000;
        }
        #endregion Construction

        private long refcount { get; set; }
        private bool conflict { get; set; }
        private object file { get; set; }
        private object fileSubscriptions { get; set; }
        private object stoppedChangingTimeout { get; set; }
        private Emitter emitter { get; set; }
        private object[] changesSinceLastStoppedChangingEvent { get; set; }
        private object[] changesSinceLastDidChangeTextEvent { get; set; }
        private string id { get; set; }
        private NativeTextBuffer buffer { get; set; }
        private object debouncedEmitDidStopChangingEvent { get; set; }
        private object maxUndoEntries { get; set; }
        private LanguageMode languageMode { get; set; }
        private long nextMarkerLayerId { get; set; }
        private long nextDisplayLayerId { get; set; }
        private MarkerLayer defaultMarkerLayer { get; set; }
        private object displayLayers { get; set; }
        private object markerLayers { get; set; }
        private Set markerLayersWithPendingUpdateEvents { get; set; }
        private Set selectionsMarkerLayerIds { get; set; }
        private long nextMarkerId { get; set; }
        private long outstandingSaveCount { get; set; }
        private long loadCount { get; set; }
        private object cachedHasAstral { get; set; }
        private bool _emittedWillChangeEvent { get; set; }

        private bool loaded { get; set; }
        private bool destroyed { get; set; }
        private long transactCallDepth { get; set; }
        private bool digestWhenLastPersisted { get; set; }
        private Action<bool> shouldDestroyOnFileDelete { get; set; }

        private long version { get; set; }
        private Regex newlineRegex { get; set; }
        private object[] spliceArray { get; set; }
        public object Patch { get; set; }
        private long stoppedChangingDelay { get; set; }
        private long fileChangeDelay { get; set; }
        private long backwardsScanChunkSize { get; set; }
        private long defaultMaxUndoEntries { get; set; }
    }
}
