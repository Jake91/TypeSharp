using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Security.Cryptography.X509Certificates;
using Castle.Core.Internal;

namespace TypeSharp
{
    public class TsTypeGenerator
    {
        public IList<TsTypeBase> Generate(Type type, bool generateInterfaceAsDefault = true)
        {
            return Generate(new List<Type>() {type});
        }

        public IList<TsTypeBase> Generate(ICollection<Type> types, bool generateInterfaceAsDefault = true)
        {
            var typeDict = types
                .Select(x => x.IsGenericType && !x.IsGenericTypeDefinition ? x.GetGenericTypeDefinition() : x) // Remove generics that are not typedefs
                .Distinct()
                .Concat(types.SelectMany(GetDependingTypes))
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

        private static IList<Type> GetDependingTypes(Type type)
        {
            var set = new HashSet<Type>();
            var stack = new Stack<Type>();
            AddBaseTypeIfItExist(stack, type);
            AddPropertyTypesIfItExist(stack, type);
            AddGenericArgumentsIfExist(stack, type);
            while (!stack.IsNullOrEmpty())
            {
                var current = stack.Pop();
                if (set.Contains(current)) // todo performance
                {
                    // stop circular refs
                    continue;
                }
                AddBaseTypeIfItExist(stack, current);
                AddPropertyTypesIfItExist(stack, current);
                AddGenericArgumentsIfExist(stack, type);
                set.Add(current);
            }
            return set.ToList();
        }

        private static void AddBaseTypeIfItExist(Stack<Type> stack, Type type)
        {
            if (HasBaseType(type.BaseType))
            {
                AddTypeAndAssureTypeDefinitionForGenerics(stack, type.BaseType);
            }
        }

        private static void AddPropertyTypesIfItExist(Stack<Type> stack, Type type)
        {
            foreach (var property in GetProperties(type).Where(x => !x.PropertyType.IsGenericParameter)/*Remove T1, T2...*/)
            {
                if (!IsDefaultType(property.PropertyType))
                {
                    AddTypeAndAssureTypeDefinitionForGenerics(stack, property.PropertyType);
                }
            }
        }

        private static void AddTypeAndAssureTypeDefinitionForGenerics(Stack<Type> stack, Type type)
        {
            if (type.IsGenericType)
            {
                stack.Push(type.IsGenericTypeDefinition ? type : type.GetGenericTypeDefinition());
            }
            else
            {
                stack.Push(type);
            }
        }

        private static void AddGenericArgumentsIfExist(Stack<Type> stack, Type type)
        {
            if (type.IsGenericType)
            {
                foreach (var genericType in type.GetGenericArguments().Where(x => !x.IsGenericParameter)/*Remove T1, T2...*/)
                {
                    AddTypeAndAssureTypeDefinitionForGenerics(stack, genericType);
                }
            }
        }

        private static TsTypeBase CreateTsType(Type type, bool generateInterfaceAsDefault)
        {
            if (type.IsEnum)
            {
                return new TsEnum(type, type.Name, true, GetEnumValues(type));
            }
            else if (type.IsClass && !generateInterfaceAsDefault)
            {
                if (type.IsGenericType) // todo
                {
                    AssureTypeDefinition(type);
                    return new TsClass(type, GetGenericTypeName(type), true, new List<TsClassProperty>(), null, type.GetGenericArguments().Select(x => new TsGenericArgument(x.Name)).ToList());
                }
                return new TsClass(type, type.Name, true, new List<TsClassProperty>(), null, new List<TsGenericArgument>());
            }
            else if (type.IsInterface || (type.IsClass && generateInterfaceAsDefault))
            {
                if (type.IsGenericType) // todo
                {
                    AssureTypeDefinition(type);
                    return new TsInterface(type, GetGenericTypeName(type), true, new List<TsInterfaceProperty>(), null, type.GetGenericArguments().Select(x => new TsGenericArgument(x.Name)).ToList());
                }
                return new TsInterface(type, type.Name, true, new List<TsInterfaceProperty>(), null, new List<TsGenericArgument>());
            }
            throw new ArgumentException($"Type ({type.Name}) is not a interface, enum or class");
        }

        private static string GetGenericTypeName(Type type)
        {
            if (!type.IsGenericType)
            {
                throw new ArgumentException("Type is not a generic");
            }
            return type.Name.Remove(type.Name.IndexOf('`'));
        }

        private static void AssureTypeDefinition(Type type)
        {
            if (type.IsGenericType && !type.IsGenericTypeDefinition)
            {
                throw new ArgumentException("Type is generic, but not a typedefinition");
            }
        }

        private static TsReferenceBase GetTsReference(Type genericType, IReadOnlyDictionary<Type, TsTypeBase> typeDict)
        {
            if (genericType.IsGenericParameter)
            {
                return GetTsGenericTypeReferenceArgument(genericType, typeDict);
            }

            if (!genericType.IsGenericType)
            {
                return IsDefaultType(genericType) ? 
                    new TsTypeReference(GetDefaultType(genericType)) : 
                    new TsTypeReference(typeDict[genericType]);
            }
            
            if (genericType.IsGenericType && genericType.IsGenericTypeDefinition)
            {
                throw new ArgumentException("TypContainer can not contain GenericTypeDefinition");
            }
            
            var list = new List<TsReferenceBase>();
            foreach (var genericArgument in genericType.GetGenericArguments())
            {
                if (genericArgument.IsGenericParameter)
                {
                    list.Add(GetTsGenericTypeReferenceArgument(genericArgument, typeDict));
                }
                else
                {
                    list.Add(GetTsReference(genericArgument, typeDict));
                }
            }
            return new TsGenericTypeReference(typeDict[genericType.GetGenericTypeDefinition()], list);
        }

        private static TsGenericTypeReferenceArgument GetTsGenericTypeReferenceArgument(Type genericParameter, IReadOnlyDictionary<Type, TsTypeBase> typeDict)
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
                    return new TsGenericTypeReferenceArgument(
                        tsClass.GenericArguments.Single(x => x.Name == genericParameter.Name));
                case TsInterface tsInterface:
                    return new TsGenericTypeReferenceArgument(
                        tsInterface.GenericArguments.Single(x => x.Name == genericParameter.Name));
                default:
                    throw new ArgumentException($"Invalid type ({tsDaddy.Name})");
            }
        }

