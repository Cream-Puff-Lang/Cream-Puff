using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Emit;
using System.Text.RegularExpressions;

namespace CreamPuff {
    using BinaryOperatorLookup = Dictionary<string, Dictionary<Type, Dictionary<Type, Action<ILValue, ILValue, ILScope, ILGenerator, ModuleBuilder>>>>;
    // using BinaryOperatorLookup1 = Dictionary<Type, Dictionary<Type, Action<ILValue, ILValue, ILScope, ILGenerator, ModuleBuilder>>>;
    // using BinaryOperatorLookup2 = Dictionary<Type, Action<ILValue, ILValue, ILScope, ILGenerator, ModuleBuilder>>;
    // 2 ILValues because Unary operators are given their content too (for parenthesized operators)
    using UnaryOperatorLookup = Dictionary<string, Dictionary<Type, Action<ILValue, ILValue, ILScope, ILGenerator, ModuleBuilder>>>;
    using UnaryOperatorLookup1 = Dictionary<Type, Action<ILValue, ILValue, ILScope, ILGenerator, ModuleBuilder>>;

    // TODO: do end indices go at or after last character
    // TODO: adding new precedences
    class Parser {
        // TODO: remove '‹', '›'?
        public static Dictionary<char, char> BracketLookup = new Dictionary<char, char> {
            { '(', ')' }, { '{', '}' }, { '[' , ']' }, { '“', '”' }, { '‘', '’' }, { '‹', '›' },
            { '«', '»' }, { '（', '）' }, { '［', '］' }, { '｛', '｝' }, { '｟', '｠' }, { '⦅', '⦆' }, { '〚', '〛' },
            { '⦃', '⦄' }, { '「', '」' }, { '〈', '〉' }, { '《', '》' }, { '【', '】' }, { '〔', '〕' }, { '⦗', '⦘' },
            { '『', '』' }, { '〖', '〗' }, { '〘', '〙' }, { '｢', '｣' }, { '⟦', '⟧' }, { '⟨', '⟩' }, { '⟪', '⟫' },
            { '⟮', '⟯' }, { '⟬', '⟭' }, { '⌈', '⌉' }, { '⌊', '⌋' }, { '⦇', '⦈' }, { '⦉', '⦊' }, { '❛', '❜' },
            { '❝', '❞' }, { '❨', '❩' }, { '❪', '❫' }, { '❴', '❵' }, { '❬', '❭' }, { '❮', '❯' }, { '❰', '❱' },
            { '❲', '❳' }, { '﴾', '﴿' }, { '〈', '〉' }, { '⦑', '⦒' }, { '⧼', '⧽' }, { '﹙', '﹚' }, { '﹛', '﹜' },
            { '﹝', '﹞' }, { '⁽', '⁾' }, { '₍', '₎' }, { '⦋', '⦌' }, { '⦍', '⦎' }, { '⦏', '⦐' }, { '⁅', '⁆' },
            { '⸢', '⸣' }, { '⸤', '⸥' }, { '⟅', '⟆' }, { '⦓', '⦔' }, { '⦕', '⦖' }, { '⸦', '⸧' }, { '⸨', '⸩' },
            { '⧘', '⧙' }, { '⧚', '⧛' }, { '⸜', '⸝' }, { '⸌', '⸍' }, { '⸂', '⸃' }, { '⸄', '⸅' }, { '⸉', '⸊' },
            { '᚛', '᚜' }, {'༺', '༻' }, { '༼', '༽' },
        };

        public Dictionary<string, short> InfixPrecedence = new Dictionary<string, short> {
            { ",", 0 }
        };

        public Dictionary<string, short> PrefixPrecedence = new Dictionary<string, short>();

        public Dictionary<string, short> PostfixPrecedence = new Dictionary<string, short> {
            // TODO: this probably needs to be changed after thing is done. also reduce precedence because we won't have some things
            { "()", 19 },
            { "{}", 20 }
        };

