using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using TypeSharp.Common;
using TypeSharp.TsModel.Types;

namespace TypeSharp.TsGenerators
{
    public interface IPropertyProvider
    {
        PropertyInfo[] GetProperties(Type type);
        TsInterfaceProperty CreateInterfaceProperty(PropertyInfo property, TsTypeBase propertyType);
        TsClassProperty CreateClassProperty(PropertyInfo property, TsTypeBase propertyType);
    }

    public class PropertyProvider : IPropertyProvider
    {
        public PropertyInfo[] GetProperties(Type type)
        {
            return type.GetProperties(BindingFlags.Instance | BindingFlags.Public | BindingFlags.DeclaredOnly);
        }

        public TsInterfaceProperty CreateInterfaceProperty(PropertyInfo property, TsTypeBase propertyType)
        {
            return new TsInterfaceProperty(property.Name, propertyType);
        }

        public TsClassProperty CreateClassProperty(PropertyInfo property, TsTypeBase propertyType)
        {
            return new TsClassProperty(property.Name, propertyType, TsAccessModifier.Public, hasGetter: false, hasSetter: false); // todo
        }
    }

    public interface ITsCollectionProvider
    {
        bool IsCollection(Type type);
        TsCollection GetCollectionType(Type type, TsTypeBase elementType);
        Type GetTypeForCollectionElements(Type type);
    }

    public class TsCollectionProvider : ITsCollectionProvider
    {
        public bool IsCollection(Type type)
        {
            return type.IsArray ||
                   (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(IEnumerable<>)) ||
                   (type.IsGenericType && type.GetGenericTypeDefinition().GetInterfaces()
                        .Select(x => x.IsGenericType ? x.GetGenericTypeDefinition() : x).Contains(typeof(IEnumerable<>)));
        }

        public TsCollection GetCollectionType(Type type, TsTypeBase elementType)
        {
            // Only supports arrays of today. Might want to have set in future
            return new TsArray(type, elementType);
        }

        public Type GetTypeForCollectionElements(Type type)
        {
            if (!IsCollection(type))
            {
                throw new ArgumentException($"Type ({type.Name}) is not a collection");
            }

            if (type.IsArray)
            {
                return type.GetElementType();
            }

            return type.GetGenericArguments().Single();
        }
    }

    public interface ITsDefaultTypeProvider
    {
        TsDefaultTypeBase GetDefaultType(Type type);
        bool IsDefaultType(Type type);
    }


    public class TsDefaultTypeProvider : ITsDefaultTypeProvider
    {
        public TsDefaultTypeBase GetDefaultType(Type type)
        {
            if (type == typeof(bool))
            {
                return new TsBoolean(type);
            }
            else if (type == typeof(DateTime) ||
                     type == typeof(DateTimeOffset))
            {
                return new TsDate(type);
            }
            else if (type == typeof(string))
            {
                return new TsString(type);
            }
            else if (type == typeof(long) ||
                     type == typeof(int) ||
                     type == typeof(decimal) ||
                     type == typeof(double))
            {
                return new TsNumber(type);
            }
            throw new ArgumentException($"Type ({type.Name}) is not a default ts type");
        }

        public bool IsDefaultType(Type type) 
        {
            // todo add more?
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
    }

    public interface IBaseTypeReader
    {
        bool HasBaseType(Type baseType);
    }

    public class BaseTypeReader : IBaseTypeReader
    {
        private readonly ITsCollectionProvider _tsCollectionProvider;

        public BaseTypeReader(ITsCollectionProvider tsCollectionProvider)
        {
            _tsCollectionProvider = tsCollectionProvider;
        }

        public bool HasBaseType(Type baseType)
        {
            return baseType != null && baseType != typeof(object) && baseType != typeof(Enum) && !this._tsCollectionProvider.IsCollection(baseType);
        }
    }

    public interface IDependingTypeResolver
    {
        IList<Type> GetDependingTypes(Type type);
    }

