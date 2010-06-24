﻿using System;
using System.Xml.Linq;
using System.Linq;
using Composite.Xml;
using Composite.Types;
using System.Collections.Generic;


namespace Composite.Data.DynamicTypes
{
	public static class DynamicTypeMarkupServices
	{
        private static readonly XName _fieldReferenceElementName = Namespaces.DynamicData10 + "fieldreference";
        private static readonly XName _fieldReferenceTypeAttributeName = "typemanagername";
        private static readonly XName _fieldReferenceFieldAttributeName = "fieldname";


        public static XElement GetReferenceElement(this DataFieldDescriptor fieldToReference, DataTypeDescriptor ownerTypeDescriptor)
        {
            return GetReferenceElement(fieldToReference.Name,ownerTypeDescriptor.TypeManagerTypeName);
        }



        public static XElement GetReferenceElement(string fieldName, string typeManagerName)
        {
            // ensure "data:" prefix in markup:
            XElement element = XElement.Parse(string.Format("<data:{0} xmlns:data='{1}' />", _fieldReferenceElementName.LocalName, _fieldReferenceElementName.NamespaceName));

            element.Add(
                new XAttribute(_fieldReferenceFieldAttributeName, fieldName),
                new XAttribute(_fieldReferenceTypeAttributeName, typeManagerName));

            return element;
        }



        public static bool TryGetDescriptors(XElement fieldReferenceElement, out DataTypeDescriptor typeDescriptor, out DataFieldDescriptor fieldDescriptor)
        {
            typeDescriptor = null;
            fieldDescriptor = null;

            if (fieldReferenceElement.Name != _fieldReferenceElementName)
            {
                throw new InvalidOperationException(string.Format("Unexpected element name '{0}'. Expected '{1}'",
                                                                  fieldReferenceElement.Name, _fieldReferenceElementName));
            }

            string typeManagerName = fieldReferenceElement.Attribute(_fieldReferenceTypeAttributeName).Value;
            string fieldName = fieldReferenceElement.Attribute(_fieldReferenceFieldAttributeName).Value;

            Type t = TypeManager.TryGetType(typeManagerName);
            if (t == null)
            {
                return false;
            }

            typeDescriptor = DynamicTypeManager.GetDataTypeDescriptor(t.GetImmutableTypeId());
            if (typeDescriptor == null)
            {
                return false;
            }

            if(fieldName == "DataSourceId")
            {
                fieldDescriptor = new DataFieldDescriptor(Guid.Empty, "DataSourceId", StoreFieldType.LargeString, typeof (string));
                return true;
            }

            fieldDescriptor = typeDescriptor.Fields.Where(f => f.Name == fieldName).FirstOrDefault();
            if (fieldDescriptor == null)
            {
                return false;
            }
            
            return true;
        }



	    public static IEnumerable<FieldReferenceDefinition> GetFieldReferenceDefinitions(XContainer container, string typeManagerName)
        {
            var typeReferenceElements = container.Descendants(_fieldReferenceElementName).Where(f=>f.Attribute(_fieldReferenceTypeAttributeName).Value==typeManagerName);

            foreach (var referenceElement in typeReferenceElements)
            {
                yield return
                    new FieldReferenceDefinition { FieldName = referenceElement.Attribute(_fieldReferenceFieldAttributeName).Value, FieldReferenceElement = referenceElement };
            }
        }


        public class FieldReferenceDefinition
        {
            public string FieldName { get; set; }
            public XElement FieldReferenceElement { get; set; }
        }

	}
}
