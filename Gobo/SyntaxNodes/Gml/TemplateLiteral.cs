using Gobo.Printer.DocTypes;

namespace Gobo.SyntaxNodes.Gml;

public sealed class TemplateLiteral : GmlSyntaxNode
{
    public List<GmlSyntaxNode> Parts => Children;

    public TemplateLiteral(TextSpan span, List<GmlSyntaxNode> atoms)
        : base(span)
    {
        Children = AsChildren(atoms);
    }

    public override Doc PrintNode(PrintContext ctx)
    {
        return Doc.Concat("$\"", Doc.Concat(PrintChildren(ctx)), "\"");
    }
}
