using System;
using System.Collections.Generic;
using System.Runtime.Serialization;
using System.Security.Claims;
using System.Xml;
using System.Xml.Schema;
using System.Xml.Serialization;

namespace Capl.Authorization
{
    /// <summary>
    ///     An interface used to evaluate a set of claims or a collection of claims and set the truthful evaluation
    ///     for both.
    /// </summary>
    /// <remarks>The abstract LogicalConnectiveCollection implements this interface.</remarks>
    [Serializable]
    public abstract class Term : IXmlSerializable
    {
        /// <summary>
        ///     Get or sets the truthful evaluation.
        /// </summary>
        public abstract bool Evaluates
        {
            get; set;
        }

        public abstract Uri TermId
        {
            get; set;
        }

        public static Term Load(XmlReader reader)
        {
            _ = reader ?? throw new ArgumentNullException(nameof(reader));

            Term evalExp = null;

            reader.MoveToStartElement();

            if (reader.IsRequiredStartElement(AuthorizationConstants.Elements.Rule))
            {
                Rule rule = new Rule();
                rule.ReadXml(reader);
                evalExp = rule;
            }

            if (reader.IsRequiredStartElement(AuthorizationConstants.Elements.LogicalAnd))
            {
                LogicalAndCollection logicalAnd = new LogicalAndCollection();
                logicalAnd.ReadXml(reader);
                evalExp = logicalAnd;
            }

            if (reader.IsRequiredStartElement(AuthorizationConstants.Elements.LogicalOr))
            {
                LogicalOrCollection logicalOr = new LogicalOrCollection();
                logicalOr.ReadXml(reader);
                evalExp = logicalOr;
            }

            if (evalExp != null)
            {
                return evalExp;
            }

            throw new SerializationException("Invalid evaluation expression element.");
        }

        public abstract bool Evaluate(IEnumerable<Claim> claims);

        public virtual XmlSchema GetSchema()
        {
            return null;
        }

        public abstract void ReadXml(XmlReader reader);

        public abstract void WriteXml(XmlWriter writer);
    }
}