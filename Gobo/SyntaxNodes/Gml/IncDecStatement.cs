﻿using Gobo.Printer.DocTypes;

namespace Gobo.SyntaxNodes.Gml;

public sealed class IncDecStatement : GmlSyntaxNode
{
    public string Operator { get; set; }
    public GmlSyntaxNode Argument { get; set; }
    public bool IsPrefix { get; set; }

    public IncDecStatement(TextSpan span, string @operator, GmlSyntaxNode argument, bool isPrefix)
        : base(span)
    {
        Operator = @operator;
        Argument = AsChild(argument);
        IsPrefix = isPrefix;
    }

    public override Doc PrintNode(PrintContext ctx)
    {
        if (IsPrefix)
        {
            return Doc.Concat(Operator, Argument.Print(ctx));
        }
        else
        {
            return Doc.Concat(Argument.Print(ctx), Operator);
        }
    }
}
