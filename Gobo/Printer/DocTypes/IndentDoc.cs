namespace Gobo.Printer.DocTypes;

public class IndentDoc : Doc, IHasContents
{
    public Doc Contents { get; set; } = Null;
}