        public Dictionary<string, bool> InfixIsRTL = new Dictionary<string, bool> {
            { ",", true }
        };

        public BinaryOperatorLookup InfixOperators = new BinaryOperatorLookup();

        public UnaryOperatorLookup PrefixOperators = new UnaryOperatorLookup();

        public UnaryOperatorLookup PostfixOperators = new UnaryOperatorLookup {
            { "()", new UnaryOperatorLookup1 {
                { typeof(object), (o, i, s, g, m) => { } }
            } },
            { "{}", new UnaryOperatorLookup1 {
                { typeof(object), (o, i, s, g, m) => { } }
            } }
        };

        private string code;
        private int index = 0;
        private int lastSuccessfulIndex = 0;
        private char Character => code[index];
        public bool Finished => stack.Count == 0;
        private Node Current => stack.Count == 0 ? Node : stack.Peek();
        public Node Node { get; private set; }
        private Stack<Node> stack = new Stack<Node>();
        private Stack<char> bracketStack = new Stack<char>();


        public Parser(string code) {
            this.code = code;
            Node = new Node(NodeType.Expression, 0, 0, null, new List<Node>());
        }

        public Parser AddCode(string code) {
            this.code += code;
            return this;
        }

        public Parser Parse() {
            Whitespace();
            while (index != code.Length && !Finished) {
                switch (Current.Type) {
                    case NodeType.String:
                        FinishString();
                        break;
                    case NodeType.Outfix:
                        FinishBracket();
                        break;
                    case NodeType.Expression:
                        FinishExpression();
                        break;
                }
                Whitespace();
            }
            while (index != code.Length) {
                if (Expression(out var node)) {
                    Node.Children.Add(node);
                    continue;
                }
                Whitespace();
                if (Finished)
                    lastSuccessfulIndex = node.End = index;
            }
            Node.End = index;
            while (!Finished && Current.Type == NodeType.Expression)
                stack.Pop();
            return this;
        }

        public void Process() => Process(Node);

        public void Process(Node node) {
            switch (node.Type) {
                case NodeType.Expression:
                    ProcessExpression(node);
                    return;
                case NodeType.Outfix:
                    ProcessBracket(node);
                    return;
            }
        }

        public void Clear() {
            code = "";
            index = 0;
            Node = new Node(NodeType.Expression, 0, 0, null, new List<Node>());
            stack = new Stack<Node>();
            bracketStack = new Stack<char>();
        }

        public char Whitespace() {
            var match = Regexes.Whitespace.Match(code, index);
            var length = (char) match.Length;
            index += length;
            return Regexes.ContainsNonSpace.IsMatch(match.Value) ? (char) 0 : length ;
        }

        public bool String(out Node node) {
            Whitespace();
            var originalIndex = index;
            if (index == code.Length) {
                node = null;
                return false;
            }
            Regex matcher;
            char quote;
            if (Character == '\'') {
                matcher = Regexes.SingleQuotedString;
                quote = '\'';
            } else if (Character == '"') {
                matcher = Regexes.DoubleQuotedString;
                quote = '"';
            } else {
                node = null;
                index = originalIndex;
                return false;
            }
            var match = matcher.Match(code, index + 1);
            if (!match.Success) {
                node = null;
                index = originalIndex;
                return false;
            }
            index += match.Length + 1;
            node = new Node(NodeType.String, originalIndex, index, match.Value);
            node.Data += quote;
            if (index < code.Length && Character == quote)
                index++;
            return true;
        }

        public void FinishString() {
            Regex matcher;
            var quote = Node.Data[Node.Data.Length - 1];
            if (quote == '\'')
                matcher = Regexes.SingleQuotedString;
            else
                matcher = Regexes.DoubleQuotedString;
            var match = matcher.Match(code, index);
            index += match.Length;
            Node.Data = Node.Data.Substring(0, Node.Data.Length - 1) + match.Value + quote;
            Node.End = index;;
            if (index < code.Length && Character == quote)
                index++;
        }

