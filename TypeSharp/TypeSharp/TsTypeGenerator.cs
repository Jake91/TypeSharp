using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace TypeSharp
{
    public class TsTypeGenerator
    {
        public TsTypeBase Generate(Type type)
        {
            return Generate(new List<Type>() {type}).Single();
        }

        public IList<TsTypeBase> Generate(ICollection<Type> types)
        {
            var typeDict = types.Select(CreateTsType).ToDictionary(x => x.CSharpType, x => x);

            foreach (var tsType in typeDict.Values.OfType<TsTypeWithPropertiesBase>())
            {
                PopulateProperties(tsType, typeDict);
            }

            return typeDict.Values.ToList();
        }

        private static TsTypeBase CreateTsType(Type type)
        {
            if (type.IsInterface)
            {
                return new TsInterface(type, type.Name, true, new List<TsProperty>());
            }
            else if (type.IsEnum)
            {
                return new TsEnum(type, type.Name, true, GetEnumValues(type));
            }
            else if (type.IsClass)
            {
                return new TsClass(type, type.Name, true, new List<TsProperty>());
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

        private static void PopulateProperties(TsTypeWithPropertiesBase type, IReadOnlyDictionary<Type, TsTypeBase> typeDict)
        {
            var properties = type.CSharpType.GetProperties(BindingFlags.Instance | BindingFlags.Public); // todo improve
            
            foreach (var propertyInfo in properties)
            {
                if (IsDefaultType(propertyInfo.PropertyType))
                {
                    type.Properties.Add(
                        new TsProperty(
                            name: propertyInfo.Name,
                            propertyType: GetDefaultType(propertyInfo.PropertyType),
                            accessModifier: TsAccessModifier.Public,
                            hasGetter: false,
                            hasSetter: false));
                }
                else if (typeDict.TryGetValue(propertyInfo.PropertyType, out var tsType))
                {
                    type.Properties.Add(new TsProperty(propertyInfo.Name, tsType, TsAccessModifier.Public, false, false));
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

    public abstract class TsTypeWithPropertiesBase : TsTypeBase
    {
        public bool IsExport { get; set; }
        public ICollection<TsProperty> Properties { get; }

        protected TsTypeWithPropertiesBase(Type cSharpType, string name, bool isExport, ICollection<TsProperty> properties) : base(cSharpType, name)
        {
            IsExport = isExport;
            Properties = properties;
        }
    }

    public sealed class TsClass : TsTypeWithPropertiesBase
    {
        
        
        public TsClass(Type cSharpType, string name, bool isExport, ICollection<TsProperty> properties) : base(cSharpType, name, isExport, properties)
        {
        }
    }

    public sealed class TsInterface : TsTypeWithPropertiesBase
    {
        public TsInterface(Type cSharpType, string name, bool isExport, ICollection<TsProperty> properties)
            : base(cSharpType, name, isExport, properties)
        {
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

    public class TsProperty
    {
        public string Name { get; set; }
        public TsTypeBase PropertyType { get; set; }
        public TsAccessModifier AccessModifier { get; set; }
        public bool HasGetter { get; set; }
        public bool HasSetter { get; set; }

        public TsProperty(string name, TsTypeBase propertyType, TsAccessModifier accessModifier, bool hasGetter, bool hasSetter)
        {
            Name = name;
            PropertyType = propertyType;
            AccessModifier = accessModifier;
            HasGetter = hasGetter;
            HasSetter = hasSetter;
        }
    }

    public enum TsAccessModifier
    {
        Private = 1,
        Protected = 2,
        Public = 3
    }
}
