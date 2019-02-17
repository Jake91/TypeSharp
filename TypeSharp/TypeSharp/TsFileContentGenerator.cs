using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace TypeSharp
{
    public class TsFileContentGenerator
    {
        public TsFile Generate(string rootElement, TsModule module)
        {
            var builder = new StringBuilder();
            foreach (var reference in module.References.OrderBy(x => x.Module.GetModuleImport(rootElement)))
            {
                AddReferences(builder, rootElement, reference);
                builder.AppendLine();
            }
            foreach (var type in module.Types)
            {
                switch (type)
                {
                    case TsEnum tsEnum:
                        AddContent(builder, string.Empty, tsEnum);
                        break;
                    case TsClass tsClass:
                        AddContent(builder, string.Empty, tsClass);
                        break;
                    case TsInterface tsInterface:
                        AddContent(builder, string.Empty, tsInterface);
                        break;
                    default:
                        throw new ArgumentException($"Could not generate content for type ({type.Name})");
                }
                builder.AppendLine();
            }
            return new TsFile(builder.ToString(), module.ModuleName, module.ModulePath, module.GeneratesJavascript ? TsFileType.TypeScript : TsFileType.Definition);
        }

        private static void AddReferences(StringBuilder builder, string rootElement, TsModuleReference reference)
        {
            builder.Append("import { ");
            builder.Append(string.Join(", ", reference.Types.Select(x => x.Name).OrderBy(x => x)));
            builder.Append(" } from \"");
            builder.Append(reference.Module.GetModuleImport(rootElement) + "\";");
        }

        private static void AddContent(StringBuilder builder, string indententionString, TsClass type)
        {
            builder.Append(indententionString);
            if (type.IsExport)
            {
                builder.Append("export ");
            }
            builder.Append("class ");
            builder.Append(type.Name);

            if (type.IsGeneric)
            {
                builder.Append("<" + string.Join(", ", type.GenericArguments.Select(x => x.Name) + ">"));
            }

            if (type.BaseType != null)
            {
                builder.Append($" extends {GetGenericContent(type.BaseType)}"); // todo
            }
            builder.Append(" {");
            builder.AppendLine();
            foreach (var property in type.Properties)
            {
                builder.Append($"{indententionString}\t");
                builder.Append(GenerateContent(property));
                builder.AppendLine();
            }
            builder.Append(indententionString + "}");
        }

        private static string GetGenericContent(TsTypeBase reference)
        {
            switch (reference)
            {
                case TsGenericArgument genericArgument:// TResult
                    return genericArgument.Name; 
                case TsGenericTypeReference tsGenericTypeReference: // ClassX<int, ClassY<string>>
                    return tsGenericTypeReference.Type.Name + "<" + string.Join(", ",
                               tsGenericTypeReference.GenericArguments.Select(GetGenericContent)) + ">"; 
                default:
                    return reference.Name;
            }
        }

        private static void AddContent(StringBuilder builder, string indententionString, TsInterface type)
        {
            builder.Append(indententionString);
            if (type.IsExport)
            {
                builder.Append("export ");
            }
            builder.Append("interface ");
            builder.Append(type.Name);

            if (type.IsGeneric)
            {
                builder.Append("<" + string.Join(", ", type.GenericArguments.Select(x => x.Name)) + ">");
            }

            if (type.BaseType != null)
            {
                builder.Append($" extends {GetGenericContent(type.BaseType)}");
            }
            builder.Append(" {");
            builder.AppendLine();
            foreach (var property in type.Properties)
            {
                builder.Append($"{indententionString}\t");
                builder.Append(GenerateContent(property));
                builder.AppendLine();
            }
            builder.Append(indententionString + "}");
        }

        private static void AddContent(StringBuilder builder, string indententionString, TsEnum type)
        {
            builder.Append(indententionString);
            if (type.IsExport)
            {
                builder.Append("export ");
            }
            builder.Append($"enum {type.Name} " + "{");
            builder.AppendLine();
            builder.Append(string.Join($",{Environment.NewLine}", type.Values.Select(x => $"{indententionString}\t{GenerateContent(x)}")));
            builder.AppendLine();
            builder.Append(indententionString + "}");
        }

        private static string GenerateContent(EnumValue enumValue)
        {
            return $"{enumValue.Name} = {enumValue.Value}";
        }

        private static string GenerateContent(TsInterfaceProperty interfaceProperty)
        {
            return $"{interfaceProperty.Name}: {GetGenericContent(interfaceProperty.PropertyType)};";
        }

        private static string GenerateContent(TsClassProperty classProperty)
        {
            return $"{Convert(classProperty.AccessModifier)}{classProperty.Name}: {GetGenericContent(classProperty.PropertyType)};";
        }

        private static string Convert(TsAccessModifier accessModifier)
        {
            switch (accessModifier)
            {
                case TsAccessModifier.None:
                    return string.Empty;
                case TsAccessModifier.Private:
                    return "private ";
                case TsAccessModifier.Protected:
                    return "protected ";
                case TsAccessModifier.Public:
                    return "public ";
                default:
                    throw new ArgumentOutOfRangeException(nameof(accessModifier), accessModifier, null);
            }
        }
    }

    public class TsFile
    {
        public string Content { get; }

        private readonly string fileName;

        private readonly IReadOnlyCollection<string> filePaths;
        private readonly TsFileType fileType;


        public TsFile(string content, string fileName, IReadOnlyCollection<string> filePaths, TsFileType fileType)
        {
            Content = content;
            this.fileName = fileName;
            this.filePaths = filePaths;
            this.fileType = fileType;
        }

        public string GetFilePath(string outPutFolder) // todo naming?
        {
            return Path.Combine(outPutFolder, $@"{string.Join(Path.DirectorySeparatorChar.ToString(), filePaths)}{Path.DirectorySeparatorChar}{fileName}.{FileType(fileType)}");
        }

        private static string FileType(TsFileType tsFileType)
        {
            switch (tsFileType)
            {
                case TsFileType.TypeScript:
                    return "ts";
                case TsFileType.Definition:
                    return "d.ts";
                default:
                    throw new ArgumentOutOfRangeException(nameof(tsFileType), tsFileType, null);
            }
        }
    }

    public enum TsFileType
    {
        TypeScript,
        Definition
    }
}
