using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Emit;
using System.Text.RegularExpressions;

namespace CreamPuff {
    using BinaryOperatorLookup = Dictionary<string, Dictionary<Type, Dictionary<Type, Action<ILValue, ILValue, ILScope, ILGenerator, ModuleBuilder>>>>;
    using BinaryOperatorLookup1 = Dictionary<Type, Dictionary<Type, Action<ILValue, ILValue, ILScope, ILGenerator, ModuleBuilder>>>;
    using BinaryOperatorLookup2 = Dictionary<Type, Action<ILValue, ILValue, ILScope, ILGenerator, ModuleBuilder>>;
    // 2 ILValues because Unary operators are given their content too (for parenthesized operators)
    using UnaryOperatorLookup = Dictionary<string, Dictionary<Type, Action<ILValue, ILValue, ILScope, ILGenerator, ModuleBuilder>>>;
    using UnaryOperatorLookup1 = Dictionary<Type, Action<ILValue, ILValue, ILScope, ILGenerator, ModuleBuilder>>;

    // TODO: do end indices go at or after last character
    // TODO: adding new precedence level - incremented all precedences at or above new precedence
    // TODO: comments and ; - for ; insert the ":" tokens as identifiers after every line, discard when in the middle of an operator
    // TODO: prepare pass for IL generation. this will mostly be to set up variables and whatnot.
    /// <summary>
    /// Parses Cream Puff code.
    /// </summary>
    class Parser {
        // TODO: remove '‹', '›'?
        /// <summary>
        /// Map of open bracket to close bracket.
        /// </summary>
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

        /// <summary>
        /// Precedence (as number) for infix operators.
        /// </summary>
        public Dictionary<string, short> InfixPrecedence = new Dictionary<string, short> {
            { "\n", 0 },
            { ";", 0 },
            { ",", 1 },
            { ":", 2 }
        };

        /// <summary>
        /// Precedence (as number) for prefix operators.
        /// </summary>
        public Dictionary<string, short> PrefixPrecedence = new Dictionary<string, short>();

        /// <summary>
        /// Precedence (as number) for postfix operators.
        /// </summary>
        public Dictionary<string, short> PostfixPrecedence = new Dictionary<string, short> {
            // TODO: this probably needs to be changed after thing is done. also reduce precedence because we won't have some things
            { "()", 19 },
            { "{}", 20 }
        };

        /// <summary>
        /// Whether an operator binds right-to-left, for every infix operator.
        /// </summary>
        public Dictionary<string, bool> InfixIsRTL = new Dictionary<string, bool> {
            { "\n", false },
            { ";", false },
            { ",", true },
            { ":", false }
        };

        /// <summary>
        /// Overload lookup for every infix operator (traverses type tree) - operator -> left type -> right type -> (left, right, scope, ilgenerator, modulebuilder) => void
        /// </summary>
        public BinaryOperatorLookup InfixOperators = new BinaryOperatorLookup {
            { ";", new BinaryOperatorLookup1 {
                { typeof(object), new BinaryOperatorLookup2 {
                    { typeof(object), (l, r, s, g, m) => {
                        //
                    } }
                } }
            } },
            { ",", new BinaryOperatorLookup1 {
                { typeof(object), new BinaryOperatorLookup2 {
                    { typeof(object), (l, r, s, g, m) => {
                        //
                    } }
                } }
            } },
            { ":", new BinaryOperatorLookup1 {
                { typeof(object), new BinaryOperatorLookup2 {
                    { typeof(object), (l, r, s, g, m) => {
                        //
                    } }
                } }
            } }
        }.Alias(";", "\n");

        /// <summary>
        /// Overload lookup for every prefix operator (traverses type tree) - operator -> operand type -> (operand, inside if operand is outdix else null, scope, ilgenerator, modulebuilder) => void
        /// </summary>
        public UnaryOperatorLookup PrefixOperators = new UnaryOperatorLookup();

        /// <summary>
        /// Overload lookup for every postfix operator (traverses type tree) - operator -> operand type -> (operand, inside if operand is outdix else null, scope, ilgenerator, modulebuilder) => void
        /// </summary>
        public UnaryOperatorLookup PostfixOperators = new UnaryOperatorLookup {
            { "()", new UnaryOperatorLookup1 {
                { typeof(object), (o, i, s, g, m) => { } }
            } },
            { "{}", new UnaryOperatorLookup1 {
                { typeof(object), (o, i, s, g, m) => { } }
            } }
        };

        /// <summary>
        /// The entire code of the program being parsed.
        /// </summary>
        private string code;

