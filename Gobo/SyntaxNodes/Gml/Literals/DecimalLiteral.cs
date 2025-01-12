﻿using Gobo.Printer.DocTypes;

namespace Gobo.SyntaxNodes.Gml.Literals
{
    public sealed class DecimalLiteral : Literal
    {
        public DecimalLiteral(TextSpan span, string text)
            : base(span, text) { }

        public override Doc PrintNode(PrintContext ctx)
        {
            var trimmed = Text.TrimStart('0');

            if (trimmed[0] == '.')
            {
                trimmed = "0" + trimmed;
            }

            if (trimmed.EndsWith('.'))
            {
                trimmed += "0";
            }

            return trimmed;
        }

        public override int GetHashCode()
        {
            return Text.Trim('0').GetHashCode();
        }
    }
}
