using System.Collections.Generic;
using System.Linq;
using TypeSharp.TsModel.Types;

namespace TypeSharp.TsModel.Modules
{
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