        /// <summary>
        /// Current index of the parser.
        /// </summary>
        private int index = 0;
        // TODO: use this
        /// <summary>
        /// Index of the last finished expression.
        /// </summary>
        private int lastSuccessfulIndex = 0;
        /// <summary>
        /// Nesting level of multiline comments at current index.
        /// </summary>
        private int commentDepth = 0;
        /// <summary>
        /// Whether to keep comments in syntax tree for documentation generation.
        /// </summary>
        private int keepComments = 0;
        /// <summary>
        /// Current character being processed by the parser.
        /// </summary>
        private char Character => code[index];
        /// <summary>
        /// Whether all items in stack do not expect more data.
        /// </summary>
        public bool Finished => stack.Count == 0;
        /// <summary>
        /// Current node being generated by the parser.
        /// </summary>
        private Node Current => stack.Count == 0 ? Node : stack.Peek();
        /// <summary>
        /// Topmost node of syntax tree, i.e. program node.
        /// </summary>
        public Node Node { get; private set; }
        /// <summary>
        /// Stack of parents of current node.
        /// </summary>
        private Stack<Node> stack = new Stack<Node>();
        /// <summary>
        /// Stack of closing brackets.
        /// </summary>
        private Stack<char> bracketStack = new Stack<char>();

        /// <summary>
        /// Initialized parser with specified code.
        /// </summary>
        /// <param name="code">Code to initialize parser with.</param>
        public Parser(string code="") {
            this.code = code;
            Node = new Node(NodeType.Expression, 0, 0, null, new List<Node>());
        }

        /// <summary>
        /// Add more code to parser.
        /// </summary>
        /// <param name="code">Code to add.</param>
        /// <returns>Itself, to provide fluent interface.</returns>
        public Parser AddCode(string code) {
            this.code += code;
            return this;
        }

        /// <summary>
        /// Parse current code.
        /// </summary>
        /// <returns>Itself, to provide fluent interface.</returns>
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

        /// <summary>
        /// Mutate program into a form usable by interpreter.
        /// </summary>
        public void Process() => Process(Node);

        /// <summary>
        /// Mutate node into a form usable by interpreter.
        /// </summary>
        /// <param name="node">Node to process.</param>
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

        /// <summary>
        /// Clear all parse data. Replaces, not resets, existing data.
        /// </summary>
        public void Clear() {
            code = "";
            index = 0;
            Node = new Node(NodeType.Expression, 0, 0, null, new List<Node>());
            stack = new Stack<Node>();
            bracketStack = new Stack<char>();
        }

        /// <summary>
        /// Consume whitespace.
        /// </summary>
        /// <returns>Consumed whitespace.</returns>
        public string Whitespace() {
            var match = Regexes.Whitespace.Match(code, index);
            index += match.Length;
            return match.Value;
        }
        
        /// <summary>
        /// Try parsing comment.
        /// </summary>
        /// <param name="node">Node with contents of comment.</param>
        /// <returns>Whether a comment was found.</returns>
        public bool Comment(out Node node) {
            // TODO
            node = null;
            return false;
        }

        /// <summary>
        /// Try parsing string.
        /// </summary>
        /// <param name="node">Node with contents of string.</param>
        /// <returns>Whether a string was found.</returns>
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

        /// <summary>
        /// Finish current string node. Does not check if current node is a string node.
        /// </summary>
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

        /// <summary>
        /// Try parsing expression.
        /// </summary>
        /// <param name="node">Node with contents of expression.</param>
        /// <returns>Whether an expression or part thereof was found.</returns>
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
            while (index != code.Length && (Identifier(out var result) || String(out result) || Number(out result) || BracketedExpression(out result)))
                node.Children.Add(result);
            if (Current.Equals(node))
                stack.Pop();
            return node.Children.Count != 0;
        }

        /// <summary>
        /// Finish current expression node. Does not check if current node is expression node.
        /// </summary>
        public void FinishExpression() {
            Current.End = index;
            stack.Pop();
        }

