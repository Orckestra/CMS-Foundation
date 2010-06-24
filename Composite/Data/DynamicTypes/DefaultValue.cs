﻿using System;
using System.Linq;
using System.Collections.Generic;
using System.Text;
using Composite.Serialization;
using Composite.Instrumentation;
using System.CodeDom;

namespace Composite.Data.DynamicTypes
{
    [Serializable()]
    public sealed class DefaultValue : IComparable
    {
        private object _value;
        private DefaultValueType _valueType;


        public static DefaultValue String(string defaultValue) { return new DefaultValue(defaultValue); }
        public static DefaultValue Integer(int defaultValue) { return new DefaultValue(defaultValue); }
        public static DefaultValue Decimal(decimal defaultValue) { return new DefaultValue(defaultValue); }
        public static DefaultValue Boolean(bool defaultValue) { return new DefaultValue(defaultValue); }
        public static DefaultValue DateTime(DateTime defaultValue) { return new DefaultValue(defaultValue); }
        public static DefaultValue Guid(Guid defaultValue) { return new DefaultValue(defaultValue); }
        public static DefaultValue Now { get { return new DefaultValue(DefaultValueType.DateTimeNow); } }
        public static DefaultValue NewGuid { get { return new DefaultValue(DefaultValueType.NewGuid); } }



        public object Value
        {
            get
            {
                switch (_valueType)
                {
                    case DefaultValueType.DateTimeNow:
                        return System.DateTime.Now;

                    case DefaultValueType.NewGuid:
                        return System.Guid.NewGuid();

                    default:
                        return _value;
                }
            }
        }


        public DefaultValueType ValueType { get { return _valueType; } }



        public DefaultValue Clone()
        {
            switch (_valueType)
            {
                case DefaultValueType.Boolean:
                    return DefaultValue.Boolean((bool)_value);

                case DefaultValueType.DateTime:
                    return DefaultValue.DateTime((DateTime)_value);

                case DefaultValueType.DateTimeNow:
                    return DefaultValue.Now;

                case DefaultValueType.Decimal:
                    return DefaultValue.Decimal((decimal)_value);

                case DefaultValueType.Guid:
                    return DefaultValue.Guid((Guid)_value);

                case DefaultValueType.Integer:
                    return DefaultValue.Integer((int)_value);

                case DefaultValueType.NewGuid:
                    return DefaultValue.NewGuid;

                case DefaultValueType.String:
                    return DefaultValue.String((string)_value);

                default:
                    throw new NotImplementedException();
            }
        }



        public string Serialize()
        {
            StringBuilder sb = new StringBuilder();

            StringConversionServices.SerializeKeyValuePair(sb, "ValueType", this.ValueType.ToString());

            if (this.Value != null)
            {
                StringConversionServices.SerializeKeyValuePair(sb, "Value", this.Value.ToString());
            }

            return sb.ToString();
        }



