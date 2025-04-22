using Curtain.Rtti;
using System.CodeDom.Compiler;

namespace Curtain.RttiDumper;

internal static class Program
{
    private static void Main(string[] args)
    {
        var module = new RttiModule(args[0]);
        var writer = new IndentedTextWriter(Console.Out);

        foreach (RttiTypeDescriptor type in module.EnumerateObjects<RttiTypeDescriptor>())
        {
            foreach (RttiCompleteObjectLocator locator in type.CompleteObjectLocators)
            {
                writer.WriteLine($"// {locator.Offset:X4}");
                writer.Write(DecoratedName.UnDecorate(type.Name));
                var hierarchy = locator.ClassDescriptor.BaseClassArray.ClassDescriptors;

                for (int i = 1; i < hierarchy.Length; i += 1 + (int)hierarchy[i].BaseCount)
                {
                    if (i == 1)
                        writer.Write(" : ");
                    else
                        writer.Write(", ");

                    writer.Write(DecoratedName.UnDecorate(hierarchy[i].TypeDescriptor.Name, UnDecorateFlags.NoUdtPrefix));
                }

                writer.WriteLine();
                writer.WriteLine('{');

                using (writer.Indent())
                {
                    foreach (var method in locator.VTable.FunctionPointers)
                    {
                        writer.WriteLine($"// {method:X16}");
                    }
                }

                writer.WriteLine("};");
                writer.WriteLine();
            }
        }
    }
}
