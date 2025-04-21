using System.CodeDom.Compiler;

namespace Curtain.RttiDumper;

internal readonly struct IndentScope : IDisposable
{
    private readonly int _indent;

    private readonly IndentedTextWriter _writer;

    public IndentScope(IndentedTextWriter writer, int indent)
    {
        _indent = writer.Indent;
        _writer = writer;
        writer.Indent = indent;
    }

    public void Dispose()
    {
        _writer.Indent = _indent;
    }
}
