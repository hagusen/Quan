﻿using Gobo.Printer.DocTypes;

namespace Gobo.SyntaxNodes.Gml;

public sealed class ConstructorClause : GmlSyntaxNode
{
    public GmlSyntaxNode Id { get; set; }
    public GmlSyntaxNode Arguments { get; set; }

    public ConstructorClause(TextSpan span, GmlSyntaxNode id, GmlSyntaxNode arguments)
        : base(span)
    {
        Id = AsChild(id);
        Arguments = AsChild(arguments);
    }

    public override Doc PrintNode(PrintContext ctx)
    {
        if (!Id.IsEmpty)
        {
            return Doc.Concat(" : ", Id.Print(ctx), Arguments.Print(ctx), " ", "constructor");
        }
        else
        {
            return Doc.Concat(" ", "constructor");
        }
    }
}
