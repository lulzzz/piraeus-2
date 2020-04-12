﻿namespace Capl.Authorization
{
    using Capl.Authorization.Matching;
    using System;
    using System.Collections.Generic;
    using System.Security.Claims;
    using System.Xml;
    using System.Xml.Serialization;

    /// <summary>
    /// A rule that performs an evaluation.
    /// </summary>
    [Serializable]
    [XmlSchemaProvider(null, IsAny = true)]
    public class Rule : Term
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="Rule"/> class.
        /// </summary>
        public Rule()
            : this(null, null, true)
        {
        }

        /// <summary>
        ///  Initializes a new instance of the <see cref="Rule"/> class.
        /// </summary>
        /// <param name="matchType">An expression to match claims for the operation.</param>
        /// <param name="operation">The operation that performs an evaluation.</param>
        public Rule(Match matchType, EvaluationOperation operation)
            : this(matchType, operation, true)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="Rule"/> class.
        /// </summary>
        /// <param name="matchType">An expression to match claims for the operation.</param>
        /// <param name="operation">The operation that performs an evaluation.</param>
        /// <param name="evaluates">The truthful evaluation for the rule.</param>
        public Rule(Match matchType, EvaluationOperation operation, bool evaluates)
        {
            this.MatchExpression = matchType;
            this.Operation = operation;
            this.Evaluates = evaluates;
        }

        public string Issuer { get; set; }

        /// <summary>
        /// Gets or sets an expression that matches claims to be evaluted.  The matching claim values
        ///  represent the left hand side operand vlaue of the authorization operation.
        /// </summary>
        public Match MatchExpression { get; set; }

        /// <summary>
        /// Gets or sets the name of the authorization operation.
        /// </summary>
        public EvaluationOperation Operation { get; set; }

        public override Uri TermId { get; set; }

        public static new Rule Load(XmlReader reader)
        {
            Rule rule = new Rule();
            rule.ReadXml(reader);

            return rule;
        }

        #region IEvaluationRule Members

        /// <summary>
        /// Gets or sets an expression
        /// </summary>
        /// <remarks>If the evaluation of the operation matches the Evaluates property, then the evaluation of the rule is true; otherwise false.</remarks>
        public override bool Evaluates { get; set; }

        /// <summary>
        /// Evaluates a set of claims using the authorization operation.
        /// </summary>
        /// <param name="claimSet">The set of claims to be evaluated.</param>
        /// <returns>True if the set of claims evaluates to true; otherwise false.</returns>
        public override bool Evaluate(IEnumerable<Claim> claims)
        {
            _ = claims ?? throw new ArgumentNullException(nameof(claims));

            MatchExpression exp = Capl.Authorization.Matching.MatchExpression.Create(this.MatchExpression.Type, null);

            IList<Claim> list = exp.MatchClaims(claims, MatchExpression.ClaimType, MatchExpression.Value);

            if (list.Count == 0)
            {
                return !this.MatchExpression.Required;
            }

            if (this.Issuer != null)
            {
                int count = list.Count;
                for (int index = 0; index < count; index++)
                {
                    if (list[index].Issuer != this.Issuer)
                    {
                        list.Remove(list[index]);
                        index--;
                        count--;
                    }
                }
            }

            Operations.Operation operation = Operations.Operation.Create(Operation.Type, null);

            foreach (Claim claim in list)
            {
                bool eval = operation.Execute(claim.Value, this.Operation.ClaimValue);

                if (this.Evaluates && eval)
                {
                    return true;
                }

                if (!this.Evaluates && eval)
                {
                    return false;
                }
            }

            return !this.Evaluates;
        }

        #endregion IEvaluationRule Members

        #region IXmlSerializable Members

        public override void ReadXml(XmlReader reader)
        {
            _ = reader ?? throw new ArgumentNullException(nameof(reader));

            reader.MoveToRequiredStartElement(AuthorizationConstants.Elements.Rule);
            string termId = reader.GetOptionalAttribute(AuthorizationConstants.Attributes.TermId);

            if (!string.IsNullOrEmpty(termId))
            {
                this.TermId = new Uri(termId);
            }

            this.Issuer = reader.GetOptionalAttribute(AuthorizationConstants.Attributes.Issuer);

            string evaluates = reader.GetOptionalAttribute(AuthorizationConstants.Attributes.Evaluates);

            if (!string.IsNullOrEmpty(evaluates))
            {
                this.Evaluates = XmlConvert.ToBoolean(evaluates);
            }

            while (reader.Read())
            {
                if (reader.IsRequiredStartElement(AuthorizationConstants.Elements.Operation))
                {
                    this.Operation = EvaluationOperation.Load(reader);
                }

                if (reader.IsRequiredStartElement(AuthorizationConstants.Elements.Match))
                {
                    this.MatchExpression = Match.Load(reader);
                }

                if (reader.IsRequiredEndElement(AuthorizationConstants.Elements.Rule))
                {
                    return;
                    //break;
                }
            }

            reader.Read();
        }

        /// <summary>
        /// Writes the Xml of a evaluation rule.
        /// </summary>
        /// <param name="writer">An XmlWriter for the evaluation rule.</param>
        public override void WriteXml(XmlWriter writer)
        {
            _ = writer ?? throw new ArgumentNullException(nameof(writer));

            writer.WriteStartElement(AuthorizationConstants.Elements.Rule, AuthorizationConstants.Namespaces.Xmlns);

            if (this.Issuer != null)
            {
                writer.WriteAttributeString(AuthorizationConstants.Attributes.Issuer, this.Issuer);
            }

            if (this.TermId != null)
            {
                writer.WriteAttributeString(AuthorizationConstants.Attributes.TermId, this.TermId.ToString());
            }

            writer.WriteAttributeString(AuthorizationConstants.Attributes.Evaluates, XmlConvert.ToString(this.Evaluates));

            this.Operation.WriteXml(writer);

            this.MatchExpression.WriteXml(writer);

            writer.WriteEndElement();
        }

        #endregion IXmlSerializable Members
    }
}