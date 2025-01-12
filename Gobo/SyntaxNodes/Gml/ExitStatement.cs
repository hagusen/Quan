using Gobo.Printer.DocTypes;

namespace Gobo.SyntaxNodes.Gml;

public sealed class ExitStatement : GmlSyntaxNode
{
    public ExitStatement(TextSpan span)
        : base(span) { }

    public override Doc PrintNode(PrintContext ctx)
    {
        return "exit";
    }
}
