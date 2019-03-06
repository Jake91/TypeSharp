using System.Collections.Generic;
using System.Linq;
using TypeSharp.TsModel.Types;

namespace TypeSharp.TsModel.Modules
{
    public sealed class TsModule
    {
        public TsModuleLocation Location { get; }

        public ICollection<TsModuleImport> Imports { get; set; }

        public ICollection<TsTypeDefinitionBase> Types { get; set; }


        public TsModule(TsModuleLocation location, ICollection<TsModuleImport> imports, ICollection<TsTypeDefinitionBase> types)
        {
            Location = location;
            Imports = imports;
            Types = types;
        }

        public bool GeneratesJavascript => Types.Any(x => !(x is TsInterface));

        public string GetModuleImport(string rootElement) // todo naming?
        {
            return $@"{rootElement}/{string.Join("/", Location.Path)}/{Location.Name}";
        }
    }
}