        private static IList<EnumValue> GetEnumValues(Type type)
        {
            var list = new List<int>();
            foreach (var enumValue in type.GetEnumValues())
            {
                list.Add((int)enumValue);
            }
            return list.Zip(type.GetEnumNames(), (value, name) => new EnumValue(name, value)).ToList();
        }

        private static PropertyInfo[] GetProperties(Type type)
        {
            return type.GetProperties(BindingFlags.Instance | BindingFlags.Public | BindingFlags.DeclaredOnly); // todo improve
        }

        private static void PopulateBaseType(TsInterface type, IReadOnlyDictionary<Type, TsTypeBase> typeDict)
        {
            PopulateBaseType(type.CSharpType.BaseType, t => type.BaseType = t, typeDict);
        }

        private static void PopulateBaseType(TsClass type, IReadOnlyDictionary<Type, TsTypeBase> typeDict)
        {
            PopulateBaseType(type.CSharpType.BaseType, t => type.BaseType = t, typeDict);
        }

        private static bool HasBaseType(Type baseType)
        {
            return baseType != null && baseType != typeof(object) && baseType != typeof(Enum);
        }

        private static void PopulateBaseType(Type baseType, Action<TsTypeReferenceBase> setBaseTypeFn, IReadOnlyDictionary<Type, TsTypeBase> typeDict)
        {
            if (HasBaseType(baseType))
            {
                setBaseTypeFn(GetTsReference(baseType, typeDict) as TsTypeReferenceBase);
            }
        }

