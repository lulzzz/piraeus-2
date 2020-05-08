namespace Piraeus.Adapters.Utilities
{
    public class ValidatorResult
    {
        public ValidatorResult(bool validated, string error = null)
        {
            Validated = validated;
            ErrorMessage = error;
        }

        public string ErrorMessage
        {
            get; set;
        }

        public bool Validated
        {
            get; internal set;
        }
    }
}