    public class DependingTypeResolver : IDependingTypeResolver
    {
        private readonly IBaseTypeReader _baseTypeReader;
        private readonly IPropertyProvider _propertyProvider;
        private readonly ITsDefaultTypeProvider _defaultTypeProvider;
        private readonly ITsCollectionProvider _collectionProvider;

        public DependingTypeResolver(
            IBaseTypeReader baseTypeReader, 
            IPropertyProvider propertyProvider,
            ITsDefaultTypeProvider defaultTypeProvider, 
            ITsCollectionProvider collectionProvider)
        {
            _baseTypeReader = baseTypeReader;
            _propertyProvider = propertyProvider;
            _defaultTypeProvider = defaultTypeProvider;
            _collectionProvider = collectionProvider;
        }

        public IList<Type> GetDependingTypes(Type type)
        {
            var dependingTypes = new HashSet<Type>();
            var typesToProcess = new Stack<Type>();
            AddBaseTypeIfExist(typesToProcess, type);
            AddPropertyTypesIfExist(typesToProcess, type);
            AddGenericArgumentsIfExist(typesToProcess, type);
            while (!typesToProcess.IsNullOrEmpty()) // Use while loop instead of recursion for performance reasons
            {
                var current = typesToProcess.Pop();
                if (dependingTypes.Contains(current))
                {
                    // stop circular refs
                    continue;
                }
                AddBaseTypeIfExist(typesToProcess, current);
                AddPropertyTypesIfExist(typesToProcess, current);
                AddGenericArgumentsIfExist(typesToProcess, type);
                dependingTypes.Add(current);
            }
            return dependingTypes.ToList();
        }

        private void AddBaseTypeIfExist(Stack<Type> typesToProcess, Type type)
        {
            if (this._baseTypeReader.HasBaseType(type.BaseType))
            {
                TryAddType(typesToProcess, type.BaseType);
            }
        }

        private void AddPropertyTypesIfExist(Stack<Type> typesToProcess, Type type)
        {
            foreach (var property in this._propertyProvider.GetProperties(type))
            {
                TryAddType(typesToProcess, property.PropertyType);
            }
        }

        private void TryAddType(Stack<Type> typesToProcess, Type type)
        {
            if (this._defaultTypeProvider.IsDefaultType(type)) /*Do not add string, int etc*/
            {
                return;
            }

            if (type.IsGenericParameter) /*Do not add T1, T2...*/
            {
                return;
            }

            if (this._collectionProvider.IsCollection(type))
            {
                TryAddType(typesToProcess, this._collectionProvider.GetTypeForCollectionElements(type));
            }
            else if (type.IsGenericType)
            {
                typesToProcess.Push(type.IsGenericTypeDefinition ? type : type.GetGenericTypeDefinition());
            }
            else
            {
                typesToProcess.Push(type);
            }
        }

        private void AddGenericArgumentsIfExist(Stack<Type> typesToProcess, Type type)
        {
            if (!type.IsGenericType)
            {
                return;
            }

            foreach (var genericType in type.GetGenericArguments())
            {
                TryAddType(typesToProcess, genericType);
            }
        }
    }

    public class TsTypeGenerator
    {
        private readonly IDependingTypeResolver _dependingTypeResolver;
        private readonly IBaseTypeReader _baseTypeReader;
        private readonly IPropertyProvider _propertyProvider;
        private readonly ITsCollectionProvider _collectionProvider;
        private readonly ITsDefaultTypeProvider _defaultTypeProvider;

        // todo
        public TsTypeGenerator() : this( 
            new DependingTypeResolver(new BaseTypeReader(new TsCollectionProvider()), new PropertyProvider(),
                new TsDefaultTypeProvider(), new TsCollectionProvider()),
            new BaseTypeReader(new TsCollectionProvider()), new PropertyProvider(), new TsCollectionProvider(),
            new TsDefaultTypeProvider())
        {

        }

        public TsTypeGenerator(
            IDependingTypeResolver dependingTypeResolver, 
            IBaseTypeReader baseTypeReader,
            IPropertyProvider propertyProvider, 
            ITsCollectionProvider collectionProvider,
            ITsDefaultTypeProvider defaultTypeProvider)
        {
            _dependingTypeResolver = dependingTypeResolver;
            _baseTypeReader = baseTypeReader;
            _propertyProvider = propertyProvider;
            _collectionProvider = collectionProvider;
            _defaultTypeProvider = defaultTypeProvider;
        }

