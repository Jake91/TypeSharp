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
            foreach (var reference in module.References.Cast<TsModuleReferenceSpecific>().OrderBy(x => x.Module.GetModuleImport(rootElement))) // only supported ref so far.
            {
                builder.Append(GenerateReference(rootElement, reference));
                builder.AppendLine();
            }
            foreach (var moduleType in module.Types)
            {
                builder.Append(GenerateContent(moduleType, string.Empty));
                builder.AppendLine();
            }
            return new TsFile(builder.ToString(), module.ModuleName, module.ModulePath, module.GeneratesJavascript ? TsFileType.TypeScript : TsFileType.Definition);
        }

        private static string GenerateReference(string rootElement, TsModuleReferenceSpecific reference)
        {
            var builder = new StringBuilder();
            builder.Append("import { ");
            builder.Append(string.Join(", ", reference.Types.Select(x => x.Name).OrderBy(x => x)));
            builder.Append(" } from \"");
            builder.Append(reference.Module.GetModuleImport(rootElement) + "\";");
            return builder.ToString(); // todo fix builders?
        }

        private string GenerateContent(TsType type, string indententionString)
        {
            var builder = new StringBuilder();
            builder.Append(indententionString);
            if (type.IsExport)
            {
                builder.Append("export ");
            }

            if (type.IsInterface)
            {
                builder.Append("interface ");
            }
            else if (type.IsEnum)
            {
                throw new NotImplementedException();
                //builder.Append("enum "); // todo jl
            }
            else
            {
                throw new NotImplementedException();
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
            return builder.ToString();
        }

        private string GenerateContent(TsProperty property)
        {
            if (property.PropertyType is TsDefaultType defaultType)
            {
                return $"{property.Name}: {Convert(defaultType.DefaultType)};";
            }

            return $"{property.Name}: {property.PropertyType.Name};";
        }

        private string Convert(TsDefault tsDefault)
        {
            switch (tsDefault)
            {
                case TsDefault.Number:
                    return "number";
                case TsDefault.Boolean:
                    return "boolean";
                case TsDefault.Date:
                    return "Date";
                case TsDefault.String:
                    return "string";
                default:
                    throw new ArgumentOutOfRangeException(nameof(tsDefault), tsDefault, null);
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
