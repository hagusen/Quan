namespace Gobo.Printer.DocTypes;

public class LiteralLine : LineDoc, IBreakParent
{
    public LiteralLine()
    {
        Type = LineType.Hard;
        IsLiteral = true;
    }
}
