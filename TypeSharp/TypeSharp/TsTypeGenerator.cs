﻿using System;
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
            var typeDict = types.Distinct()
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
            while (!stack.IsNullOrEmpty())
            {
                var current = stack.Pop();
                AddBaseTypeIfItExist(stack, current);
                AddPropertyTypesIfItExist(stack, current);
                set.Add(current);
            }
            return set.ToList();
        }

        private static void AddBaseTypeIfItExist(Stack<Type> stack, Type type)
        {
            if (HasBaseType(type.BaseType))
            {
                stack.Push(type.BaseType);
            }
        }

        private static void AddPropertyTypesIfItExist(Stack<Type> stack, Type type)
        {
            foreach (var property in GetProperties(type))
            {
                if (!IsDefaultType(property.PropertyType))
                {
                    stack.Push(property.PropertyType);
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
                return new TsClass(type, type.Name, true, new List<TsClassProperty>(), null);
            }
            else if (type.IsInterface || (type.IsClass && generateInterfaceAsDefault))
            {
                return new TsInterface(type, type.Name, true, new List<TsInterfaceProperty>(), null);
            }
            throw new ArgumentException($"Type ({type.Name}) is not a interface, enum or class");
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
            return baseType != null && baseType != typeof(object);
        }

        private static void PopulateBaseType(Type baseType, Action<TsTypeBase> setBaseTypeFn, IReadOnlyDictionary<Type, TsTypeBase> typeDict)
        {
            if (HasBaseType(baseType))
            {
                if (typeDict.TryGetValue(baseType, out var tsType))
                {
                    setBaseTypeFn(tsType);
                }
                else
                {
                    throw new ArgumentException($"Could not populate basetype ({baseType.Name})");
                }
            }
        }

        private static void PopulateProperties(TsClass type, IReadOnlyDictionary<Type, TsTypeBase> typeDict)
        {
            var properties = GetProperties(type.CSharpType);

            foreach (var propertyInfo in properties)
            {
                if (IsDefaultType(propertyInfo.PropertyType))
                {
                    type.Properties.Add(
                        new TsClassProperty(
                            name: propertyInfo.Name,
                            propertyType: GetDefaultType(propertyInfo.PropertyType),
                            accessModifier: TsAccessModifier.Public, // todo fix these fields
                            hasGetter: false,
                            hasSetter: false));
                }
                else if (typeDict.TryGetValue(propertyInfo.PropertyType, out var tsType))
                {
                    type.Properties.Add(new TsClassProperty(propertyInfo.Name, tsType, TsAccessModifier.Public, false, false));
                }
                else
                {
                    throw new ArgumentException($"Type ({type.Name}) is not a default type, and it is not part of the types to generate");
                }
            }
        }

        private static void PopulateProperties(TsInterface type, IReadOnlyDictionary<Type, TsTypeBase> typeDict)
        {
            var properties = GetProperties(type.CSharpType);
            
            foreach (var propertyInfo in properties)
            {
                if (IsDefaultType(propertyInfo.PropertyType))
                {
                    type.Properties.Add(
                        new TsInterfaceProperty(
                            propertyInfo.Name,
                            GetDefaultType(propertyInfo.PropertyType)));
                }
                else if (typeDict.TryGetValue(propertyInfo.PropertyType, out var tsType))
                {
                    type.Properties.Add(new TsInterfaceProperty(propertyInfo.Name, tsType));
                }
                else
                {
                    throw new ArgumentException($"Type ({type.Name}) is not a default type, and it is not part of the types to generate");
                }
            }
        }

        private static TsDefaultType GetDefaultType(Type type)
        {
            if (type == typeof(bool))
            {
                return new TsBoolean(type, type.Name);
            }
            else if (type == typeof(DateTime) ||
                     type == typeof(DateTimeOffset))
            {
                return new TsDate(type, type.Name);
            }
            else if (type == typeof(string))
            {
                return new TsString(type, type.Name);
            }
            else if (type == typeof(long) ||
                     type == typeof(int) ||
                     type == typeof(decimal) ||
                     type == typeof(double))
            {
                return new TsNumber(type, type.Name);
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
        public string Name { get; }

        protected TsTypeBase(Type cSharpType, string name)
        {
            CSharpType = cSharpType;
            Name = name;
        }
    }

    public sealed class TsClass : TsTypeBase
    {
        public TsTypeBase BaseType { get; internal set; }

        public bool IsExport { get; set; }

        public ICollection<TsClassProperty> Properties { get; }

        public TsClass(Type cSharpType, string name, bool isExport, ICollection<TsClassProperty> properties, TsTypeBase baseType) : base(cSharpType, name)
        {
            IsExport = isExport;
            Properties = properties;
            BaseType = baseType;
        }
    }

    public sealed class TsInterface : TsTypeBase
    {
        public bool IsExport { get; }

        public ICollection<TsInterfaceProperty> Properties { get; }

        public TsTypeBase BaseType { get; internal set; }

        public TsInterface(Type cSharpType, string name, bool isExport, ICollection<TsInterfaceProperty> properties, TsTypeBase baseType)
            : base(cSharpType, name)
        {
            IsExport = isExport;
            Properties = properties;
            BaseType = baseType;
        }
    }

    public sealed class TsEnum : TsTypeBase
    {
        public bool IsExport { get; set; }
        public ICollection<EnumValue> Values { get; }

        public TsEnum(Type cSharpType, string name, bool isExport, ICollection<EnumValue> values) : base(cSharpType, name)
        {
            IsExport = isExport;
            Values = values;
        }
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

        protected TsDefaultType(Type cSharpType, string name) : base(cSharpType, name)
        {
        }
    }

    public sealed class TsNumber : TsDefaultType
    {
        public override bool IsObject { get; protected set; }

        public TsNumber(Type cSharpType, string name, bool isObject = false) : base(cSharpType, name)
        {
            IsObject = isObject;
        }

        public void SetIsObject(bool isObject)
        {
            IsObject = isObject;
        }
    }

    public sealed class TsBoolean : TsDefaultType
    {
        public override bool IsObject { get; protected set; }

        public TsBoolean(Type cSharpType, string name, bool isObject = false) : base(cSharpType, name)
        {
            IsObject = isObject;
        }

        public void SetIsObject(bool isObject)
        {
            IsObject = isObject;
        }
    }

    public sealed class TsString : TsDefaultType
    {
        public override bool IsObject { get; protected set; }

        public TsString(Type cSharpType, string name, bool isObject = false) : base(cSharpType, name)
        {
            IsObject = isObject;
        }

        public void SetIsObject(bool isObject)
        {
            IsObject = isObject;
        }
    }

    public sealed class TsDate : TsDefaultType
    {
        public override bool IsObject { get; protected set; }

        public TsDate(Type cSharpType, string name) : base(cSharpType, name)
        {
            IsObject = true;
        }
    }

    public class TsClassProperty
    {
        public string Name { get; set; }
        public TsTypeBase PropertyType { get; }
        public TsAccessModifier AccessModifier { get; set; }
        public bool HasGetter { get; set; }
        public bool HasSetter { get; set; }

        public TsClassProperty(string name, TsTypeBase propertyType, TsAccessModifier accessModifier, bool hasGetter, bool hasSetter)
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
        public TsTypeBase PropertyType { get; }

        public TsInterfaceProperty(string name, TsTypeBase propertyType)
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
