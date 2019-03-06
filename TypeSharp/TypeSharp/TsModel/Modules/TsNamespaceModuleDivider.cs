using System;
using System.Linq;
using TypeSharp.TsModel.Types;

namespace TypeSharp.TsModel.Modules
{
    public class TsNamespaceModuleDivider : IModuleDivider
    {
        public TsModuleLocation GetLocationAndName(TsTypeDefinitionBase tsType)
        {
            if (string.IsNullOrEmpty(tsType.CSharpType.Namespace))
            {
                throw new ArgumentException($"Type ({tsType.CSharpType.Name}) is missing a namespace");
            }

            var namespaceArray = tsType.CSharpType.Namespace.Split('.');
            var name = namespaceArray[namespaceArray.Length - 1];
            var path = namespaceArray.Take(namespaceArray.Length - 1).ToList();
            return new TsModuleLocation(name, path);
        }
    }
}