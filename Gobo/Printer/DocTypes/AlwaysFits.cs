namespace Gobo.Printer.DocTypes;

public class AlwaysFits : Doc
{
    public readonly Doc Contents;

    public AlwaysFits(Doc printedTrivia)
    {
        Contents = printedTrivia;
    }
}