        public bool Expression(out Node node) {
            Whitespace();
            var originalIndex = index;
            if (index == code.Length) {
                node = null;
                index = originalIndex;
                return false;
            }
            // TODO
            node = new Node(NodeType.Expression, 0, 0, null, new List<Node>());
            stack.Push(node);
            while (index != code.Length && (String(out var result) || Number(out result) || Identifier(out result) || BracketedExpression(out result)))
                node.Children.Add(result);
            if (Current.Equals(node))
                stack.Pop();
            return node.Children.Count != 0;
        }

        public void FinishExpression() {
            Current.End = index;
            stack.Pop();
        }

        public void ProcessExpression(Node node) {
            var stack = new Stack<Node>();
            var operators = new Stack<(Node node, short precedence)>();
            var consecutiveOperators = new List<Node>();
            var first = true;
            // really hacky
            var last = new Node(NodeType.Comment, 0, 0);
            // TODO: what happens if comma is removed from scope
            var commaPrecedence = InfixPrecedence[","];
            var commaIsRTL = InfixIsRTL[","];
            node.Children.Add(last);
            // TODO: oops. operators, not stack
            foreach (var child in node.Children) {
                Process(child);
                if ((child.Type == NodeType.Identifier || child.Type == NodeType.Outfix) && (
                    PrefixOperators.ContainsKey(child.Data) ||
                    InfixOperators.ContainsKey(child.Data) ||
                    PostfixOperators.ContainsKey(child.Data)
                ))
                    consecutiveOperators.Add(child);
                else {
                    if (consecutiveOperators.Count == 0 && stack.Count != 0 && child != last) {
                        var end = stack.Peek().End;
                        // comma may be reassigned. implicit comma stays though, not sure if it's a bad idea
                        while (operators.Count != 0) {
                            var previous = operators.Peek();
                            var prev = previous.node;
                            if (commaIsRTL ? previous.precedence < commaPrecedence : previous.precedence <= commaPrecedence) {
                                if (prev.Type == NodeType.Prefix) {
                                    var item = stack.Pop();
                                    stack.Push(new Node(NodeType.Prefix, prev.Start, item.End, new List<Node> { item }, prev.Data));
                                } else {
                                    var right = stack.Pop();
                                    var left = stack.Pop();
                                    stack.Push(new Node(NodeType.Infix, left.Start, right.End, new List<Node> { left, right }, prev.Data));
                                }
                                operators.Pop();
                            } else
                                break;
                        }
                        operators.Push((new Node(NodeType.Infix, end, end, ","), commaPrecedence));
                        stack.Push(child);
                    } else {
                        int i;
                        if (first) {
                            foreach (var op in consecutiveOperators) {
                                if (!PrefixOperators.ContainsKey(op.Data))
                                    throw new SyntaxError($"Prefix operator expected, {(InfixOperators.ContainsKey(op.Data) ? "infix" : "postfix")} operator ({op.Data}) found", op.Start, op.End);
                                op.Type = NodeType.Prefix;
                                operators.Push((op, PrefixPrecedence[op.Data]));
                            }
                            stack.Push(child);
                            first = false;
                            continue;
                        }
                        int infixIndex = -1, infixPrecedence = -1, end = -1, start = consecutiveOperators.Count;
                        if (child == last)
                            infixIndex = consecutiveOperators.Count;
                        else {
                            for (; ++end < consecutiveOperators.Count && PostfixOperators.ContainsKey(consecutiveOperators[end].Data);) ;
                            for (; --start >= 0 && PrefixOperators.ContainsKey(consecutiveOperators[start].Data);) ;
                            end--;
                            start++;
                            for (i = start; i <= end; i++) {
                                var op = consecutiveOperators[start].Data;
                                if (InfixOperators.ContainsKey(op) && InfixPrecedence[op] > infixPrecedence) {
                                    infixIndex = start;
                                    infixPrecedence = InfixPrecedence[op];
                                }
                            }
                            if (infixPrecedence == -1 && end < consecutiveOperators.Count - 1 && start > 0)
                                throw new SyntaxError($"Infix operator expected, none found", consecutiveOperators[0].Start, consecutiveOperators.Last().End);
                            if (infixPrecedence == -1)
                                if (end == consecutiveOperators.Count - 1)
                                    infixIndex = consecutiveOperators.Count;
                                else if (start == 0)
                                    infixIndex = -1;
                        }
                        for (i = 0; i < infixIndex; i++) {
                            var curr = consecutiveOperators[i];
                            var currPrecedence = PostfixPrecedence[curr.Data];
                            Node item;
                            while (operators.Count != 0) {
                                var previous = operators.Peek();
                                var prev = previous.node;
                                if (previous.precedence <= currPrecedence) {
                                    if (prev.Type == NodeType.Prefix) {
                                        item = stack.Pop();
                                        stack.Push(new Node(NodeType.Prefix, prev.Start, item.End, new List<Node> { item }, prev.Data));
                                    } else {
                                        var right = stack.Pop();
                                        var left = stack.Pop();
                                        stack.Push(new Node(NodeType.Infix, left.Start, right.End, new List<Node> { left, right }, prev.Data));
                                    }
                                    operators.Pop();
                                } else
                                    break;
                            }
                            item = stack.Pop();
                            if (curr.Type == NodeType.Outfix)
                                stack.Push(new Node(NodeType.PostfixOutfix, item.Start, curr.End, new List<Node> { item, curr }));
                            else
                                stack.Push(new Node(NodeType.Postfix, item.Start, curr.End, new List<Node> { item }, curr.Data));
                        }
                        if (child == last || end == consecutiveOperators.Count - 1)
                            break; // because infixOperator doesn't exist. or does it? <vsauce music starts>
                        if (i > 0) {
                            var infixOperator = consecutiveOperators[i++];
                            infixOperator.Type = NodeType.Infix;
                            var isRTL = InfixIsRTL[infixOperator.Data];
                            while (operators.Count != 0) {
                                var previous = operators.Peek();
                                var prev = previous.node;
                                if (isRTL ? previous.precedence < infixPrecedence : previous.precedence <= infixPrecedence) {
                                    if (prev.Type == NodeType.Prefix) {
                                        var item = stack.Pop();
                                        stack.Push(new Node(NodeType.Prefix, prev.Start, item.End, new List<Node> { item }, prev.Data));
                                    } else {
                                        var right = stack.Pop();
                                        var left = stack.Pop();
                                        stack.Push(new Node(NodeType.Infix, left.Start, right.End, new List<Node> { left, right }, prev.Data));
                                    }
                                    operators.Pop();
                                } else
                                    break;
                            }
                            operators.Push((infixOperator, InfixPrecedence[infixOperator.Data]));
                        }
                        for (; i < consecutiveOperators.Count; i++) {
                            var op = consecutiveOperators[i];
                            op.Type = NodeType.Prefix;
                            operators.Push((op, PrefixPrecedence[op.Data]));
                        }
                        stack.Push(child);
                    }
                }
            }
            // lsat should only leave the infix
            while (operators.Count != 0) {
                var right = stack.Pop();
                var left = stack.Pop();
                stack.Push(new Node(NodeType.Infix, left.Start, right.End, new List<Node> { left, right }, operators.Pop().node.Data));
            }
            var result = stack.Pop();
            if (node.Type == NodeType.Outfix) {
                node.Children = new List<Node> { result };
            } else {
                node.Type = result.Type;
                node.Children = result.Children;
                node.Data = result.Data;
            }
        }

