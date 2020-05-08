using System;
using System.Runtime.Serialization;
using System.Xml;
using System.Xml.Schema;
using System.Xml.Serialization;

namespace Capl.Authorization
{
    /// <summary>
    ///     An abstract operation that performs an evaluation.
    /// </summary>
    [Serializable]
    [XmlSchemaProvider(null, IsAny = true)]
    public class EvaluationOperation : IXmlSerializable
    {
        /// <summary>
        ///     The claim value defined by the operation.
        /// </summary>
        private string claimValue;

        /// <summary>
        ///     The URI that identifies an operation.
        /// </summary>
        private Uri operationType;

        /// <summary>
        ///     Initializes a new instance of the <see cref="EvaluationOperation" /> class.
        /// </summary>
        public EvaluationOperation()
        {
        }

        // <summary>
        /// Initializes a new instance of the
        /// <see cref="EvaluationOperation" />
        /// class.
        /// </summary>
        /// <param name="operationType">The URI that identifies the operation.</param>
        /// <param name="claimValue">The claim value defined by the operation.</param>
        public EvaluationOperation(Uri operationType, string claimValue)
        {
            this.operationType = operationType;
            this.claimValue = claimValue;
        }

        /// <summary>
        ///     Gets or sets the claim value defined by the operation.
        /// </summary>
        /// <remarks>If the claim value is null, it implies a unary operation.</remarks>
        public string ClaimValue
        {
            get => claimValue;
            set => claimValue = value;
        }

        /// <summary>
        ///     Gets or sets the URI that identifies the operation.
        /// </summary>
        public Uri Type
        {
            get => operationType;
            set => operationType = value;
        }

        public static EvaluationOperation Load(XmlReader reader)
        {
            EvaluationOperation evalOperation = new EvaluationOperation();
            evalOperation.ReadXml(reader);

            return evalOperation;
        }

        #region IXmlSerializable Members

        /// <summary>
        ///     Provides a schema for an operation.
        /// </summary>
        /// <returns>Schema for an operation.</returns>
        /// <remarks>The methods always return null; the schema is provided by an XmlSchemaProvider.</remarks>
        public XmlSchema GetSchema()
        {
            return null;
        }

        /// <summary>
        ///     Reads the Xml of an operation.
        /// </summary>
        /// <param name="reader">An XmlReader for the operation.</param>
        public void ReadXml(XmlReader reader)
        {
            _ = reader ?? throw new ArgumentNullException(nameof(reader));

            reader.MoveToRequiredStartElement(AuthorizationConstants.Elements.Operation);
            operationType = new Uri(reader.GetRequiredAttribute(AuthorizationConstants.Attributes.Type));
            claimValue = reader.GetElementValue(AuthorizationConstants.Elements.Operation);

            if (!reader.IsRequiredEndElement(AuthorizationConstants.Elements.Operation))
            {
                throw new SerializationException("Unexpected element " + reader.LocalName);
            }
        }

        /// <summary>
        ///     Writes the Xml of an operation.
        /// </summary>
        /// <param name="writer">An XmlWriter for the operation.</param>
        public void WriteXml(XmlWriter writer)
        {
            _ = writer ?? throw new ArgumentNullException(nameof(writer));

            writer.WriteStartElement(AuthorizationConstants.Elements.Operation,
                AuthorizationConstants.Namespaces.Xmlns);
            writer.WriteAttributeString(AuthorizationConstants.Attributes.Type, operationType.ToString());
            writer.WriteString(claimValue);
            writer.WriteEndElement();
        }

        #endregion IXmlSerializable Members
    }
}