        public IList<TsTypeDefinitionBase> Generate(Type type, bool generateInterfaceAsDefault = true)
        {
            return Generate(new List<Type>() {type});
        }

        public IList<TsTypeDefinitionBase> Generate(ICollection<Type> types, bool generateInterfaceAsDefault = true)
        {
            var typeDict = types
                .Select(AssureGenericsAreTypeDefinitions)
                .Distinct()
                .Concat(types.SelectMany(this._dependingTypeResolver.GetDependingTypes))
                .Distinct()
                .Select(x => CreateTsType(x, generateInterfaceAsDefault))
                .ToDictionary(x => x.CSharpType, x => x);

            foreach (var tsType in typeDict.Values.OfType<TsInterface>())
            {
                PopulateBaseType(tsType, typeDict);
                PopulateProperties(tsType, typeDict);
            }
            foreach (var tsType in typeDict.Values.OfType<TsClass>())
            {
                PopulateBaseType(tsType, typeDict);
                PopulateProperties(tsType, typeDict);
            }

            return typeDict.Values.ToList();
        }

        private Type AssureGenericsAreTypeDefinitions(Type type)
        {
            return type.IsGenericType && !type.IsGenericTypeDefinition ? type.GetGenericTypeDefinition() : type;
        }

        private static TsTypeDefinitionBase CreateTsType(Type type, bool generateInterfaceAsDefault)
        {
            if (type.IsEnum)
            {
                return new TsEnum(type, type.Name, true, GetEnumValues(type));
            }
            else if (type.IsClass && !generateInterfaceAsDefault)
            {
                if (type.IsGenericType)
                {
                    AssureTypeDefinition(type);
                    return new TsClass(type, GetGenericTypeName(type), true, new List<TsClassProperty>(), null, GetGenericArguments(type));
                }
                return new TsClass(type, type.Name, true, new List<TsClassProperty>(), null, new List<TsGenericArgument>());
            }
            else if (type.IsInterface || (type.IsClass && generateInterfaceAsDefault))
            {
                if (type.IsGenericType)
                {
                    AssureTypeDefinition(type);
                    return new TsInterface(type, GetGenericTypeName(type), true, new List<TsInterfaceProperty>(), null, GetGenericArguments(type));
                }
                return new TsInterface(type, type.Name, true, new List<TsInterfaceProperty>(), null, new List<TsGenericArgument>());
            }
            throw new ArgumentException($"Type ({type.Name}) is not a interface, enum or class");
        }

        private static string GetGenericTypeName(Type type)
        {
            if (!type.IsGenericType)
            {
                throw new ArgumentException($"Type ({type.Name}) is not a generic");
            }
            return type.Name.Remove(type.Name.IndexOf('`'));
        }

        private static List<TsGenericArgument> GetGenericArguments(Type type)
        {
            if (!type.IsGenericType)
            {
                throw new ArgumentException($"Type ({type.Name}) is not a generic");
            }
            return type.GetGenericArguments().Select(x => new TsGenericArgument(x, x.Name)).ToList();
        }

        private static void AssureTypeDefinition(Type type)
        {
            if (type.IsGenericType && !type.IsGenericTypeDefinition)
            {
                throw new ArgumentException($"Type ({type.Name}) is generic, but not a typedefinition");
            }
        }

