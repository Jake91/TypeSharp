using TypeSharp.TsModel.Types;

namespace TypeSharp.TsModel.Modules
{
    public interface IModuleDivider
    {
        TsModuleLocation GetLocationAndName(TsTypeDefinitionBase tsType);
    }
}