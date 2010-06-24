using Composite.Functions;
using System.Collections.Generic;
using Composite.StandardPlugins.Functions.FunctionProviders.StandardFunctionProvider.Foundation;
using Composite.ResourceSystem;
using System.CodeDom;
using Composite.Validation;
using System;
using Composite.Validation.Validators;


namespace Composite.StandardPlugins.Functions.FunctionProviders.StandardFunctionProvider.Utils.Validation
{
    public sealed class StringLengthValidationFunction : StandardFunctionBase
	{
        public StringLengthValidationFunction(EntityTokenFactory entityTokenFactory)
            : base("StringLengthValidation", "Composite.Utils.Validation", typeof(PropertyValidatorBuilder<string>), entityTokenFactory)
        {
        }


        protected override IEnumerable<StandardFunctionParameterProfile> StandardFunctionParameterProfiles
        {
            get
            {
                yield return new StandardFunctionParameterProfile(
                    "min", typeof(int), true, new NoValueValueProvider(), StandardWidgetFunctions.IntegerTextBoxWidget);

                yield return new StandardFunctionParameterProfile(
                    "max", typeof(int), true, new NoValueValueProvider(), StandardWidgetFunctions.IntegerTextBoxWidget);
            }
        }



        public override object Execute(ParameterList parameters, FunctionContextContainer context)
        {
            CodeAttributeDeclaration codeAttributeDeclaration = new CodeAttributeDeclaration(new CodeTypeReference(typeof(StringSizeValidatorAttribute)));

            int min = parameters.GetParameter<int>("min");
            int max = parameters.GetParameter<int>("max");

            codeAttributeDeclaration.Arguments.Add(new CodeAttributeArgument(new CodePrimitiveExpression(min)));
            codeAttributeDeclaration.Arguments.Add(new CodeAttributeArgument(new CodePrimitiveExpression(max)));

            return new ConstrucorBasedPropertyValidatorBuilder<string>(codeAttributeDeclaration, new StringSizeValidatorAttribute(min, max));
        }
	}
}
