﻿using Antlr4.Runtime;

namespace PrettierGML.Nodes.SyntaxNodes
{
    internal class MacroDeclaration : GmlSyntaxNode
    {
        public GmlSyntaxNode Id { get; set; }
        public string Body { get; set; }

        public MacroDeclaration(
            ParserRuleContext context,
            CommonTokenStream tokenStream,
            GmlSyntaxNode id,
            string body
        )
            : base(context, tokenStream)
        {
            Id = id;
            Body = body;
        }

        public override Doc Print()
        {
            return Doc.Concat("#macro", " ", Id.Print(), " ", Body);
        }
    }
}
