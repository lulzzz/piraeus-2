using System;
using System.Collections.Generic;
using System.Security.Claims;
using System.Xml;
using System.Xml.Serialization;

namespace Capl.Authorization
{
    /// <summary>
    ///     An authorization policy.
    /// </summary>
    [Serializable]
    [XmlSchemaProvider(null, IsAny = true)]
    public class AuthorizationPolicy : AuthorizationPolicyBase
    {
        /// </summary>
        public AuthorizationPolicy()
            : this(null)
        {
        }

        /// <summary>
        ///     Initializes a new instance of the <see cref="AuthorizationPolicy" /> class.
        /// </summary>
        /// <param name="authorizationRule">The authorization rule to be evaluated.</param>
        public AuthorizationPolicy(Term evaluationExpression)
            : this(evaluationExpression, null)
        {
        }

        /// <summary>
        ///     Initializes a new instance of the <see cref="AuthorizationPolicy" /> class.
        /// </summary>
        /// <param name="evaluationExpression">An evaluation expression.</param>
        /// <param name="policyUri">A unique URI for the authorization policy.</param>
        public AuthorizationPolicy(Term evaluationExpression, Uri policyId)
            : this(evaluationExpression, policyId, false)
        {
        }

        public AuthorizationPolicy(Term evaluationExpression, Uri policyId, bool delegation)
            : this(evaluationExpression, policyId, delegation, null)
        {
        }

        public AuthorizationPolicy(Term evaluationExpression, Uri policyId, bool delegation,
            TransformCollection transforms)
        {
            Expression = evaluationExpression;
            PolicyId = policyId;
            Delegation = delegation;

            if (transforms == null)
            {
                Transforms = new TransformCollection();
            }
            else
            {
                Transforms = transforms;
            }
        }

        public bool Delegation
        {
            get; set;
        }

        /// <summary>
        ///     Gets and sets an evaluation expression.
        /// </summary>
        public override Term Expression
        {
            get; set;
        }

        /// <summary>
        ///     Gets or sets an operation URI that identifies the policy.
        /// </summary>
        public Uri PolicyId
        {
            get; set;
        }

        /// <summary>
        ///     Gets transforms for the authorization policy.
        /// </summary>
        public override TransformCollection Transforms
        {
            get; internal set;
        }

        /// <summary>
        ///     Loads an authorization policy.
        /// </summary>
        /// <param name="reader">XmlReader instance of the authorization policy.</param>
        /// <returns>Capl.Authorization.AuthorizationPolicy</returns>
        public static AuthorizationPolicy Load(XmlReader reader)
        {
            AuthorizationPolicy policy = new AuthorizationPolicy();
            policy.ReadXml(reader);

            return policy;
        }

        public bool Evaluate(ClaimsIdentity identity)
        {
            _ = identity ?? throw new ArgumentNullException(nameof(identity));

            List<Claim> claims;
            if (!Delegation)
            {
                claims = new List<Claim>(identity.Claims);
            }
            else if (identity.Actor != null)
            {
                claims = new List<Claim>(identity.Actor.Claims);
            }
            else
            {
                return false;
            }

            foreach (ClaimTransform transform in Transforms)
                claims = new List<Claim>(transform.TransformClaims(claims.ToArray()));

            return Expression.Evaluate(claims);
        }

        #region IXmlSerializable Members

        /// <summary>
        ///     Reads an authorization policy.
        /// </summary>
        /// <param name="reader">XmlReader instance of the authorization policy.</param>
        public override void ReadXml(XmlReader reader)
        {
            _ = reader ?? throw new ArgumentNullException(nameof(reader));

            reader.MoveToRequiredStartElement(AuthorizationConstants.Elements.AuthorizationPolicy);
            PolicyId = new Uri(reader.GetOptionalAttribute(AuthorizationConstants.Attributes.PolicyId));
            string del = reader.GetOptionalAttribute(AuthorizationConstants.Attributes.Delegation);

            if (!string.IsNullOrEmpty(del))
            {
                Delegation = XmlConvert.ToBoolean(del);
            }

            while (reader.Read())
            {
                if (reader.IsRequiredStartElement(AuthorizationConstants.Elements.LogicalAnd) ||
                    reader.IsRequiredStartElement(AuthorizationConstants.Elements.LogicalOr) ||
                    reader.IsRequiredStartElement(AuthorizationConstants.Elements.Rule))
                {
                    Expression = Term.Load(reader);
                }

                if (reader.IsRequiredStartElement(AuthorizationConstants.Elements.Transforms))
                {
                    Transforms.ReadXml(reader);
                }

                if (reader.IsRequiredEndElement(AuthorizationConstants.Elements.AuthorizationPolicy))
                {
                    break;
                }
            }

            reader.Read();
        }

        /// <summary>
        ///     Writes an authorization policy an XmlWriter.
        /// </summary>
        /// <param name="writer">Writer to write the authorization policy.</param>
        public override void WriteXml(XmlWriter writer)
        {
            _ = writer ?? throw new ArgumentNullException(nameof(writer));

            writer.WriteStartElement(AuthorizationConstants.Elements.AuthorizationPolicy,
                AuthorizationConstants.Namespaces.Xmlns);

            if (PolicyId != null)
            {
                writer.WriteAttributeString(AuthorizationConstants.Attributes.PolicyId, PolicyId.ToString());
            }

            writer.WriteAttributeString(AuthorizationConstants.Attributes.Delegation, XmlConvert.ToString(Delegation));

            Expression.WriteXml(writer);

            if (Transforms != null)
            {
                Transforms.WriteXml(writer);
            }

            writer.WriteEndElement();
        }

        #endregion IXmlSerializable Members
    }
}