        public static DefaultValue Deserialize(string serializedData)
        {
            using (TimerProfiler timerProfiler = TimerProfilerFacade.CreateTimerProfiler())
            {
                if (string.IsNullOrEmpty(serializedData) == true) throw new ArgumentNullException("serializedData");

                Dictionary<string, string> dic = StringConversionServices.ParseKeyValueCollection(serializedData);

                if (dic.ContainsKey("ValueType") == false) throw new ArgumentException("Wrong serialized format");

                string valueTypeString = StringConversionServices.DeserializeValue<string>(dic["ValueType"]);
                DefaultValueType valueType = (DefaultValueType)Enum.Parse(typeof(DefaultValueType), valueTypeString);

                bool hasValue = dic.ContainsKey("Value") == true;

                switch (valueType)
                {
                    case DefaultValueType.Boolean:
                        if (hasValue == false) throw new ArgumentException("Wrong serialized format");
                        bool boolValue = StringConversionServices.DeserializeValueBool(dic["Value"]);
                        return DefaultValue.Boolean(boolValue);

                    case DefaultValueType.DateTime:
                        if (hasValue == false) throw new ArgumentException("Wrong serialized format");
                        DateTime dateTimeValue = StringConversionServices.DeserializeValueDateTime(dic["Value"]);
                        return DefaultValue.DateTime(dateTimeValue);

                    case DefaultValueType.DateTimeNow:
                        return DefaultValue.Now;

                    case DefaultValueType.Decimal:
                        if (hasValue == false) throw new ArgumentException("Wrong serialized format");
                        decimal decimalValue = StringConversionServices.DeserializeValueDecimal(dic["Value"]);
                        return DefaultValue.Decimal(decimalValue);

                    case DefaultValueType.Guid:
                        if (hasValue == false) throw new ArgumentException("Wrong serialized format");
                        Guid guidValue = StringConversionServices.DeserializeValueGuid(dic["Value"]);
                        return DefaultValue.Guid(guidValue);

                    case DefaultValueType.Integer:
                        if (hasValue == false) throw new ArgumentException("Wrong serialized format");
                        int intValue = StringConversionServices.DeserializeValueInt(dic["Value"]);
                        return DefaultValue.Integer(intValue);

                    case DefaultValueType.NewGuid:
                        return DefaultValue.NewGuid;

                    case DefaultValueType.String:
                        string stringValue = null;
                        if (hasValue == true)
                        {
                            stringValue = StringConversionServices.DeserializeValueString(dic["Value"]);
                        }
                        return DefaultValue.String(stringValue);

                    default:
                        throw new NotImplementedException();
                }
            }                    
        }



        public override string ToString()
        {
            return Serialize();
        }



        internal CodeAttributeDeclaration GetCodeAttributeDeclaration()
        {
            CodeAttributeDeclaration codeAttributeDeclaration;

            switch (this.ValueType)
            {
                case DefaultValueType.DateTimeNow:
                    codeAttributeDeclaration = new CodeAttributeDeclaration(new CodeTypeReference(typeof(DefaultFieldNowDateTimeValueAttribute)));
                    break;

                case DefaultValueType.Guid:
                    codeAttributeDeclaration = new CodeAttributeDeclaration(new CodeTypeReference(typeof(DefaultFieldGuidValueAttribute)));
                    codeAttributeDeclaration.Arguments.Add(new CodeAttributeArgument(new CodePrimitiveExpression(this.Value.ToString())));
                    break;

                case DefaultValueType.Integer :
                    codeAttributeDeclaration = new CodeAttributeDeclaration(new CodeTypeReference(typeof(DefaultFieldIntValueAttribute)));
                    codeAttributeDeclaration.Arguments.Add(new CodeAttributeArgument(new CodePrimitiveExpression(this.Value)));
                    break;

                case DefaultValueType.Decimal:
                    codeAttributeDeclaration = new CodeAttributeDeclaration(new CodeTypeReference(typeof(DefaultFieldDecimalValueAttribute)));
                    codeAttributeDeclaration.Arguments.Add(new CodeAttributeArgument(new CodePrimitiveExpression(decimal.ToInt32((decimal)this.Value))));
                    break;

                case DefaultValueType.NewGuid:
                    codeAttributeDeclaration = new CodeAttributeDeclaration(new CodeTypeReference(typeof(DefaultFieldNewGuidValueAttribute)));
                    break;

                case DefaultValueType.String:
                    codeAttributeDeclaration = new CodeAttributeDeclaration(new CodeTypeReference(typeof(DefaultFieldStringValueAttribute)));
                    codeAttributeDeclaration.Arguments.Add(new CodeAttributeArgument(new CodePrimitiveExpression(this.Value)));
                    break;

                case DefaultValueType.Boolean:
                    codeAttributeDeclaration = new CodeAttributeDeclaration(new CodeTypeReference(typeof(DefaultFieldBoolValueAttribute)));
                    codeAttributeDeclaration.Arguments.Add(new CodeAttributeArgument(new CodePrimitiveExpression(this.Value)));
                    break;

                    
                default:
                    throw new NotImplementedException();
            }

            return codeAttributeDeclaration;
        }



