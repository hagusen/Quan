namespace Gobo.Printer.DocTypes;

public class ForceFlat : Doc, IHasContents
{
    public Doc Contents { get; set; } = Null;
}