        // TODO: this might not be needed -prefix and postfix should be applied directly
        public Node CreateOperatorExpression(Stack<Node> nodes, Node op) {
            if (op.Type == NodeType.Infix) {
                Node right = stack.Pop(),
                    left = stack.Pop();
                return new Node(NodeType.Infix, left.Start, right.End, new List<Node> { left, right }, op.Data);
            } else if (op.Type == NodeType.Prefix) {
                var element = stack.Pop();
                return new Node(NodeType.Prefix, op.Start, element.End, new List<Node> { element });
            } else if (op.Type == NodeType.Postfix) {
                var element = stack.Pop();
                return new Node(NodeType.Postfix, element.Start, op.End, new List<Node> { element });
            } else if (op.Type == NodeType.PostfixOutfix) {
                var element = stack.Pop();
                return new Node(NodeType.PostfixOutfix, element.Start, op.End, new List<Node> { element, op });
            }
            return null;
        }

        public bool BracketedExpression(out Node node) {
            Whitespace();
            var originalIndex = index;
            if (index == code.Length) {
                node = null;
                index = originalIndex;
                return false;
            }
            if (!BracketLookup.ContainsKey(Character)) {
                node = null;
                index = originalIndex;
                return false;
            }
            bracketStack.Push(BracketLookup[Character]);
            var character = Character;
            index++;
            if (Expression(out node)) {
                node.Type = NodeType.Outfix;
                node.Data = character.ToString() + BracketLookup[character].ToString();
                if (Current.Equals(node))
                    return true;
            } else {
                node = new Node(NodeType.Outfix, originalIndex, 0, character.ToString() + BracketLookup[character].ToString(), new List<Node>());
            }
            stack.Push(node);
            Whitespace();
            if (index != code.Length && Character == bracketStack.Peek()) {
                index++;
                node.End = index;
                stack.Pop();
                bracketStack.Pop();
            }
            return true;
        }

