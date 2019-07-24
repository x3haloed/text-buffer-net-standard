using System;
using System.Text.RegularExpressions;

namespace TextBuffer
{
    public class TextBuffer
    {
        private object TextEditorComponent = null;
        private object TextEditorElement = null;

        private const ushort SERIALIZATION_VERSION = 1;
        private readonly Regex NON_WHITESPACE_REGEXP = new Regex(@"\S");
        private const char ZERO_WIDTH_NBSP = '\ufeff';
        private long nextId = 0;

        private const string DEFAULT_NON_WORD_CHARACTERS = "/\\()\"':,.;<>~!@#$%^&*|+=[]{}`?-…";

        // Essential: This class represents all essential editing state for a single
        // {TextBuffer}, including cursor and selection positions, folds, and soft wraps.
        // If you're manipulating the state of an editor, use this class.
        //
        // A single {TextBuffer} can belong to multiple editors. For example, if the
        // same file is open in two different panes, Atom creates a separate editor for
        // each pane. If the buffer is manipulated the changes are reflected in both
        // editors, but each maintains its own cursor position, folded lines, etc.
        //
        // ## Accessing TextEditor Instances
        //
        // The easiest way to get hold of `TextEditor` objects is by registering a callback
        // with `::observeTextEditors` on the `atom.workspace` global. Your callback will
        // then be called with all current editor instances and also when any editor is
        // created in the future.
        //
        // ```js
        // atom.workspace.observeTextEditors(editor => {
        //   editor.insertText('Hello World')
        // })
        // ```
        //
        // ## Buffer vs. Screen Coordinates
        //
        // Because editors support folds and soft-wrapping, the lines on screen don't
        // always match the lines in the buffer. For example, a long line that soft wraps
        // twice renders as three lines on screen, but only represents one line in the
        // buffer. Similarly, if rows 5-10 are folded, then row 6 on screen corresponds
        // to row 11 in the buffer.
        //
        // Your choice of coordinates systems will depend on what you're trying to
        // achieve. For example, if you're writing a command that jumps the cursor up or
        // down by 10 lines, you'll want to use screen coordinates because the user
        // probably wants to skip lines *on screen*. However, if you're writing a package
        // that jumps between method definitions, you'll want to work in buffer
        // coordinates.
        //
        // **When in doubt, just default to buffer coordinates**, then experiment with
        // soft wraps and folds to ensure your code interacts with them correctly.
    }
}
