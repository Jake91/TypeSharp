using System;
using System.Collections.Generic;
using System.Linq;
using TypeSharp.Common;
using TypeSharp.TsModel.Modules;
using TypeSharp.TsModel.Types;

namespace TypeSharp.TsGenerators
{
    public class TsModuleGenerator
    {
        private readonly IModuleDivider _moduleDivider;

        public TsModuleGenerator() : this(new TsNamespaceModuleDivider())
        {

        }

        public TsModuleGenerator(IModuleDivider moduleDivider)
        {
            _moduleDivider = moduleDivider;
        }
        
        public TsModule Generate(TsTypeDefinitionBase type)
        {
            return Generate(new List<TsTypeDefinitionBase>() { type }).Single();
        }

        public IList<TsModule> Generate(ICollection<TsTypeDefinitionBase> types)
        {
            var modulesForLocation = new Dictionary<TsModuleLocation, TsModule>();
            foreach (var type in types)
            {
                var moduleLocation = this._moduleDivider.GetLocationAndName(type);

                if (modulesForLocation.TryGetValue(moduleLocation, out var typeList))
                {
                    typeList.Types.Add(type);
                }
                else
                {
                    modulesForLocation[moduleLocation] = new TsModule(moduleLocation, new List<TsModuleImport>(), new List<TsTypeDefinitionBase>() { type });
                }
            }

            foreach (var tsModule in modulesForLocation.Values)
            {
                tsModule.Imports.AddRange(GetReferences(tsModule, modulesForLocation));
            }
            return modulesForLocation.Select(x => x.Value).ToList();
        }

        private IList<TsModuleImport> GetReferences(TsModule module, IReadOnlyDictionary<TsModuleLocation, TsModule> modulesForLocation)
        {
            var referenceDict = new Dictionary<TsModule, List<TsTypeDefinitionBase>>();
            foreach (var type in module.Types)
            {
                foreach (var typeDefinitionReference in GetTypeDefinitionReferences(type).Except(module.Types))
                {
                    var currentModule = modulesForLocation[this._moduleDivider.GetLocationAndName(typeDefinitionReference)];
                    
                    if (referenceDict.TryGetValue(currentModule, out var types))
                    {
                        types.Add(typeDefinitionReference);
                    }
                    else
                    {
                        referenceDict[currentModule] = new List<TsTypeDefinitionBase> { typeDefinitionReference };
                    }
                }
            }
            return referenceDict.Select(kvp => new TsModuleImport(kvp.Key, kvp.Value)).ToList();
        }

        private static IList<TsTypeDefinitionBase> GetTypeDefinitionReferences(TsTypeBase tsType)
        {
            var set = new HashSet<TsTypeDefinitionBase>();
            var stack = new Stack<TsTypeBase>();
            AddReferencesForTsType(stack, tsType);
            while (!stack.IsNullOrEmpty()) // Use while loop instead of recursion for performance reasons
            {
                var current = stack.Pop();
                if (set.Contains(current))
                {
                    // stop circular refs
                    continue;
                }
                AddReferencesForTsType(stack, current);
                if (current is TsTypeDefinitionBase tsTypeDefinition)
                {
                    set.Add(tsTypeDefinition);
                }
            }
            return set.ToList();

        }

        private static void AddReferencesForTsType(Stack<TsTypeBase> stack, TsTypeBase tsType)
        {
            switch (tsType)
            {
                case TsArray tsArray:
                    stack.Push(tsArray.ElementType);
                    return;
                case TsClass tsClass:
                    if (tsClass.BaseType != null)
                    {
                        stack.Push(tsClass.BaseType);
                    }
                    foreach (var propertyType in tsClass.Properties.Select(x => x.PropertyType))
                    {
                        stack.Push(propertyType);
                    }
                    return;
                case TsInterface tsInterface:
                    if (tsInterface.BaseType != null)
                    {
                        stack.Push(tsInterface.BaseType);
                    }
                    foreach (var propertyType in tsInterface.Properties.Select(x => x.PropertyType))
                    {
                        stack.Push(propertyType);
                    }
                    return;
                case TsGenericTypeReference tsGenericTypeReference:
                    foreach (var genericArgument in tsGenericTypeReference.GenericArguments)
                    {
                        stack.Push(genericArgument);
                    }
                    stack.Push(tsGenericTypeReference.Type);
                    return;
                case TsEnum _:
                case TsGenericArgument _:
                case TsBoolean _:
                case TsDate _:
                case TsNumber _:
                case TsString _:
                    return;
                default:
                    throw new ArgumentException($"Type ({tsType.Name}) is missing");
            }
        }
    }
}
