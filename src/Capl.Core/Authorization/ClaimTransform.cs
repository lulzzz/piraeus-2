using System;
using System.Collections.Generic;
using System.Security.Claims;
using System.Xml;
using Capl.Authorization.Matching;
using Capl.Authorization.Transforms;

namespace Capl.Authorization
{
    /// <summary>
    ///     The abstract scope of a transform.
    /// </summary>
    [Serializable]
    public class ClaimTransform : Transform
    {
        public ClaimTransform()
            : this(null, null, null, null, null)
        {
        }

        public ClaimTransform(Uri transformType, LiteralClaim targetClaim)
            : this(null, transformType, null, targetClaim, null)
        {
        }

        public ClaimTransform(Uri transformType, Match matchExpression)
            : this(null, transformType, matchExpression, null, null)
        {
        }

        public ClaimTransform(Uri transformType, Match matchExpression, LiteralClaim targetClaim)
            : this(null, transformType, matchExpression, targetClaim, null)
        {
        }

        public ClaimTransform(Uri transformId, Uri transformType, Match matchExpression, LiteralClaim targetClaim,
            Term evaluationExpression)
        {
            TransformID = transformId;
            Type = transformType;
            MatchExpression = matchExpression;
            TargetClaim = targetClaim;
            Expression = evaluationExpression;
        }

        public override Term Expression
        {
            get; set;
        }

        public override Match MatchExpression
        {
            get; set;
        }

        public override LiteralClaim TargetClaim
        {
            get; set;
        }

        public override Uri TransformID
        {
            get; set;
        }

        public override Uri Type
        {
            get; set;
        }

        public static ClaimTransform Load(XmlReader reader)
        {
            ClaimTransform trans = new ClaimTransform();
            trans.ReadXml(reader);

            return trans;
        }

        #region ITransform Members

        /// <summary>
        ///     Executes the transform.
        /// </summary>
        /// <param name="claimSet">Input set of claims to transform.</param>
        /// <returns>Transformed set of claims.</returns>
        public override IEnumerable<Claim> TransformClaims(IEnumerable<Claim> claims)
        {
            _ = claims ?? throw new ArgumentNullException(nameof(claims));

            IList<Claim> matchedClaims = null;
            IEnumerable<Claim> transformedClaims = null;
            TransformAction action = TransformAction.Create(Type, null);

            if (MatchExpression != null)
            {
                MatchExpression
                    matcher = MatchExpressionDictionary.Default[
                        MatchExpression.Type
                            .ToString()]; //CaplConfigurationManager.MatchExpressions[this.MatchExpression.Type.ToString()];
                matchedClaims = matcher.MatchClaims(claims, MatchExpression.ClaimType, MatchExpression.Value);
            }

            bool eval;
            if (Expression == null)
            {
                eval = true;
            }
            else
            {
                eval = Expression.Evaluate(claims);
            }

            if (eval)
            {
                transformedClaims = action.Execute(claims, matchedClaims, TargetClaim);
            }

            if (transformedClaims != null)
            {
                return transformedClaims;
            }

            return claims;
        }

        #endregion ITransform Members

        #region IXmlSerializable Members

        /// <summary>
        ///     Reads the Xml of a scope transform.
        /// </summary>
        /// <param name="reader">An XmlReader for a scope transform.</param>
        public override void ReadXml(XmlReader reader)
        {
            _ = reader ?? throw new ArgumentNullException(nameof(reader));

            reader.MoveToRequiredStartElement(AuthorizationConstants.Elements.Transform);

            Type = new Uri(reader.GetRequiredAttribute(AuthorizationConstants.Attributes.Type));

            while (reader.Read())
            {
                if (reader.IsRequiredStartElement(AuthorizationConstants.Elements.Match))
                {
                    MatchExpression = Match.Load(reader);
                }

                if (reader.IsRequiredStartElement(AuthorizationConstants.Elements.TargetClaim))
                {
                    TargetClaim = new LiteralClaim
                    {
                        ClaimType = reader.GetRequiredAttribute(AuthorizationConstants.Attributes.ClaimType)
                    };

                    if (!reader.IsEmptyElement)
                    {
                        TargetClaim.ClaimValue = reader.GetElementValue(AuthorizationConstants.Elements.TargetClaim);
                    }
                }

                if (reader.LocalName == AuthorizationConstants.Elements.Rule ||
                    reader.LocalName == AuthorizationConstants.Elements.LogicalAnd ||
                    reader.LocalName == AuthorizationConstants.Elements.LogicalOr)
                {
                    Expression = Term.Load(reader);
                }

                if (reader.IsRequiredEndElement(AuthorizationConstants.Elements.Transform))
                {
                    break;
                }
            }
        }

        /// <summary>
        ///     Writes the Xml of a scope transform.
        /// </summary>
        /// <param name="writer">An XmlWriter for the scope transform.</param>
        public override void WriteXml(XmlWriter writer)
        {
            _ = writer ?? throw new ArgumentNullException(nameof(writer));

            writer.WriteStartElement(AuthorizationConstants.Elements.Transform,
                AuthorizationConstants.Namespaces.Xmlns);

            if (TransformID != null)
            {
                writer.WriteAttributeString(AuthorizationConstants.Attributes.TransformId, TransformID.ToString());
            }

            writer.WriteAttributeString(AuthorizationConstants.Attributes.Type, Type.ToString());

            if (MatchExpression != null)
            {
                MatchExpression.WriteXml(writer);
            }

            if (TargetClaim != null)
            {
                writer.WriteStartElement(AuthorizationConstants.Elements.TargetClaim,
                    AuthorizationConstants.Namespaces.Xmlns);
                writer.WriteAttributeString(AuthorizationConstants.Attributes.ClaimType, TargetClaim.ClaimType);

                if (!string.IsNullOrEmpty(TargetClaim.ClaimValue))
                {
                    writer.WriteString(TargetClaim.ClaimValue);
                }

                writer.WriteEndElement();
            }

            if (Expression != null)
            {
                Expression.WriteXml(writer);
            }

            writer.WriteEndElement();
        }

        #endregion IXmlSerializable Members
    }
}