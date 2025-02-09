using System.Text;

namespace Gobo.Printer.DocPrinter;

public class Indent
{
    public string Value = string.Empty;
    public int Length;
    public IList<IIndentType>? TypesForTabs;
}

public interface IIndentType { }

public class IndentType : IIndentType
{
    protected IndentType() { }

    public static IndentType Instance = new();
}

public class AlignType : IIndentType
{
    public int Width { get; init; }
}

public class Indenter
{
    protected readonly DocPrinterOptions PrinterOptions;

    public Indenter(DocPrinterOptions printerOptions)
    {
        PrinterOptions = printerOptions;
    }

    public static Indent GenerateRoot()
    {
        return new();
    }

    public Indent IncreaseIndent(Indent indent)
    {
        if (PrinterOptions.UseTabs)
        {
            if (indent.TypesForTabs != null)
            {
                return MakeIndentWithTypesForTabs(indent, IndentType.Instance);
            }

            return new Indent
            {
                Value = indent.Value + "\t",
                Length = indent.Length + PrinterOptions.TabWidth
            };
        }
        else
        {
            return new Indent
            {
                Value = indent.Value + new string(' ', PrinterOptions.TabWidth),
                Length = indent.Length + PrinterOptions.TabWidth
            };
        }
    }

    public Indent AddAlign(Indent indent, int alignment)
    {
        if (PrinterOptions.UseTabs)
        {
            return MakeIndentWithTypesForTabs(indent, new AlignType { Width = alignment });
        }
        else
        {
            return new Indent
            {
                Value = indent.Value + new string(' ', alignment),
                Length = indent.Length + alignment
            };
        }
    }

    // when using tabs we need to sometimes replace the spaces from align with tabs
    // trailing aligns stay as spaces, but any aligns before a tab get converted to a single tab
    // see https://github.com/prettier/prettier/blob/main/commands.md#align
    private Indent MakeIndentWithTypesForTabs(Indent indent, IIndentType nextIndentType)
    {
        List<IIndentType> types;

        // if it doesn't exist yet, then all values on it are regular indents, not aligns
        if (indent.TypesForTabs == null)
        {
            types = new List<IIndentType>();
            for (var x = 0; x < indent.Value.Length; x++)
            {
                types.Add(IndentType.Instance);
            }

            types.Add(nextIndentType);
        }
        else
        {
            var placeTab = false;
            types = new List<IIndentType>(indent.TypesForTabs) { nextIndentType };
            for (var x = types.Count - 1; x >= 0; x--)
            {
                if (types[x] == IndentType.Instance)
                {
                    placeTab = true;
                }

                if (placeTab)
                {
                    types[x] = IndentType.Instance;
                }
            }

            // merge back to back aligns into tabs
            for (var x = 0; x < types.Count - 1; x++)
            {
                if (types[x] is AlignType && types[x + 1] is AlignType)
                {
                    types[x] = IndentType.Instance;
                    types.RemoveAt(x + 1);
                    x -= 1;
                }
            }
        }

        var length = 0;
        var value = new StringBuilder();
        foreach (var indentType in types)
        {
            if (indentType is AlignType alignType)
            {
                value.Append(' ', alignType.Width);
                length += alignType.Width;
            }
            else
            {
                value.Append('\t');
                length += PrinterOptions.TabWidth;
            }
        }

        return new Indent
        {
            Length = length,
            Value = value.ToString(),
            TypesForTabs = types
        };
    }
}