        /// <summary>
        /// Process expression, using modified shunting yard algorithm to build syntax tree.
        /// </summary>
        /// <param name="node">Node to process.</param>
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
                            if (commaIsRTL ? commaPrecedence < previous.precedence : commaPrecedence <= previous.precedence) {
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
                                    throw new SyntaxError($"Prefix operator expected, {(InfixOperators.ContainsKey(op.Data) ? "infix" : "postfix")} operator ({op.Data}) found", code, op.Start, op.End);
                                op.Type = NodeType.Prefix;
                                operators.Push((op, PrefixPrecedence[op.Data]));
                            }
                            stack.Push(child);
                            first = false;
                            continue;
                        }
                        int infixIndex = -1, infixPrecedence = -1, end = 0, start = consecutiveOperators.Count - 1;
                        bool allPrefix = false, allPostfix = false;
                        if (child == last) {
                            allPostfix = true;
                            infixIndex = consecutiveOperators.Count;
                        } else {
                            for (; PostfixOperators.ContainsKey(consecutiveOperators[end++].Data) || consecutiveOperators[end - 1].Data == "\n";)
                                if (end == consecutiveOperators.Count) {
                                    allPostfix = true;
                                    break;
                                }
                            for (; PrefixOperators.ContainsKey(consecutiveOperators[start--].Data) || consecutiveOperators[start + 1].Data == "\n";)
                                if (start == -1) {
                                    allPrefix = true;
                                    break;
                                }
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
                                throw new SyntaxError($"Infix operator expected, none found", code, consecutiveOperators[0].Start, consecutiveOperators.Last().End);
                            if (infixPrecedence == -1)
                                if (allPostfix)
                                    infixIndex = consecutiveOperators.Count;
                                else if (allPrefix)
                                    infixIndex = -1;
                        }
                        if (allPostfix || allPrefix || infixPrecedence != -1 && consecutiveOperators[infixIndex].Data != "\n")
                            for (i = 0; i < consecutiveOperators.Count; i++)
                                if (consecutiveOperators[i].Data == "\n") {
                                    consecutiveOperators.RemoveAt(i--);
                                    if (i < infixIndex)
                                        infixIndex--;
                                }
                        i = 0;
                        while (i < infixIndex) {
                            var curr = consecutiveOperators[i++];
                            var currPrecedence = PostfixPrecedence[curr.Data];
                            Node item;
                            while (operators.Count != 0) {
                                var previous = operators.Peek();
                                var prev = previous.node;
                                if (currPrecedence <= previous.precedence) {
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
                        if (child == last)
                            break; // because infixOperator shouldn't be here
                        var infixOperator = infixIndex != -1 && infixIndex != consecutiveOperators.Count ? consecutiveOperators[i++] : new Node(NodeType.Infix, -1, -1, ",");
                        infixOperator.Type = NodeType.Infix;
                        var isRTL = InfixIsRTL[infixOperator.Data];
                        while (operators.Count != 0) {
                            var previous = operators.Peek();
                            var prev = previous.node;
                            if (isRTL ? infixPrecedence < previous.precedence : infixPrecedence <= previous.precedence) {
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
                        while (i < consecutiveOperators.Count) {
                            var op = consecutiveOperators[i++];
                            op.Type = NodeType.Prefix;
                            operators.Push((op, PrefixPrecedence[op.Data]));
                        }
                        consecutiveOperators.Clear();
                        stack.Push(child);
                    }
                }
            }
            // only infix operators should remaining
            // TODO: figure out whether prefix operators may remain - this can be done by checking only when child == last
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

        /// <summary>
        /// Try parsing bracketed expression.
        /// </summary>
        /// <param name="node">Node with contents of bracketed expression.</param>
        /// <returns>Whether a btacketed expression or part thereof was found.</returns>
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

        /// <summary>
        /// Finish current bracket node. Does not check if current node is bracket node.
        /// </summary>
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

        /// <summary>
        /// Process bracketed expression. This processes contents as expression, except when only contents are a single operator, in which case returns reference to operator as function.
        /// </summary>
        /// <param name="node">Node to process.</param>
        public void ProcessBracket(Node node) {
            if (
                node.Children.Count > 0 &&
                node.Children.All(child => child.Type == NodeType.Identifier && (child.Data == "\n" || InfixOperators.ContainsKey(child.Data))) &&
                node.Children.Any(child => child.Data != "\n")
            ) {
                // functionized operator
                node.Type = NodeType.Identifier;
                node.Data = node.Children.First(child => child.Data != "\n").Data;
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
            var newline = Regexes.ContainsNewline.Match(Whitespace());
            if (newline.Success) {
                node = new Node(NodeType.Identifier, index + newline.Index, index + newline.Index + 1, "\n");
                return true;
            }
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
            /// <summary>
            /// Match escape sequences in double quoted strings.
            /// </summary>
            public static Regex DoubleQuotedEscapes = new Regex(@"\\(x[0-9a-fA-F]{2}|x\{[0-9a-fA-F]+\}|[""nt])", RegexOptions.Compiled);
            /// <summary>
            /// Match escape sequences in single quoted strings.
            /// </summary>
            public static Regex SingleQuotedEscapes = new Regex(@"\\(x[0-9a-fA-F]{2}|x\{[0-9a-fA-F]+\}|['nt])", RegexOptions.Compiled);
            /// <summary>
            /// Match characters which must be escaped in double quoted strings.
            /// </summary>
            public static Regex DoubleQuotedUnescapes = new Regex(@"\p{C}|""", RegexOptions.Compiled);
            /// <summary>
            /// Match characters which must be escaped in single quoted strings.
            /// </summary>
            public static Regex SingleQuotedUnescapes = new Regex(@"\p{C}|'", RegexOptions.Compiled);
            /// <summary>
            /// Match any whitespace character but space.
            /// </summary>
            public static Regex ContainsNonSpace = new Regex(@"[\r\n\t\f\v]", RegexOptions.Singleline | RegexOptions.Compiled);
            /// <summary>
            /// Match newline character.
            /// </summary>
            public static Regex ContainsNewline = new Regex(@"[\r\n]", RegexOptions.Singleline | RegexOptions.Compiled);
            /// <summary>
            /// Match all whitespace starting from current index.
            /// </summary>
            public static Regex Whitespace = new Regex(@"\G\s*", RegexOptions.Singleline | RegexOptions.Compiled);
            /// <summary>
            /// Match number starting from current index.
            /// </summary>
            public static Regex Number = new Regex(@"\G\d[\d_]*(\.[\d_]+)?");
            /// <summary>
            /// Match line comment starting from current index.
            /// </summary>
            public static Regex LineComment = new Regex(@"\G#.+", RegexOptions.Compiled);
            /// <summary>
            /// Match multiline comment starting from current index. Ends either at end of string, before multiline comment opening tag, or before multiline comment closing tag.
            /// </summary>
            public static Regex MultilineComment = new Regex(@"\G[^#%]|%(?:[^#]|$)|#(?:[^%]|$)", RegexOptions.Singleline | RegexOptions.Compiled);
            /// <summary>
            /// Match contents of double quoted string starting from current index.
            /// </summary>
            public static Regex DoubleQuotedString = new Regex(@"\G(?:\\x[0-9a-fA-F]{2}|\\x\{[0-9a-fA-F]+\}|\\[""nt]|[^""])*", RegexOptions.Singleline | RegexOptions.Compiled);
            /// <summary>
            /// Match contents of single quoted string starting from current index.
            /// </summary>
            public static Regex SingleQuotedString = new Regex(@"\G(?:\\x[0-9a-fA-F]{2}|\\x\{[0-9a-fA-F]+\}|\\['nt]|[^'])*", RegexOptions.Singleline | RegexOptions.Compiled);
            /// <summary>
            /// Match identifier starting from current index.
            /// </summary>
            public static Regex Identifier = new Regex(@"\G\$+|\G\$*[_\p{L}]+|\G\$*[^\p{L}\p{M}\p{C}\p{Z}0-9'""\$\[\]\{\}\(\)\
“”‘’‹›«»（）［］｛｝｟｠⦅⦆〚〛⦃⦄「」〈〉《》【】〔〕⦗⦘『』〖〗〘〙｢｣⟦⟧⟨⟩⟪⟫⟮⟯⟬⟭⌈⌉⌊⌋⦇⦈⦉⦊❛ ❜ ❝ ❞❨❩❪❫❴❵❬❭❮❯❰❱❲❳﴾﴿〈〉⦑⦒⧼⧽﹙﹚﹛﹜﹝﹞⁽⁾₍₎⦋⦌⦍⦎⦏⦐⁅⁆⸢⸣⸤⸥⟅⟆⦓⦔⦕⦖⸦⸧⸨⸩⧘⧙⧚⧛⸜⸝⸌⸍⸂⸃⸄⸅⸉⸊᚛᚜༺༻༼༽]+", RegexOptions.Singleline | RegexOptions.Compiled);
        }
    }

    static class DictionaryExtensions {
        /// <summary>
        /// Set keys of a dictionay to the same value as that of another key.
        /// </summary>
        /// <typeparam name="K">Dictionary key type.</typeparam>
        /// <typeparam name="V">Dictionary value type.</typeparam>
        /// <param name="dictionary"Dictionary to modify.></param>
        /// <param name="original">Key with target valkue.</param>
        /// <param name="_new">Keys to set value of.</param>
        /// <returns>Dictionary, to provide fluent interface.</returns>
        public static Dictionary<K, V> Alias<K, V>(this Dictionary<K, V> dictionary, K original, params K[] _new) {
            foreach (var key in _new)
                dictionary[key] = dictionary[original];
            return dictionary;
        }
    }
}
