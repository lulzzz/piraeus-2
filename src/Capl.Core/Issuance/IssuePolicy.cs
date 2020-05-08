using System;
using System.Collections.Generic;
using System.Security.Claims;
using System.Xml;
using System.Xml.Serialization;
using Capl.Authorization;

namespace Capl.Issuance
{
    [Serializable]
    [XmlSchemaProvider(null, IsAny = true)]
    public class IssuePolicy : IssuePolicyBase
    {
        private IssueMode mode;

        private string policyId;

        public IssuePolicy()
        {
            Transforms = new TransformCollection();
        }

        public IssueMode Mode
        {
            get => mode;
            set => mode = value;
        }

        public string PolicyId
        {
            get => policyId;
            set => policyId = value;
        }

        public TransformCollection Transforms
        {
            get; private set;
        }

        public static IssuePolicy Load(XmlReader reader)
        {
            IssuePolicy policy = new IssuePolicy();
            policy.ReadXml(reader);

            return policy;
        }

        public ClaimsIdentity Issue(ClaimsIdentity identity)
        {
            _ = identity ?? throw new ArgumentNullException(nameof(identity));

            return new ClaimsIdentity(Issue(identity.Claims));
        }

        public IEnumerable<Claim> Issue(IEnumerable<Claim> claims)
        {
            _ = claims ?? throw new ArgumentNullException(nameof(claims));

            ClaimsIdentity identity = new ClaimsIdentity(claims);
            List<Claim> clone = new List<Claim>(identity.Claims);

            List<Claim> copyList = new List<Claim>();
            foreach (Claim claim in clone)
                copyList.Add(claim);

            IEnumerable<Claim> inputClaims = new ClaimsIdentity(copyList).Claims;

            List<ICollection<Claim>> list = new List<ICollection<Claim>>();

            foreach (ClaimTransform transform in Transforms)
                clone = new List<Claim>(transform.TransformClaims(clone.ToArray()));

            if (mode == IssueMode.Unique)
            {
                foreach (Claim c in inputClaims)
                {
                    ICollection<Claim> claimSet = clone.FindAll(delegate (Claim claim) {
                        return c.Type == claim.Type && c.Value == claim.Value && c.Issuer == claim.Issuer;
                    });

                    if (claimSet.Count > 0)
                    {
                        list.Add(claimSet);
                    }
                }

                foreach (ICollection<Claim> claimCollection in list)
                {
                    foreach (Claim c in claimCollection)
                        clone.Remove(c);
                }
            }

            return clone;
        }

        public override void ReadXml(XmlReader reader)
        {
            _ = reader ?? throw new ArgumentNullException(nameof(reader));

            reader.MoveToRequiredStartElement(IssueConstants.Elements.IssuePolicy, IssueConstants.Namespaces.Xmlns);
            policyId = reader.GetOptionalAttribute(IssueConstants.Attributes.PolicyId);
            string mode = reader.GetOptionalAttribute(IssueConstants.Attributes.Mode);

            if (mode == IssueConstants.IssueModes.Aggregate)
            {
                this.mode = IssueMode.Aggregate;
            }
            else if (mode == IssueConstants.IssueModes.Unique)
            {
                this.mode = IssueMode.Unique;
            }
            else if (mode == null)
            {
                this.mode = IssueMode.Aggregate;
            }
            else
            {
                throw new IssueModeNotRecognizedException("Issue mode is not recognized.");
            }

            while (reader.Read())
            {
                if (reader.IsRequiredStartElement(AuthorizationConstants.Elements.Transforms))
                {
                    Transforms = TransformCollection.Load(reader);
                }

                if (reader.IsRequiredEndElement(IssueConstants.Elements.IssuePolicy, IssueConstants.Namespaces.Xmlns))
                {
                    break;
                }
            }

            reader.Read();
        }

        public override void WriteXml(XmlWriter writer)
        {
            _ = writer ?? throw new ArgumentNullException(nameof(writer));

            writer.WriteStartElement(IssueConstants.Elements.IssuePolicy, IssueConstants.Namespaces.Xmlns);

            if (mode == IssueMode.Aggregate)
            {
                writer.WriteAttributeString(IssueConstants.Attributes.Mode, IssueConstants.IssueModes.Aggregate);
            }
            else
            {
                writer.WriteAttributeString(IssueConstants.Attributes.Mode, IssueConstants.IssueModes.Unique);
            }

            if (policyId != null)
            {
                writer.WriteAttributeString(AuthorizationConstants.Attributes.PolicyId, policyId);
            }

            Transforms.WriteXml(writer);
            writer.WriteEndElement();
        }
    }
}