namespace Gobo.Printer.DocTypes;

public class Fill : Doc
{
    public IList<Doc> Contents { get; set; }

    public Fill(IList<Doc> contents)
    {
        Contents = contents;
    }
}
