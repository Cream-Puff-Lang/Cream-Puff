using System;
using System.Collections.Generic;
using System.Text;

namespace CreamPuff {
    class SyntaxError : Exception {
        public readonly int Start;
        public readonly int End;

        public SyntaxError(string message, int start, int end) : base(message) {
            Start = start;
            End = end;
        }
    }
}
