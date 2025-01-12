using Gobo.Printer.DocTypes;

namespace Gobo.SyntaxNodes;

public class EmptyNode : GmlSyntaxNode
{
    public static EmptyNode Instance { get; } = new();

    public override Doc PrintNode(PrintContext ctx)
    {
        return Doc.Null;
    }
}
