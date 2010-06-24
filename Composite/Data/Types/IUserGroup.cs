﻿using System;
using Composite.Data.Hierarchy;
using Composite.Data.Hierarchy.DataAncestorProviders;
using Microsoft.Practices.EnterpriseLibrary.Validation.Validators;


namespace Composite.Data.Types
{
    [AutoUpdateble]
    [KeyPropertyName("Id")]
    [LabelPropertyName("Name")]
    [DataScope(DataScopeIdentifier.PublicName)]
    [ImmutableTypeId("{F32B2CBB-92A1-473c-99D6-5D0C20A7480E}")]
    [DataAncestorProvider(typeof(NoAncestorDataAncestorProvider))]
    public interface IUserGroup : IData
    {
        [StoreFieldType(PhysicalStoreFieldType.Guid)]
        [ImmutableFieldId("{93EA801A-AA0D-4cac-BE31-EDF9A8345D29}")]
        Guid Id { get; set; }


        [NotNullValidator()]
        [Composite.Validation.Validators.StringSizeValidator(2, 64)]
        [StoreFieldType(PhysicalStoreFieldType.String, 64)]
        [ImmutableFieldId("{86C65F7F-64EA-4fcc-980D-AAF79C32CEC6}")]
        string Name { get; set; }
    }
}
