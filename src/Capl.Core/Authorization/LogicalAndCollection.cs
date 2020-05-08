using System;
using System.Collections.Generic;
using System.Security.Claims;
using System.Xml;
using System.Xml.Serialization;

namespace Capl.Authorization
{
    /// <summary>
    ///     Performs a logical conjunction (Logical AND) on a collection of objects implementing IEvaluate.
    /// </summary>
    /// <remarks>
    ///     The collection of objects all implement the IEvaluate interface. Therefore, the collection
    ///     of objects must also inherit one of the abstract classes Scope or LogicalConnectiveCollection.
    /// </remarks>
    [Serializable]
    [XmlSchemaProvider(null, IsAny = true)]
    public class LogicalAndCollection : LogicalConnectiveCollection
    {
        public static new LogicalConnectiveCollection Load(XmlReader reader)
        {
            LogicalAndCollection lac = new LogicalAndCollection();
            lac.ReadXml(reader);

            return lac;

            //Adds an object to the end of the Capl.Authorization.LogicalAndCollection
            //Removes all element from the Capl.Authorization.LogicalAndCollection
            //Determines whether an element in the Capl.Authorization.LogicalAndCollection
            //Copies the entire Capl.Authorization.LogicalAndCollection to a compatible one-dimensional array System.Array, starting at the specified index of the target array.
            //Gets the number of element actually contained in the Capl.Authorization.LogicalAndCollection
            //Returns an enumerator that iterates through the Capl.Authorization.LogicalAndCollection
            // Inserts an elmenent into the Capl.Authorization.LogicalAndCollection at the specified location.
            //Remmoves the first occurrence of a specifed object from the Capl.Authorization.LogicalAndCollection
            //Removes the element at a specified index from the Capl.Authorization.LogicalAndCollection
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
                if (!eval)
                {
                    if (!Evaluates)
                    {
                        return true;
                    }

                    return false;
                }
            }

            return Evaluates;
        }

        /// <summary>
        ///     Reads the Xml of a logical AND.
        /// </summary>
        /// <param name="reader">An XmlReader for a logical AND.</param>
        public override void ReadXml(XmlReader reader)
        {
            _ = reader ?? throw new ArgumentNullException(nameof(reader));

            reader.MoveToRequiredStartElement(AuthorizationConstants.Elements.LogicalAnd);
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
                    Add(Load(reader));
                }

                if (reader.IsRequiredStartElement(AuthorizationConstants.Elements.LogicalOr))
                {
                    Add(LogicalOrCollection.Load(reader));
                }

                if (reader.IsRequiredStartElement(AuthorizationConstants.Elements.Rule))
                {
                    Add(Rule.Load(reader));
                }

                if (reader.IsRequiredEndElement(AuthorizationConstants.Elements.LogicalAnd))
                {
                    return;
                    //break;
                }
            }

            reader.Read();
        }

        /// <summary>
        ///     Writes the Xml of a logical AND.
        /// </summary>
        /// <param name="writer">An XmlWriter for a logical AND.</param>
        public override void WriteXml(XmlWriter writer)
        {
            _ = writer ?? throw new ArgumentNullException(nameof(writer));

            writer.WriteStartElement(AuthorizationConstants.Elements.LogicalAnd,
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