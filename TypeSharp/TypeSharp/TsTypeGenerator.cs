using Castle.Core.Internal;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using TypeSharp.TsModel.Types;

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
            while (!stack.IsNullOrEmpty()) // Use while loop instead of recursion for performance reasons
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
                TryAddType(stack, type.BaseType);
            }
        }

        private static void AddPropertyTypesIfItExist(Stack<Type> stack, Type type)
        {
            foreach (var property in GetProperties(type))
            {
                TryAddType(stack, property.PropertyType);
            }
        }

        private static void TryAddType(Stack<Type> stack, Type type)
        {
            if (IsDefaultType(type)) /*Remove string, int etc*/
            {
                return;
            }

            if (type.IsGenericParameter) /*Remove T1, T2...*/
            {
                return;
            }
            
            if(IsCollection(type))
            {
                TryAddType(stack, GetCollectionType(type));
            }
            else if (type.IsGenericType)
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
                foreach (var genericType in type.GetGenericArguments())
                {
                    TryAddType(stack, genericType);
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
                    return new TsClass(type, GetGenericTypeName(type), true, new List<TsClassProperty>(), null, type.GetGenericArguments().Select(x => new TsGenericArgument(x, x.Name)).ToList());
                }
                return new TsClass(type, type.Name, true, new List<TsClassProperty>(), null, new List<TsGenericArgument>());
            }
            else if (type.IsInterface || (type.IsClass && generateInterfaceAsDefault))
            {
                if (type.IsGenericType) // todo
                {
                    AssureTypeDefinition(type);
                    return new TsInterface(type, GetGenericTypeName(type), true, new List<TsInterfaceProperty>(), null, type.GetGenericArguments().Select(x => new TsGenericArgument(x, x.Name)).ToList());
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

        private static TsTypeBase GetTsType(Type type, IReadOnlyDictionary<Type, TsTypeBase> typeDict) // todo naming?
        {
            if (type.IsGenericParameter)
            {
                return GetTsGenericTypeReferenceArgument(type, typeDict);
            }
            
            if (IsCollection(type))
            {
                return new TsArray(type, GetTsType(GetCollectionType(type), typeDict));
            }

            if (!type.IsGenericType)
            {
                return IsDefaultType(type) ?
                    GetDefaultType(type) :
                    typeDict[type];
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

        private static TsGenericArgument GetTsGenericTypeReferenceArgument(Type genericParameter, IReadOnlyDictionary<Type, TsTypeBase> typeDict)
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
            return baseType != null && baseType != typeof(object) && baseType != typeof(Enum) && !IsCollection(baseType);
        }

        private static void PopulateBaseType(Type baseType, Action<TsTypeBase> setBaseTypeFn, IReadOnlyDictionary<Type, TsTypeBase> typeDict)
        {
            if (HasBaseType(baseType))
            {
                setBaseTypeFn(GetTsType(baseType, typeDict));
            }
        }

        private static void PopulateProperties(TsClass type, IReadOnlyDictionary<Type, TsTypeBase> typeDict)
        {
            var properties = GetProperties(type.CSharpType);

            foreach (var propertyInfo in properties)
            {
                var typeContainer = GetTsType(propertyInfo.PropertyType, typeDict);
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
                        GetTsType(propertyInfo.PropertyType, typeDict)));
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

        private static bool IsCollection(Type type)
        {
            return type.IsArray ||
                   (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(IEnumerable<>)) ||
                   (type.IsGenericType && type.GetGenericTypeDefinition().GetInterfaces()
                                        .Select(x => x.IsGenericType ? x.GetGenericTypeDefinition() : x).Contains(typeof(IEnumerable<>)));
        }

        private static Type GetCollectionType(Type type)
        {
            if (!IsCollection(type))
            {
                throw new ArgumentException("Type is not a collection");
            }

            if (type.IsArray)
            {
                return type.GetElementType();
            }

            return type.GetGenericArguments().Single();
        }
    }
}
