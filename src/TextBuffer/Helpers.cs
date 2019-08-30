using System;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;

namespace TextBuffer
{
    public static class Helpers
    {
        public static readonly Regex NewlineRegex = new Regex("\r\n|\n|\r");
    }
}
