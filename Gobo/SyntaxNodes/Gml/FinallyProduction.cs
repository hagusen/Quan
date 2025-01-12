﻿using Gobo.Printer.DocTypes;
using Gobo.SyntaxNodes.PrintHelpers;

namespace Gobo.SyntaxNodes.Gml;

public sealed class FinallyProduction : GmlSyntaxNode
{
    public GmlSyntaxNode Body { get; set; }

    public FinallyProduction(TextSpan span, GmlSyntaxNode body)
        : base(span)
    {
        Body = AsChild(body);
    }

    public override Doc PrintNode(PrintContext ctx)
    {
        return Doc.Concat("finally", " ", Statement.EnsureStatementInBlock(ctx, Body));
    }
}
