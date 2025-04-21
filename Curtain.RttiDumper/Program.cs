using Curtain.Rtti;
using System.CodeDom.Compiler;

namespace Curtain.RttiDumper;

internal static class Program
{
    private static void Main(string[] args)
    {
        var module = new RttiModule(args[0]);
        var writer = new IndentedTextWriter(Console.Out);
        
        foreach (var type in module.EnumerateObjects<RttiCompleteObjectLocator>())
        {
            writer.WriteLine($"// {type.Offset:X4}");
            writer.Write(DecoratedName.UnDecorate(type.TypeDescriptor.Name));
            var hierarchy = type.ClassDescriptor.BaseClassArray.ClassDescriptors;

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
                foreach (var method in type.VTable.FunctionPointers)
                {
                    writer.WriteLine($"// {method:X16}");
                }
            }

            writer.WriteLine("};");
            writer.WriteLine();
        }
    }
}
