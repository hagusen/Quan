﻿using Antlr4.Runtime;
using Antlr4.Runtime.Tree;

namespace PrettierGML.Nodes.SyntaxNodes
{
    internal class Literal : GmlSyntaxNode
    {
        public string Text { get; set; }

        public Literal(ParserRuleContext context, string text)
            : base(context)
        {
            Text = text;
        }

        public Literal(ITerminalNode context, string text)
            : base(context)
        {
            Text = text;
        }

        public override Doc Print(PrintContext ctx)
        {
            return Text;
        }
    }
}
