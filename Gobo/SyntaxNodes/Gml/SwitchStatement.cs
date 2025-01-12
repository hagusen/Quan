﻿using Gobo.Printer.DocTypes;
using Gobo.SyntaxNodes.PrintHelpers;

namespace Gobo.SyntaxNodes.Gml;

public sealed class SwitchStatement : GmlSyntaxNode
{
    public GmlSyntaxNode Discriminant { get; set; }
    public GmlSyntaxNode Cases { get; set; }

    public SwitchStatement(TextSpan span, GmlSyntaxNode discriminant, GmlSyntaxNode cases)
        : base(span)
    {
        Discriminant = AsChild(discriminant);
        Cases = AsChild(cases);
    }

    public override Doc PrintNode(PrintContext ctx)
    {
        return Statement.PrintControlFlowStatement(ctx, "switch", Discriminant, Cases);
    }
}
