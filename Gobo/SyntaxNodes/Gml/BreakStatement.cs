using Gobo.Printer.DocTypes;

namespace Gobo.SyntaxNodes.Gml;

public sealed class BreakStatement : GmlSyntaxNode
{
    public BreakStatement(TextSpan span)
        : base(span) { }

    public override Doc PrintNode(PrintContext ctx)
    {
        return "break";
    }
}
