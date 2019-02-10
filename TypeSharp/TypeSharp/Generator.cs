using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;

namespace TypeSharp
{
    //public class Module
    //{
    //    public string GetFilePath(string outPutFolder)
    //    {
    //        return $@"{outPutFolder}{filePath}";
    //    }

    //    private readonly string filePath;
    //    public string FileContent { get; private set; }

    //    public Module(string filePath, string fileContent)
    //    {
    //        this.filePath = filePath;
    //        FileContent = fileContent;
    //    }
    //}

    public class Generator
    {
        public IList<Module> Convert(ICollection<TsModule> modules)
        {
            var typeDict = modules.SelectMany(x => x.Types).ToDictionary(x => x, x => new TsType(x, x.Name, true, true, x.IsEnum, new List<TsProperty>()));
            var moduleDict = modules.ToDictionary(x => x, x => new Module(x.FilePath, new List<Module>(), x.Types.Select(y => typeDict[y]).ToList()));
            

            foreach (var moduleKvp in moduleDict)
            {
                foreach (var typeKvp in moduleKvp.Value.Types)
                {
                    PopulateProperties(typeKvp, typeDict);
                }

                foreach (var referenceKvp in moduleKvp.Key.References)
                {
                    moduleKvp.Value.References.Add(moduleDict[referenceKvp]);
                }
            }
            return moduleDict.Select(x => x.Value).ToList();
        }

        private void PopulateProperties(TsType type, IReadOnlyDictionary<Type, TsType> typeDict)
        {
            var properties = type.CSharpType.GetProperties(BindingFlags.Instance | BindingFlags.Public); // todo improve
            foreach (var propertyInfo in properties)
            {
                if (IsDefaultType(propertyInfo.PropertyType))
                {
                    type.Properties.Add(
                        new TsProperty(
                            propertyInfo.Name, 
                            new TsDefaultType(
                                propertyInfo.PropertyType, 
                                propertyInfo.PropertyType.Name, 
                                GetDefaultType(propertyInfo.PropertyType)), 
                            TsAccessModifier.Public, 
                            false, 
                            false));
                }
                else if(typeDict.TryGetValue(propertyInfo.PropertyType, out var tsType))
                {
                    type.Properties.Add(new TsProperty(propertyInfo.Name, tsType, TsAccessModifier.Public, false, false));
                }
                else
                {
                    throw new ArgumentException("Type is not a default type, and is not declared within any of the modules");
                }
            }
        }


        private TsDefault GetDefaultType(Type type)
        {
            if (type == typeof(bool))
            {
                return TsDefault.Boolean;
            }
            else if (type == typeof(DateTime) ||
                     type == typeof(DateTimeOffset))
            {
                return TsDefault.Date;
            }
            else if (type == typeof(string))
            {
                return TsDefault.String;
            }
            else if (type == typeof(long) ||
                     type == typeof(int) ||
                     type == typeof(decimal) ||
                     type == typeof(double))
            {
                return TsDefault.Number;
            }
            throw new ArgumentException("Type is not a default ts type");
        }

        private bool IsDefaultType(Type type)
        {
            return
                type == typeof(bool) ||
                type == typeof(string) ||
                type == typeof(DateTime) ||
                type == typeof(DateTimeOffset) ||
                type == typeof(long) ||
                type == typeof(int) ||
                type == typeof(decimal) ||
                type == typeof(double);
        }

        public IList<TsModule> StructureModules(ICollection<Type> types)
        {
            var modulesForNamespace = new Dictionary<string, TsModule>();
            var namespaceForType = new Dictionary<Type, string>();
            foreach (var type in types)
            {
                if (string.IsNullOrEmpty(type.Namespace))
                {
                    throw new ArgumentException("Must have a namespace");
                }

                if(modulesForNamespace.TryGetValue(type.Namespace, out var typeList))
                {
                    typeList.Types.Add(type);
                }
                else
                {
                    modulesForNamespace[type.Namespace] = new TsModule($@"\{type.Namespace.Replace('.', '\\')}", new List<TsModule>(), new List<Type>() { type });
                }
                namespaceForType[type] = type.Namespace;
            }

            foreach (var tsModule in modulesForNamespace)
            {
                foreach (var type in tsModule.Value.Types)
                {
                    foreach(var reference in GetReferences(type, namespaceForType, modulesForNamespace))
                    {
                        if (reference != tsModule.Value) // no need to reference itself
                        {
                            tsModule.Value.References.Add(reference);
                        }
                    }
                }
                
            }
            return modulesForNamespace.Select(x => x.Value).ToList();
        }

