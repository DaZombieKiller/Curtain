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
                var hierarchy = locator.ClassDescriptor.BaseClassArray.ClassDescriptors;
                var baseClass = (RttiBaseClassDescriptor?)null;

                for (int i = 1; i < hierarchy.Length; i++)
                {
                    if (hierarchy[i].MDisplacement == locator.Offset)
                    {
                        baseClass = hierarchy[i];
                        break;
                    }
                }

                writer.Write($"// {locator.Offset:X4}");

                if (baseClass != null)
                    writer.WriteLine($" ({DecoratedName.UnDecorate(baseClass.TypeDescriptor.Name)})");
                else
                    writer.WriteLine();

                writer.Write(DecoratedName.UnDecorate(type.Name));

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
