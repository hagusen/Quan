using Gobo.Printer.DocTypes;

namespace Gobo.SyntaxNodes.Gml;

public sealed class ContinueStatement : GmlSyntaxNode
{
    public ContinueStatement(TextSpan span)
        : base(span) { }

    public override Doc PrintNode(PrintContext ctx)
    {
        return "continue";
    }
}
