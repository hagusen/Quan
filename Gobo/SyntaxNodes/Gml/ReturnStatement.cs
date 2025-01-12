﻿using Gobo.Printer.DocTypes;

namespace Gobo.SyntaxNodes.Gml;

public sealed class ReturnStatement : GmlSyntaxNode
{
    public GmlSyntaxNode Argument { get; set; }

    public ReturnStatement(TextSpan span, GmlSyntaxNode argument)
        : base(span)
    {
        Argument = AsChild(argument);
    }

    public override Doc PrintNode(PrintContext ctx)
    {
        return Doc.Concat("return", Argument.IsEmpty ? "" : " ", Argument.Print(ctx));
    }
}
