using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace CreamPuff {
    class Node {
        public static Regex IndentA = new Regex("│$", RegexOptions.Compiled);
        public static Regex IndentB = new Regex(" $", RegexOptions.Compiled);
        public NodeType Type;
        public string Data;
        public List<Node> Children;
        public int Start;
        public int End;
        public string Error;

        public Node(NodeType type, int start, int end, string data = null, List<Node> children = null, string error = null) {
            Type = type;
            Start = start;
            End = end;
            Data = data;
            Children = children;
            Error = error;
        }

        public Node(NodeType type, int start, int end, List<Node> children, string data = null, string error = null) {
            Type = type;
            Start = start;
            End = end;
            Data = data;
            Children = children;
            Error = error;
        }

        public override string ToString() => ToString("");

        public string ToString(string indent) {
            var result = IndentB.Replace(IndentA.Replace(indent, "├", 1), "└") + (Data == null ? Type.ToString() : $"{Type}: {(Type == NodeType.String ? Data[Data.Length - 1] + Parser.Escape(Data) : Data)}");
            if (Children == null || Children.Count == 0)
                return result;
            var newIndent = indent + '│';
            for (var i = 0; i < Children.Count - 1; i++)
                result += '\n' + Children[i].ToString(newIndent);
            result += '\n' + Children[Children.Count - 1].ToString(indent + ' ');
            return result;
        }
    }
}
