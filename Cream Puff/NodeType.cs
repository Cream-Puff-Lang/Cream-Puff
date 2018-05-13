namespace CreamPuff {
    /// <summary>
    /// Possible types of syntax tree node.
    /// </summary>
    enum NodeType {
        Expression,
        Identifier,
        Integer,
        Float,
        String,
        List,
        Set,
        Tuple,
        Dictionary,
        Infix,
        Prefix,
        Postfix,
        Outfix,
        PostfixOutfix,
        Comment
    }
}
