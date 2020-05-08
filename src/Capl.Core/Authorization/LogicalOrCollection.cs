using System;
using System.Collections.Generic;
using System.Security.Claims;
using System.Xml;
using System.Xml.Serialization;

namespace Capl.Authorization
{
    /// <summary>
    ///     Performs a logical disjunction (Logical OR) on a collection of objects implementing IEvaluate.
    /// </summary>
    /// <remarks>
    ///     The collection of objects all implement the IEvaluate interface. Therefore, the collection
    ///     of objects must also inherit one of the abstract classes Scope or LogicalConnectiveCollection.
    /// </remarks>
    [Serializable]
    [XmlSchemaProvider(null, IsAny = true)]
    public class LogicalOrCollection : LogicalConnectiveCollection
    {
        public static new LogicalConnectiveCollection Load(XmlReader reader)
        {
            LogicalOrCollection loc = new LogicalOrCollection();
            loc.ReadXml(reader);

            return loc;
        }

        /// <summary>
        ///     Evaluates a set of claims.
        /// </summary>
        /// <param name="claimSet">The set of claims to be evaluated.</param>
        /// <returns>True, if the evaluation is true; otherwise false.</returns>
        public override bool Evaluate(IEnumerable<Claim> claims)
        {
            _ = claims ?? throw new ArgumentNullException(nameof(claims));

            foreach (Term item in this)
            {
                bool eval = item.Evaluate(claims);
                if (Evaluates)
                {
                    if (eval)
                    {
                        return true;
                    }
                }
                else
                {
                    if (eval)
                    {
                        return false;
                    }
                }
            }

            return !Evaluates;
        }

        /// <summary>
        ///     Reads the Xml of a logical OR.
        /// </summary>
        /// <param name="reader">An XmlReader for a logical OR.</param>
        public override void ReadXml(XmlReader reader)
        {
            _ = reader ?? throw new ArgumentNullException(nameof(reader));

            reader.MoveToRequiredStartElement(AuthorizationConstants.Elements.LogicalOr);

            string evaluates = reader.GetOptionalAttribute(AuthorizationConstants.Attributes.Evaluates);
            string termId = reader.GetOptionalAttribute(AuthorizationConstants.Attributes.TermId);

            if (!string.IsNullOrEmpty(termId))
            {
                TermId = new Uri(termId);
            }

            if (!string.IsNullOrEmpty(evaluates))
            {
                Evaluates = XmlConvert.ToBoolean(evaluates);
            }

            while (reader.Read())
            {
                if (reader.IsRequiredStartElement(AuthorizationConstants.Elements.LogicalAnd))
                {
                    Add(LogicalAndCollection.Load(reader));
                }

                if (reader.IsRequiredStartElement(AuthorizationConstants.Elements.LogicalOr))
                {
                    Add(Load(reader));
                }

                if (reader.IsRequiredStartElement(AuthorizationConstants.Elements.Rule))
                {
                    Add(Rule.Load(reader));
                }

                if (reader.IsRequiredEndElement(AuthorizationConstants.Elements.LogicalOr))
                {
                    return;
                    //break;
                }
            }

            reader.Read();
        }

        /// <summary>
        ///     Writes the Xml of a logical OR.
        /// </summary>
        /// <param name="writer">An XmlWriter for a logical OR.</param>
        public override void WriteXml(XmlWriter writer)
        {
            _ = writer ?? throw new ArgumentNullException(nameof(writer));

            writer.WriteStartElement(AuthorizationConstants.Elements.LogicalOr,
                AuthorizationConstants.Namespaces.Xmlns);

            if (TermId != null)
            {
                writer.WriteAttributeString(AuthorizationConstants.Attributes.TermId, TermId.ToString());
            }

            writer.WriteAttributeString(AuthorizationConstants.Attributes.Evaluates, XmlConvert.ToString(Evaluates));

            foreach (Term eval in this)
                eval.WriteXml(writer);

            writer.WriteEndElement();
        }
    }
}