        internal bool IsAssignableTo(StoreFieldType storeType)
        {
            switch (this.ValueType)
            {
                case DefaultValueType.DateTime:
                case DefaultValueType.DateTimeNow:
                    return storeType.IsDateTime || storeType.IsString;
                case DefaultValueType.String:
                    if (storeType.IsString == false) return false;
                    if (this.Value == null) return true;
                    if (storeType.MaximumLength >= this.Value.ToString().Length) return true;
                    return false;
                case DefaultValueType.Integer:
                    return storeType.IsNumeric || storeType.IsString;
                case DefaultValueType.Decimal:
                    return storeType.IsDecimal;
                case DefaultValueType.Boolean:
                    return storeType.IsBoolean || storeType.IsString;
                case DefaultValueType.Guid:
                case DefaultValueType.NewGuid:
                    return storeType.IsGuid || storeType.IsString;
            }

            throw new ArgumentException("Unknown store type");
        }



        private DefaultValue(string defaultValue)
        {
            _valueType = DefaultValueType.String;
            _value = defaultValue;
        }


        private DefaultValue(int defaultValue)
        {
            _valueType = DefaultValueType.Integer;
            _value = defaultValue;
        }


        private DefaultValue(decimal defaultValue)
        {
            _valueType = DefaultValueType.Decimal;
            _value = defaultValue;
        }


        private DefaultValue(bool defaultValue)
        {
            _valueType = DefaultValueType.Boolean;
            _value = defaultValue;
        }


        private DefaultValue(DateTime defaultValue)
        {
            _valueType = DefaultValueType.DateTime;
            _value = defaultValue;
        }


        private DefaultValue(Guid defaultValue)
        {
            _valueType = DefaultValueType.Guid;
            _value = defaultValue;
        }


        private DefaultValue(DefaultValueType defaultValueType)
        {
            switch (defaultValueType)
            {
                case DefaultValueType.DateTimeNow:
                case DefaultValueType.NewGuid:
                    _valueType = defaultValueType;
                    return;
            }

            throw new ArgumentException("Provided value type can not be constructed using this constructor. Use other constructor.");
        }



        public int CompareTo(object obj)
        {
            if (obj == null) throw new ArgumentNullException();
            if (obj.GetType() != typeof(DefaultValue)) throw new ArithmeticException(string.Format("Expected object of type {0}", typeof(DefaultValue)));

            var compareTo = (DefaultValue)obj;

            if (this.ValueType != compareTo.ValueType) return -1;

            switch (_valueType)
            {
                case DefaultValueType.DateTimeNow:
                case DefaultValueType.NewGuid:
                    return 0;

                case DefaultValueType.String:
                    if ((this.Value == null) && (compareTo.Value == null)) return 0;
                    return this.Value.ToString().CompareTo(compareTo.Value.ToString());

                case DefaultValueType.Integer:
                    return ((int)this.Value).CompareTo((int)compareTo.Value);

                case DefaultValueType.Decimal:
                    return ((Decimal)this.Value).CompareTo((Decimal)compareTo.Value);

                case DefaultValueType.Boolean:
                    return ((bool)this.Value).CompareTo((bool)compareTo.Value);

                case DefaultValueType.DateTime:
                    return ((DateTime)this.Value).CompareTo((DateTime)compareTo.Value);

                case DefaultValueType.Guid:
                    return this.Value.ToString().CompareTo(compareTo.Value.ToString());

                default:
                    throw new NotImplementedException("Unanble to compare DefaultValue objects - unknown case of DefaultValueType");
            }
        }
    }


    public enum DefaultValueType
    {
        DateTimeNow = 0,
        String = 1,
        Integer = 2,
        Decimal = 3,
        Boolean = 4,
        DateTime = 5,
        Guid = 6,
        NewGuid = 7
    }
}
