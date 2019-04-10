﻿using Schema.DataHolders;

namespace Schema.Validators.Object
{
    public class TweezersObjectValidator : IValidator
    {
        private TweezersObject ObjectReference { get; set; }

        private TweezersObjectValidator(TweezersObject objectReference)
        {
            this.ObjectReference = objectReference;
        }

        public static TweezersObjectValidator Create(TweezersObject objectReference)
        {
            return new TweezersObjectValidator(objectReference);
        }

        public TweezersValidationResult Validate(string fieldName, dynamic value)
        {
            return ObjectReference.Validate(value);
        }

        public string Name => $"Object {ObjectReference.DisplayNames.SingularName}";
    }
}
