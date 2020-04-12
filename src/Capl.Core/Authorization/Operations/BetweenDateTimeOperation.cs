
namespace Capl.Authorization.Operations
{
    using System;
    using System.Xml;

    public class BetweenDateTimeOperation : Operation
    {
        public static Uri OperationUri => new Uri(AuthorizationConstants.OperationUris.BetweenDateTime);

        public override Uri Uri => new Uri(AuthorizationConstants.OperationUris.BetweenDateTime);

        public override bool Execute(string left, string right)
        {
            ///the LHS is ignored and the RHS using a normalized string containing 2 xsd:dateTime values.
            ///the current time should be between the 2 dateTime values

            string[] parts = right.Split(new char[] { ' ' });
            DateTime startDate = XmlConvert.ToDateTime(parts[0], XmlDateTimeSerializationMode.Utc);
            DateTime endDate = XmlConvert.ToDateTime(parts[1], XmlDateTimeSerializationMode.Utc);
            DateTime now = DateTime.Now;

            return (startDate <= now && endDate >= now);
        }
    }
}