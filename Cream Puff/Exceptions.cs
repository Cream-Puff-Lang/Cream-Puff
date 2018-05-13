using System;

namespace CreamPuff {
    /// <summary>
    /// Syntax error with start and end indices for display.
    /// </summary>
    class SyntaxError : Exception {
        public readonly string Code;
        public readonly int Start;
        public readonly int End;

        /// <summary>
        /// Create a syntax error.
        /// </summary>
        /// <param name="message">Description of error.</param>
        /// <param name="code">Code triggering error.</param>
        /// <param name="start">Start index of token triggering error.</param>
        /// <param name="end">End index of token triggering error.</param>
        public SyntaxError(string message, string code, int start, int end) : base(message) {
            Code = code;
            Start = start;
            End = end;
        }

        // TODO: ToString
    }
}
