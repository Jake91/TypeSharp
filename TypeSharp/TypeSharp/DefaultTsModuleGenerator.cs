﻿using System;
using System.Collections.Generic;
using System.Linq;

namespace TypeSharp
{
    public class DefaultTsModuleGenerator
    {
        public TsModule Generate(TsTypeBase type)
        {
            return Generate(new List<TsTypeBase>() { type }).Single();
        }

        public IList<TsModule> Generate(ICollection<TsTypeBase> types)
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
                    modulesForNamespace[type.CSharpType.Namespace] = new TsModule(name, path, new List<TsModuleReference>(), new List<TsTypeBase>() { type });
                }
            }

            foreach (var tsModule in modulesForNamespace.Values)
            {
                foreach (var type in tsModule.Types.OfType<TsInterface>())
                {
                    tsModule.References.AddRange(GetReferences(type, modulesForNamespace));
                }
                foreach (var type in tsModule.Types.OfType<TsClass>())
                {
                    tsModule.References.AddRange(GetReferences(type, modulesForNamespace));
                }
            }
            return modulesForNamespace.Select(x => x.Value).ToList();
        }

        private static IList<TsModuleReference> GetReferences(TsInterface tsType, IReadOnlyDictionary<string, TsModule> modulesForNamespace)
        {
            return GetReferences(tsType.CSharpType.Namespace, tsType.Properties.Select(x => x.PropertyType), tsType.BaseType, modulesForNamespace);
        }

        private static IList<TsModuleReference> GetReferences(TsClass tsType, IReadOnlyDictionary<string, TsModule> modulesForNamespace)
        {
            return GetReferences(tsType.CSharpType.Namespace, tsType.Properties.Select(x => x.PropertyType), tsType.BaseType, modulesForNamespace);
        }

        private static IList<TsModuleReference> GetReferences(string @namespace, IEnumerable<TsTypeBase> propertyTypes, TsTypeBase baseType, IReadOnlyDictionary<string, TsModule> modulesForNamespace)
        {
            var referenceDict = new Dictionary<TsModule, List<TsTypeBase>>();
            var ownModule = modulesForNamespace[@namespace];

            // todo look at generics
            var nonDefaultPropertyTypes = propertyTypes.Where(x => !(x is TsDefaultType)).ToList();
            if (baseType != null)
            {
                nonDefaultPropertyTypes.Add(baseType);
            }
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
                        referenceDict[module] = new List<TsTypeBase> { propertyType };
                    }
                }
                else
                {
                    throw new ArgumentException($"Property type ({propertyType.CSharpType.Name}) are not part of the type set");
                }
            }
            return referenceDict.Select(kvp => new TsModuleReference(kvp.Key, kvp.Value)).ToList();
        }
    }

    public sealed class TsModuleReference
    {
        public TsModule Module { get; }
        public IList<TsTypeBase> Types { get; }

        public TsModuleReference(TsModule module, IList<TsTypeBase> types)
        {
            Module = module;
            Types = types;
        }
    }

    public sealed class TsModule
    {
        public readonly string ModuleName; // SimpleClasses
        public readonly IReadOnlyCollection<string> ModulePath; // { "TypeSharp","Tests", "TestData" }
        public ICollection<TsModuleReference> References { get; set; }
        public ICollection<TsTypeBase> Types { get; set; }


        public TsModule(string moduleName, IReadOnlyCollection<string> modulePath, ICollection<TsModuleReference> references, ICollection<TsTypeBase> types)
        {
            ModuleName = moduleName;
            ModulePath = modulePath;
            References = references;
            Types = types;
        }

        public bool GeneratesJavascript => Types.Any(x => !(x is TsInterface));

        public string GetModuleImport(string rootElement) // todo naming?
        {
            return $@"{rootElement}/{string.Join("/", ModulePath)}/{ModuleName}";
        }
    }
}
