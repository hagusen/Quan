﻿using Antlr4.Runtime;
using PrettierGML.Printer.DocTypes;
using PrettierGML.SyntaxNodes.PrintHelpers;

namespace PrettierGML.SyntaxNodes.Gml
{
    internal class ArrayExpression : GmlSyntaxNode
    {
        public List<GmlSyntaxNode> Elements => Children;

        public ArrayExpression(ParserRuleContext context, List<GmlSyntaxNode> elements)
            : base(context)
        {
            AsChildren(elements);
        }

        public override Doc Print(PrintContext ctx)
        {
            return DelimitedList.PrintInBrackets(ctx, "[", this, "]", ",");
        }
    }
}