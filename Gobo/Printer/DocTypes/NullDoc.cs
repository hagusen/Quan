namespace Gobo.Printer.DocTypes;

public class NullDoc : Doc
{
    public static NullDoc Instance { get; } = new();

    private NullDoc() { }
}
