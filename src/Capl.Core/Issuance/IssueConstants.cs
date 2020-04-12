namespace Capl.Issuance
{
    public static class IssueConstants
    {
        public static class Attributes
        {
            public const string Mode = "Mode";

            public const string PolicyId = "PolicyID";
        }

        public static class Elements
        {
            public const string IssuePolicy = "IssuePolicy";
        }

        public static class IssueModes
        {
            public const string Aggregate = "http://schemas.authz.org/cipl/mode#Aggregate";

            public const string Unique = "http://schemas.authz.org/cipl/mode#Unique";
        }

        public static class Namespaces
        {
            public const string Xmlns = "http://schemas.authz.org/cipl";
        }
    }
}