        private TsTypeBase GetTsType(Type type, IReadOnlyDictionary<Type, TsTypeDefinitionBase> typeDict) // todo naming?
        {
            if (type.IsGenericParameter)
            {
                return GetTsGenericTypeReferenceArgument(type, typeDict);
            }
            
            if (this._collectionProvider.IsCollection(type))
            {
                return this._collectionProvider.GetCollectionType(type,
                    GetTsType(this._collectionProvider.GetTypeForCollectionElements(type), typeDict));
            }

            if (!type.IsGenericType)
            {
                return this._defaultTypeProvider.IsDefaultType(type) ?
                    this._defaultTypeProvider.GetDefaultType(type) :
                    (TsTypeBase)typeDict[type];
            }
            
            if (type.IsGenericType && type.IsGenericTypeDefinition)
            {
                throw new ArgumentException("TypContainer can not contain GenericTypeDefinition");
            }
            
            var list = new List<TsTypeBase>();
            foreach (var genericArgument in type.GetGenericArguments())
            {
                if (genericArgument.IsGenericParameter)
                {
                    list.Add(GetTsGenericTypeReferenceArgument(genericArgument, typeDict));
                }
                else
                {
                    list.Add(GetTsType(genericArgument, typeDict));
                }
            }
            return new TsGenericTypeReference(type, typeDict[type.GetGenericTypeDefinition()], list);
        }

        private static TsGenericArgument GetTsGenericTypeReferenceArgument(Type genericParameter, IReadOnlyDictionary<Type, TsTypeDefinitionBase> typeDict)
        {
            if (!genericParameter.IsGenericParameter)
            {
                throw new ArgumentException("Is not a generic parameter");
            }

            if (genericParameter.DeclaringType == null)
            {
                throw new ArgumentNullException(nameof(genericParameter.DeclaringType));
            }
            var declaringGenericTypeDefinition = genericParameter.DeclaringType.IsGenericTypeDefinition ?
                genericParameter.DeclaringType :
                genericParameter.DeclaringType.GetGenericTypeDefinition();
            var tsDaddy = typeDict[declaringGenericTypeDefinition];
            switch (tsDaddy)
            {
                case TsClass tsClass:
                    return tsClass.GenericArguments.Single(x => x.Name == genericParameter.Name);
                case TsInterface tsInterface:
                    return tsInterface.GenericArguments.Single(x => x.Name == genericParameter.Name);
                default:
                    throw new ArgumentException($"Invalid type ({tsDaddy.Name})");
            }
        }

        private static IList<TsEnumValue> GetEnumValues(Type type)
        {
            var list = new List<int>();
            foreach (var enumValue in type.GetEnumValues())
            {
                list.Add((int)enumValue);
            }
            return list.Zip(type.GetEnumNames(), (value, name) => new TsEnumValue(name, value)).ToList();
        }

        private void PopulateBaseType(TsInterface type, IReadOnlyDictionary<Type, TsTypeDefinitionBase> typeDict)
        {
            PopulateBaseType(type.CSharpType.BaseType, t => type.BaseType = t, typeDict);
        }

        private void PopulateBaseType(TsClass type, IReadOnlyDictionary<Type, TsTypeDefinitionBase> typeDict)
        {
            PopulateBaseType(type.CSharpType.BaseType, t => type.BaseType = t, typeDict);
        }

        private void PopulateBaseType(Type baseType, Action<TsTypeBase> setBaseTypeFn, IReadOnlyDictionary<Type, TsTypeDefinitionBase> typeDict)
        {
            if (this._baseTypeReader.HasBaseType(baseType))
            {
                setBaseTypeFn(GetTsType(baseType, typeDict));
            }
        }

        private void PopulateProperties(TsClass type, IReadOnlyDictionary<Type, TsTypeDefinitionBase> typeDict)
        {
            var properties = this._propertyProvider.GetProperties(type.CSharpType);

            foreach (var propertyInfo in properties)
            {
                var propertyType = GetTsType(propertyInfo.PropertyType, typeDict);
                type.Properties.Add(this._propertyProvider.CreateClassProperty(propertyInfo, propertyType));
            }
        }

        private void PopulateProperties(TsInterface type, IReadOnlyDictionary<Type, TsTypeDefinitionBase> typeDict)
        {
            var properties = this._propertyProvider.GetProperties(type.CSharpType);
            
            foreach (var propertyInfo in properties)
            {
                type.Properties.Add(this._propertyProvider.CreateInterfaceProperty(propertyInfo, GetTsType(propertyInfo.PropertyType, typeDict)));
            }
        }
    }
}
