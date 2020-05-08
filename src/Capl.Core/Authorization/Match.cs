using System;
using System.Runtime.Serialization;
using System.Xml;
using System.Xml.Schema;
using System.Xml.Serialization;

namespace Capl.Authorization
{
    [Serializable]
    public class Match : IXmlSerializable
    {
        public Match()
        {
        }

        public Match(Uri matchExpressionUri, string claimType)
            : this(matchExpressionUri, claimType, true, null)
        {
        }

        public Match(Uri matchExpressionUri, string claimType, bool required)
            : this(matchExpressionUri, claimType, required, null)
        {
        }

        public Match(Uri matchExpressionUri, string claimType, bool required, string value)
        {
            Type = matchExpressionUri;
            ClaimType = claimType;
            Required = required;
            Value = value;
        }

        /// <summary>
        ///     Gets or sets a value for a claim type.
        /// </summary>
        public string ClaimType
        {
            get; set;
        }

        /// <summary>
        ///     Gets or sets a value indicating whether matching is required for evaluation.
        /// </summary>
        public bool Required
        {
            get; set;
        }

        /// <summary>
        ///     Gets or set the type of match expression
        /// </summary>
        public Uri Type
        {
            get; set;
        }

        /// <summary>
        ///     Gets or sets a value for claim.
        /// </summary>
        public string Value
        {
            get; set;
        }

        public static Match Load(XmlReader reader)
        {
            Match match = new Match();
            match.ReadXml(reader);
            return match;
        }

        public XmlSchema GetSchema()
        {
            return null;
        }

        public void ReadXml(XmlReader reader)
        {
            _ = reader ?? throw new ArgumentNullException(nameof(reader));

            reader.MoveToRequiredStartElement(AuthorizationConstants.Elements.Match);
            ClaimType = reader.GetOptionalAttribute(AuthorizationConstants.Attributes.ClaimType);
            Type = new Uri(reader.GetRequiredAttribute(AuthorizationConstants.Attributes.Type));
            string required = reader.GetOptionalAttribute(AuthorizationConstants.Attributes.Required);

            if (string.IsNullOrEmpty(required))
            {
                Required = true;
            }
            else
            {
                Required = XmlConvert.ToBoolean(required);
            }

            Value = reader.GetElementValue(AuthorizationConstants.Elements.Match);

            if (!reader.IsRequiredEndElement(AuthorizationConstants.Elements.Match))
            {
                throw new SerializationException(string.Format("Unexpected element {0}", reader.LocalName));
            }
        }

        public void WriteXml(XmlWriter writer)
        {
            _ = writer ?? throw new ArgumentNullException(nameof(writer));

            writer.WriteStartElement(AuthorizationConstants.Elements.Match, AuthorizationConstants.Namespaces.Xmlns);

            writer.WriteAttributeString(AuthorizationConstants.Attributes.Type, Type.ToString());
            writer.WriteAttributeString(AuthorizationConstants.Attributes.ClaimType, ClaimType);
            writer.WriteAttributeString(AuthorizationConstants.Attributes.Required, XmlConvert.ToString(Required));

            if (!string.IsNullOrEmpty(Value))
            {
                writer.WriteString(Value);
            }

            writer.WriteEndElement();
        }
    }
}