        public void FinishBracket() {
            var current = Current;
            if (Expression(out var node))
                current.Children.AddRange(node.Children);
            Whitespace();
            if (index != code.Length && Character == bracketStack.Peek()) {
                index++;
                current.End = index;
                stack.Pop();
                bracketStack.Pop();
            }
        }

        public void ProcessBracket(Node node) {
            if (node.Children.Count == 1 && node.Children[0].Type == NodeType.Identifier && InfixOperators.ContainsKey(node.Children[0].Data)) {
                // functionized operator
                node.Type = NodeType.Identifier;
                node.Data = node.Children[0].Data;
                node.Children = null;
            } else
                ProcessExpression(node);
        }

        public bool Number(out Node node) {
            Whitespace();
            var originalIndex = index;
            if (index == code.Length) {
                node = null;
                index = originalIndex;
                return false;
            }
            var match = Regexes.Number.Match(code, index);
            if (!match.Success) {
                node = null;
                index = originalIndex;
                return false;
            }
            index += match.Length;
            node = new Node(match.Groups[1].Length == 0 ? NodeType.Integer : NodeType.Float, originalIndex, index, match.Value.Replace("_", ""));
            return true;
        }

        public bool Identifier(out Node node) {
            Whitespace();
            var originalIndex = index;
            if (index == code.Length) {
                node = null;
                index = originalIndex;
                return false;
            }
            var match = Regexes.Identifier.Match(code, index);
            if (!match.Success) {
                node = null;
                index = originalIndex;
                return false;
            }
            index += match.Length;
            node = new Node(NodeType.Identifier, originalIndex, index, match.Value);
            return true;
        }