        private static void PopulateProperties(TsClass type, IReadOnlyDictionary<Type, TsTypeBase> typeDict)
        {
            var properties = GetProperties(type.CSharpType);

            foreach (var propertyInfo in properties)
            {
                var typeContainer = GetTsReference(propertyInfo.PropertyType, typeDict);
                type.Properties.Add(new TsClassProperty(propertyInfo.Name, typeContainer, TsAccessModifier.Public, false, false));
            }
        }

        private static void PopulateProperties(TsInterface type, IReadOnlyDictionary<Type, TsTypeBase> typeDict)
        {
            var properties = GetProperties(type.CSharpType);
            
            foreach (var propertyInfo in properties)
            {
                type.Properties.Add(
                    new TsInterfaceProperty(
                        propertyInfo.Name,
                        GetTsReference(propertyInfo.PropertyType, typeDict)));
            }
        }

        private static TsDefaultType GetDefaultType(Type type)
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
            throw new ArgumentException("Type is not a default ts type");
        }

        private static bool IsDefaultType(Type type) // todo location?
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
    }

    public abstract class TsTypeBase
    {
        public Type CSharpType { get; }
        public abstract string Name { get; }

        protected TsTypeBase(Type cSharpType)
        {
            CSharpType = cSharpType;
        }
    }

    public sealed class TsClass : TsTypeBase
    {
        public TsTypeReferenceBase BaseType { get; internal set; }

        public bool IsExport { get; set; }

        public bool IsGeneric => !GenericArguments.IsNullOrEmpty();
        public ICollection<TsGenericArgument> GenericArguments { get; }

        public ICollection<TsClassProperty> Properties { get; }

        public TsClass(Type cSharpType, string name, bool isExport, ICollection<TsClassProperty> properties,
            TsTypeReferenceBase baseType, ICollection<TsGenericArgument> genericArguments) : base(cSharpType)
        {
            IsExport = isExport;
            Properties = properties;
            BaseType = baseType;
            GenericArguments = genericArguments;
            Name = name;
        }

        public override string Name { get; }
    }

    public class TsGenericTypeReference : TsTypeReferenceBase // <ClassX<T1>>
    {
        public override string Name => Type.Name;
        public override TsTypeBase Type { get; }
        public ICollection<TsReferenceBase> GenericArguments { get; }

        public TsGenericTypeReference(TsTypeBase type, ICollection<TsReferenceBase> genericArguments)
        {
            Type = type;
            GenericArguments = genericArguments;
        }
    }

    public class TsGenericTypeReferenceArgument : TsReferenceBase // <T1>...
    {
        public override string Name => GenericArgument.Name;

        public TsGenericArgument GenericArgument { get; }

        public TsGenericTypeReferenceArgument(TsGenericArgument genericArgument)
        {
            GenericArgument = genericArgument;
        }
    }

    public abstract class TsReferenceBase
    {
        public abstract string Name { get; }
    }

    public abstract class TsTypeReferenceBase : TsReferenceBase
    {
        public abstract TsTypeBase Type { get; }
    }

    public sealed class TsTypeReference : TsTypeReferenceBase // ClassX
    {
        public override string Name => Type.Name;
        public override TsTypeBase Type { get; }

        public TsTypeReference(TsTypeBase type)
        {
            Type = type;
        }

        //public static implicit operator TsTypeBase(TypeContainer baseTypeContainer)
        //{
        //    return baseTypeContainer.Type;
        //}

        //public static explicit operator TypeContainer(TsTypeBase tsTypeBase)
        //{
        //    return new TypeContainer(tsTypeBase, new List<TsTypeBase>());
        //}
    }

    public sealed class TsInterface : TsTypeBase
    {
        public bool IsExport { get; }

        public ICollection<TsInterfaceProperty> Properties { get; }

        public bool IsGeneric => !GenericArguments.IsNullOrEmpty();
        public ICollection<TsGenericArgument> GenericArguments { get; }

        public TsTypeReferenceBase BaseType { get; internal set; }

        public TsInterface(Type cSharpType, string name, bool isExport, ICollection<TsInterfaceProperty> properties,
            TsTypeReferenceBase baseType, ICollection<TsGenericArgument> genericArguments)
            : base(cSharpType)
        {
            IsExport = isExport;
            Properties = properties;
            BaseType = baseType;
            GenericArguments = genericArguments;
            Name = name;
        }

        public override string Name { get; }
    }

    public class TsGenericArgument
    {
        public string Name { get; }

        public TsGenericArgument(string name)
        {
            Name = name;
        }
    }

    public sealed class TsEnum : TsTypeBase
    {
        public bool IsExport { get; set; }
        public ICollection<EnumValue> Values { get; }

        public TsEnum(Type cSharpType, string name, bool isExport, ICollection<EnumValue> values) : base(cSharpType)
        {
            IsExport = isExport;
            Values = values;
            Name = name;
        }

        public override string Name { get; }
    }

    public sealed class EnumValue
    {
        public string Name { get; }
        public int Value { get; }

        public EnumValue(string name, int value)
        {
            Name = name;
            Value = value;
        }
    }

    public abstract class TsDefaultType : TsTypeBase
    {
        public abstract bool IsObject { get; protected set; }

        protected TsDefaultType(Type cSharpType) : base(cSharpType)
        {
        }
    }

    public sealed class TsNumber : TsDefaultType
    {
        public override bool IsObject { get; protected set; }

        public TsNumber(Type cSharpType, bool isObject = false) : base(cSharpType)
        {
            IsObject = isObject;
        }

        public void SetIsObject(bool isObject)
        {
            IsObject = isObject;
        }

        public override string Name => IsObject ? "Number" : "number";
    }

    public sealed class TsBoolean : TsDefaultType
    {
        public override bool IsObject { get; protected set; }

        public TsBoolean(Type cSharpType, bool isObject = false) : base(cSharpType)
        {
            IsObject = isObject;
        }

        public void SetIsObject(bool isObject)
        {
            IsObject = isObject;
        }

        public override string Name => IsObject ? "Boolean" : "boolean";
    }

    public sealed class TsString : TsDefaultType
    {
        public override bool IsObject { get; protected set; }

        public TsString(Type cSharpType, bool isObject = false) : base(cSharpType)
        {
            IsObject = isObject;
        }

        public void SetIsObject(bool isObject)
        {
            IsObject = isObject;
        }

        public override string Name => IsObject ? "String" : "string";
    }

    public sealed class TsDate : TsDefaultType
    {
        public override bool IsObject { get; protected set; }

        public TsDate(Type cSharpType) : base(cSharpType)
        {
            IsObject = true;
        }

        public override string Name => "Date";
    }

    public class TsClassProperty
    {
        public string Name { get; set; }
        public TsReferenceBase PropertyType { get; }
        public TsAccessModifier AccessModifier { get; set; }
        public bool HasGetter { get; set; } // todo
        public bool HasSetter { get; set; } // todo

        public TsClassProperty(string name, TsReferenceBase propertyType, TsAccessModifier accessModifier, bool hasGetter, bool hasSetter)
        {
            Name = name;
            PropertyType = propertyType;
            AccessModifier = accessModifier;
            HasGetter = hasGetter;
            HasSetter = hasSetter;
        }
    }

    public class TsInterfaceProperty
    {
        public string Name { get; }
        public TsReferenceBase PropertyType { get; }

        public TsInterfaceProperty(string name, TsReferenceBase propertyType)
        {
            Name = name;
            PropertyType = propertyType;
        }
    }

    public enum TsAccessModifier
    {
        None = 0,
        Private = 1,
        Protected = 2,
        Public = 3
    }
}
