﻿using Tweezers.Schema.DataHolders;

namespace Tweezers.Schema.Validators.String
{
    public sealed class MaximumLengthValidator : IValidator
    {
        public int Maximum { get; set; }

        private MaximumLengthValidator() { }

        public static MaximumLengthValidator Create(int minimum)
        {
            return new MaximumLengthValidator() {Maximum = minimum};
        }

        public string Name => "Maximum Length";

        public TweezersValidationResult Validate(string fieldName, dynamic value)
        {
            try
            {
                string parsedValue = (string) value;
                return parsedValue.Length <= Maximum
                    ? TweezersValidationResult.Accept()
                    : TweezersValidationResult.Reject($"The length of {fieldName} is higher than {Maximum}");
            }
            catch
            {
                return TweezersValidationResult.Reject($"Could not parse {fieldName}");
            }
        }
    }
}