using Gobo.Text;

namespace Gobo;

public class PrintContext
{
    public FormatOptions Options { get; init; }
    public SourceText SourceText { get; init; }

    public PrintContext(FormatOptions options, SourceText sourceText)
    {
        Options = options;
        SourceText = sourceText;
    }
}
