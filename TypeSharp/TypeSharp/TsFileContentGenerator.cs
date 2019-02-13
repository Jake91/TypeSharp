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
                    case TsTypeWithPropertiesBase tsTypeWithPropertiesBase:
                        AddContent(builder, string.Empty, tsTypeWithPropertiesBase);
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

        private static void AddContent(StringBuilder builder, string indententionString, TsTypeWithPropertiesBase type)
        {
            builder.Append(indententionString);
            if (type.IsExport)
            {
                builder.Append("export ");
            }

            switch (type)
            {
                case TsInterface _:
                    builder.Append("interface ");
                    break;
                case TsClass _:
                    builder.Append("class ");
                    break;
                default:
                    throw new ArgumentException($"Can not generate content for type ({type.Name})");
            }

            builder.Append(type.Name + " {");
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

        private static string GenerateContent(TsProperty property)
        {
            if (property.PropertyType is TsDefaultType defaultType)
            {
                return $"{property.Name}: {Convert(defaultType)};";
            }

            return $"{property.Name}: {property.PropertyType.Name};";
        }

        private static string Convert(TsDefaultType tsDefaultType)
        {
            switch (tsDefaultType)
            {
                case TsBoolean tsBoolean:
                    return tsBoolean.IsObject ? "Boolean" : "boolean";
                case TsDate _:
                    return "Date";
                case TsNumber tsNumber:
                    return tsNumber.IsObject ? "Number" : "number";
                case TsString tsString:
                    return tsString.IsObject ? "String" : "string";
                default:
                    throw new ArgumentException("Could not create content for default type");
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
