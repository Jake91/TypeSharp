using System;
using System.Collections.Generic;
using System.Linq;

namespace TypeSharp
{
    public class DefaultTsModuleGenerator
    {
        public TsModule Generate(TsType type)
        {
            return Generate(new List<TsType>() { type }).Single();
        }

        public IList<TsModule> Generate(ICollection<TsType> types)
        {
            var modulesForNamespace = new Dictionary<string, TsModule>();
            foreach (var type in types)
            {
                if (string.IsNullOrEmpty(type.CSharpType.Namespace))
                {
                    throw new ArgumentException($"Type ({type.CSharpType.Name}) is missing a namespace");
                }

                if (modulesForNamespace.TryGetValue(type.CSharpType.Namespace, out var typeList))
                {
                    typeList.Types.Add(type);
                }
                else
                {
                    var namespaceArray = type.CSharpType.Namespace.Split('.');
                    var name = namespaceArray[namespaceArray.Length - 1];
                    var path = namespaceArray.Take(namespaceArray.Length - 1).ToList();
                    modulesForNamespace[type.CSharpType.Namespace] = new TsModule(name, path, new List<TsModuleReferenceBase>(), new List<TsType>() { type });
                }
            }

            foreach (var tsModule in modulesForNamespace.Values)
            {
                foreach (var type in tsModule.Types)
                {
                    tsModule.References.AddRange(GetReferences(type, modulesForNamespace));
                }
            }
            return modulesForNamespace.Select(x => x.Value).ToList();
        }
        private static IList<TsModuleReferenceBase> GetReferences(TsType tsType, IReadOnlyDictionary<string, TsModule> modulesForNamespace)
        {
            if (tsType is TsDefaultType)
            {
                return new List<TsModuleReferenceBase>();
            }
            var referenceDict = new Dictionary<TsModule, List<TsType>>();
            var ownModule = modulesForNamespace[tsType.CSharpType.Namespace];

            // todo look att base classes and generics
            var nonDefaultPropertyTypes = tsType.Properties.Select(x => x.PropertyType).Where(x => !(x is TsDefaultType)).ToList();
            foreach (var propertyType in nonDefaultPropertyTypes)
            {
                // ReSharper disable once AssignNullToNotNullAttribute Already check that namespace is correct when creating TsModule
                if (modulesForNamespace.TryGetValue(propertyType.CSharpType.Namespace, out var module))
                {
                    if (module == ownModule) // no need to reference itself
                    {
                        continue;
                    }
                    if (referenceDict.TryGetValue(module, out var types))
                    {
                        types.Add(propertyType);
                    }
                    else
                    {
                        referenceDict[module] = new List<TsType>() { propertyType };
                    }
                }
                else
                {
                    throw new ArgumentException($"Property type ({propertyType.CSharpType.Name}) are not part of the type set");
                }
            }
            return referenceDict.Select(kvp => new TsModuleReferenceSpecific(kvp.Key, kvp.Value)).Cast<TsModuleReferenceBase>().ToList();
        }
    }

    public abstract class TsModuleReferenceBase
    {
        public TsModule Module { get; set; }

        protected TsModuleReferenceBase(TsModule module)
        {
            Module = module;
        }
    }

    public class TsModuleReferenceSpecific : TsModuleReferenceBase
    {
        public IList<TsType> Types { get; }

        public TsModuleReferenceSpecific(TsModule module, IList<TsType> types) : base(module)
        {
            Types = types;
        }
    }

    //public class TsModuleReferenceAll : TsModuleReferenceBase
    //{
    //    public string AsVariable { get; }

    //    public TsModuleReferenceAll(TsModule module, string asVariable) : base(module)
    //    {
    //        AsVariable = asVariable;
    //    }
    //}

    public class TsModule
    {
        public readonly string ModuleName; // SimpleClasses
        public readonly IReadOnlyCollection<string> ModulePath; // { "TypeSharp","Tests", "TestData" }
        public ICollection<TsModuleReferenceBase> References { get; set; }
        public ICollection<TsType> Types { get; set; }


        public TsModule(string moduleName, IReadOnlyCollection<string> modulePath, ICollection<TsModuleReferenceBase> references, ICollection<TsType> types)
        {
            ModuleName = moduleName;
            ModulePath = modulePath;
            References = references;
            Types = types;
        }

        public bool GeneratesJavascript => Types.Any(x => !x.IsInterface);

        public string GetModuleImport(string rootElement) // todo naming?
        {
            return $@"{rootElement}/{string.Join("/", ModulePath)}/{ModuleName}";
        }
    }
}
