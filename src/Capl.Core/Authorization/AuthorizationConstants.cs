using System.Diagnostics.CodeAnalysis;

namespace Capl.Authorization
{
    /// <summary>
    ///     Constants used for Access Control
    /// </summary>
    public static class AuthorizationConstants
    {
        [SuppressMessage("Microsoft.Design", "CA1034:NestedTypesShouldNotBeVisible")]
        public static class Attributes
        {
            public const string ClaimType = "ClaimType";

            public const string Delegation = "Delegation";

            public const string Evaluates = "Evaluates";

            public const string Issuer = "Issuer";

            public const string PolicyId = "PolicyID";

            public const string Required = "Required";

            public const string TermId = "TermID";

            public const string TransformId = "TransformID";

            public const string Type = "Type";
        }

        [SuppressMessage("Microsoft.Design", "CA1034:NestedTypesShouldNotBeVisible")]
        public static class Elements
        {
            public const string AuthorizationPolicy = "AuthorizationPolicy";

            public const string LogicalAnd = "LogicalAnd";

            public const string LogicalOr = "LogicalOr";

            public const string Match = "Match";

            public const string Operation = "Operation";

            public const string Rule = "Rule";

            public const string SourceClaim = "SourceClaim";

            public const string TargetClaim = "TargetClaim";

            public const string Transform = "Transform";

            public const string Transforms = "Transforms";
        }

        [SuppressMessage("Microsoft.Design", "CA1034:NestedTypesShouldNotBeVisible")]
        public static class MatchUris
        {
            public const string Any = "http://schemas.authz.org/capl/match#Any";

            public const string ComplexType = "http://schemas.authz.org/capl/match#ComplexType";

            public const string Literal = "http://schemas.authz.org/capl/match#Literal";

            public const string Pattern = "http://schemas.authz.org/capl/match#Pattern";
        }

        [SuppressMessage("Microsoft.Design", "CA1034:NestedTypesShouldNotBeVisible")]
        public static class Namespaces
        {
            public const string AuthorizationOperations = "http://schemas.authz.org/capl/operation";

            public const string TransformOperations = "http://schemas.authz.org/capl/transform";

            public const string Xmlns = "http://schemas.authz.org/capl";
        }

        [SuppressMessage("Microsoft.Design", "CA1034:NestedTypesShouldNotBeVisible")]
        public static class OperationUris
        {
            public const string BetweenDateTime = "http://schemas.authz.org/capl/operation#BetweenDateTime";

            public const string Contains = "http://schemas.authz.org/capl/operation#Contains";

            public const string Equal = "http://schemas.authz.org/capl/operation#Equal";

            public const string EqualDateTime = "http://schemas.authz.org/capl/operation#EqualDateTime";

            public const string EqualNumeric = "http://schemas.authz.org/capl/operation#EqualNumeric";

            public const string Exists = "http://schemas.authz.org/capl/operation#Exists";

            public const string GreaterThan = "http://schemas.authz.org/capl/operation#GreaterThan";

            public const string GreaterThanDateTime = "http://schemas.authz.org/capl/operation#GreaterThanDateTime";

            public const string GreaterThanOrEqual = "http://schemas.authz.org/capl/operation#GreaterThanOrEqual";

            public const string GreaterThanOrEqualDateTime =
                "http://schemas.authz.org/capl/operation#GreaterThanOrEqualDateTime";

            public const string LessThan = "http://schemas.authz.org/capl/operation#LessThan";

            public const string LessThanDateTime = "http://schemas.authz.org/capl/operation#LessThanDateTime";

            public const string LessThanOrEqual = "http://schemas.authz.org/capl/operation#LessThanOrEqual";

            public const string LessThanOrEqualDateTime =
                "http://schemas.authz.org/capl/operation#LessThanOrEqualDateTime";

            public const string NotEqual = "http://schemas.authz.org/capl/operation#NotEqual";
        }

        [SuppressMessage("Microsoft.Design", "CA1034:NestedTypesShouldNotBeVisible")]
        public static class TransformUris
        {
            public const string Add = "http://schemas.authz.org/capl/transform#Add";

            public const string Remove = "http://schemas.authz.org/capl/transform#Remove";

            public const string Replace = "http://schemas.authz.org/capl/transform#Replace";
        }
    }
}