        private IList<TsModule> GetReferences(Type type, IReadOnlyDictionary<Type, string> namespaceForType, IReadOnlyDictionary<string, TsModule> modulesForNamespace)
        {
            var references = new List<TsModule>();
            var types = GetTypesFromType(type);
            foreach (var t in types)
            {
                if (namespaceForType.TryGetValue(t, out var @namespace))
                {
                    if(modulesForNamespace.TryGetValue(@namespace, out var module))
                    {
                        references.Add(module);
                    }
                }
            }
            return references;
        }

        private IList<Type> GetTypesFromType(Type type)
        {
            return type.GetProperties(BindingFlags.Public | BindingFlags.Instance).Select(x => x.PropertyType).ToList(); // todo only look at properties on first level. No generic, no subtypes
        }
    }

    public class TsModule
    {
        public readonly string FilePath; // "TypeSharp\Tests\TestData\SimpleClasses"
        public ICollection<TsModule> References { get; }
        public ICollection<Type> Types { get; }

        public TsModule(string filePath, ICollection<TsModule> references, ICollection<Type> types)
        {
            FilePath = filePath;
            References = references;
            Types = types;
        }

    }

    public class ContentGenerator
    {
        public string GenerateContent(Module module)
        {
            var builder = new StringBuilder();
            foreach (var reference in module.References)
            {
                builder.Append(GenerateReference(reference));
                builder.AppendLine();
            }
            foreach (var moduleType in module.Types)
            {
                builder.Append(GenerateContent(moduleType, string.Empty));
                builder.AppendLine();
            }
            return builder.ToString();
        }

        public string GenerateReference(Module module)
        {
            var builder = new StringBuilder();
            return builder.ToString(); // todo jl
        }

        public string GenerateContent(TsType type, string indententionString)
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

        public string GenerateContent(TsProperty property)
        {
            if (property.PropertyType is TsDefaultType defaultType)
            {
                return $"{property.Name}: {Convert(defaultType.DefaultType)}";
            }

            return $"{property.Name}: {property.PropertyType.Name}";
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

    public class Module
    {
        private readonly string filePath; // "\TypeSharp\Tests\TestData\SimpleClasses"
        public ICollection<Module> References { get; set; }
        public ICollection<TsType> Types { get; set; }
        

        public Module(string filePath, ICollection<Module> references, ICollection<TsType> types)
        {
            this.filePath = filePath;
            References = references;
            Types = types;
        }

        private bool GeneratesJavascript => Types.Any(x => !x.IsInterface);

        private string FileType()
        {
            return GeneratesJavascript ? ".ts" : "d.ts";
        }

        public string GetFilePath(string outPutFolder)
        {
            return $@"{outPutFolder}{filePath}{FileType()}";
        }
    }

    public class TsType
    {
        public Type CSharpType { get; set; }
        public string Name { get; set; }
        public bool IsExport { get; set; }
        public bool IsInterface { get; set; }
        //public bool IsClass { get; set; }
        //public bool IsAbstract { get; set; }
        public bool IsEnum { get; set; }
        //public bool IsPrimitive { get; set; }
        public ICollection<TsProperty> Properties { get; set; }

        public TsType(Type cSharpType, string name, bool isExport, bool isInterface, bool isEnum, ICollection<TsProperty> properties)
        {
            CSharpType = cSharpType;
            Name = name;
            IsExport = isExport;
            IsInterface = isInterface;
            IsEnum = isEnum;
            Properties = properties;
        }
    }

    public enum TsDefault
    {
        Number,
        Boolean,
        Date,
        String
    }

    public class TsDefaultType : TsType
    {
        public TsDefault DefaultType { get; }

        public TsDefaultType(Type cSharpType, string name, TsDefault defaultType) : base(cSharpType, name, false, false, false, new List<TsProperty>())
        {
            DefaultType = defaultType;
        }
    }

    public class TsProperty
    {
        public string Name { get; set; }
        public TsType PropertyType { get; set; }
        public TsAccessModifier AccessModifier{ get; set; }
        public bool HasGetter { get; set; }
        public bool HasSetter { get; set; }

        public TsProperty(string name, TsType propertyType, TsAccessModifier accessModifier, bool hasGetter, bool hasSetter)
        {
            Name = name;
            PropertyType = propertyType;
            AccessModifier = accessModifier;
            HasGetter = hasGetter;
            HasSetter = hasSetter;
        }
    }

    public enum TsAccessModifier
    {
        Private = 1,
        Protected = 2,
        Public = 3
    }
}
