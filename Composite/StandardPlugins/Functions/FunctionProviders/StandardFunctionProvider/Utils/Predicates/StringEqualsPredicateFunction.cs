﻿using System;
using System.Linq;
using System.Collections.Generic;
using System.Data.SqlTypes;

using Composite.Functions;

using Composite.StandardPlugins.Functions.FunctionProviders.StandardFunctionProvider.Foundation;
using Composite.ResourceSystem;
using System.Linq.Expressions;

namespace Composite.StandardPlugins.Functions.FunctionProviders.StandardFunctionProvider.Utils.Predicates
{
    public sealed class StringEqualsPredicateFunction : StandardFunctionBase
    {
        public StringEqualsPredicateFunction(EntityTokenFactory entityTokenFactory)
            : base("StringEquals", "Composite.Utils.Predicates", typeof(Expression<Func<string, bool>>), entityTokenFactory)
        {
        }


        protected override IEnumerable<StandardFunctionParameterProfile> StandardFunctionParameterProfiles
        {
            get
            {
                WidgetFunctionProvider textboxWidget = StandardWidgetFunctions.TextBoxWidget;

                yield return new StandardFunctionParameterProfile(
                    "Value", typeof(string), true, new ConstantValueProvider(""), textboxWidget);
            }
        }


        public override object Execute(ParameterList parameters, FunctionContextContainer context)
        {
            string value = parameters.GetParameter<string>("Value");
            Expression<Func<string, bool>> predicate = f => f == value;
            return predicate;
        }
    }
}