        // TODO: where to put this? surely not in parser?
        public static string Unescape(string s) => (s[s.Length - 1] == '"' ? Regexes.DoubleQuotedEscapes : Regexes.SingleQuotedEscapes).Replace(s, m => {
            var g1 = m.Groups[1].Value;
            if (g1[0] != 'x')
                switch (g1[0]) {
                    case var c when c == s[s.Length - 1]:
                        return c.ToString();
                    case 'n':
                        return "\n";
                    case 't':
                        return "\t";
                }
            if (g1[1] == '{')
                return ((char) Convert.ToUInt32(g1.Substring(2, g1.Length - 3), 16)).ToString();
            return ((char) Convert.ToUInt32(g1.Substring(1), 16)).ToString();
        });

        public static string Escape(string s) => (s[s.Length - 1] == '"' ? Regexes.DoubleQuotedUnescapes : Regexes.SingleQuotedUnescapes).Replace(s.Substring(0, s.Length - 1), m => {
            var g1 = m.Groups[0].Value;
            switch (g1[0]) {
                case var c when c == s[s.Length -1]:
                    return "\\" + c;
                case '\n':
                    return "\\n";
                case '\t':
                    return "\\t";
                default:
                    if (g1[0] < 255)
                        return "\\x" + ((int) g1[0]).ToString("X2");
                    return $"\\x{{{((int) g1[0]).ToString("X")}}}";
            }
        }) + s[s.Length - 1];

        public static class Regexes {
            // TODO: comments, nested multiline comments, regex
            public static Regex DoubleQuotedEscapes = new Regex(@"\\(x[0-9a-fA-F]{2}|x\{[0-9a-fA-F]+\}|[""nt])", RegexOptions.Compiled);
            public static Regex SingleQuotedEscapes = new Regex(@"\\(x[0-9a-fA-F]{2}|x\{[0-9a-fA-F]+\}|['nt])", RegexOptions.Compiled);
            public static Regex DoubleQuotedUnescapes = new Regex(@"\p{C}|""", RegexOptions.Compiled);
            public static Regex SingleQuotedUnescapes = new Regex(@"\p{C}|'", RegexOptions.Compiled);
            public static Regex ContainsNonSpace = new Regex(@"[\r\n\t\f\v]", RegexOptions.Singleline | RegexOptions.Compiled);
            public static Regex Whitespace = new Regex(@"\G\s*", RegexOptions.Singleline | RegexOptions.Compiled);
            public static Regex Number = new Regex(@"\G\d[\d_]*(\.[\d_]+)?");
            public static Regex DoubleQuotedString = new Regex(@"\G(?:\\x[0-9a-fA-F]{2}|\\x\{[0-9a-fA-F]+\}|\\[""nt]|[^""])*", RegexOptions.Singleline | RegexOptions.Compiled);
            public static Regex SingleQuotedString = new Regex(@"\G(?:\\x[0-9a-fA-F]{2}|\\x\{[0-9a-fA-F]+\}|\\['nt]|[^'])*", RegexOptions.Singleline | RegexOptions.Compiled);
            public static Regex Identifier = new Regex(@"\G\$+|\G\$*[_\p{L}]+|\G\$*[^\p{L}\p{M}\p{C}\p{Z}0-9'"";\$\[\]\{\}\(\)\
“”‘’‹›«»（）［］｛｝｟｠⦅⦆〚〛⦃⦄「」〈〉《》【】〔〕⦗⦘『』〖〗〘〙｢｣⟦⟧⟨⟩⟪⟫⟮⟯⟬⟭⌈⌉⌊⌋⦇⦈⦉⦊❛ ❜ ❝ ❞❨❩❪❫❴❵❬❭❮❯❰❱❲❳﴾﴿〈〉⦑⦒⧼⧽﹙﹚﹛﹜﹝﹞⁽⁾₍₎⦋⦌⦍⦎⦏⦐⁅⁆⸢⸣⸤⸥⟅⟆⦓⦔⦕⦖⸦⸧⸨⸩⧘⧙⧚⧛⸜⸝⸌⸍⸂⸃⸄⸅⸉⸊᚛᚜༺༻༼༽]+", RegexOptions.Singleline | RegexOptions.Compiled);
        }
    }
}
