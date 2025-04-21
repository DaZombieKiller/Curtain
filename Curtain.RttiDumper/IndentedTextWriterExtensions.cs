using System.CodeDom.Compiler;

namespace Curtain.RttiDumper;

internal static class IndentedTextWriterExtensions
{
    public static IndentScope Indent(this IndentedTextWriter writer, int indent = 1)
    {
        return new IndentScope(writer, indent);
    }
}
