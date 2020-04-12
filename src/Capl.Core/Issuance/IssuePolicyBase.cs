/*
Claims Authorization Policy Langugage SDK ver. 3.0
Copyright (c) Matt Long labskunk@gmail.com
All rights reserved.
MIT License
*/

namespace Capl.Issuance
{
    using System;
    using System.IO;
    using System.Runtime.Serialization;
    using System.Xml;
    using System.Xml.Schema;
    using System.Xml.Serialization;

    [Serializable]
    [XmlSchemaProvider("GetSchema", IsAny = false)]
    [KnownType(typeof(IssuePolicy))]
    public abstract class IssuePolicyBase : IXmlSerializable
    {
        public static XmlQualifiedName GetSchema(XmlSchemaSet schemaSet)
        {
            _ = schemaSet ?? throw new ArgumentNullException(nameof(schemaSet));

            using (StringReader reader = new StringReader(Capl.Properties.Resources.IssuePolicySchema))
            {
                XmlSchema schema = XmlSchema.Read(reader, null);
                schemaSet.Add(schema);
            }

            using (StringReader reader = new StringReader(Capl.Properties.Resources.AuthorizationPolicySchema))
            {
                XmlSchema schema = XmlSchema.Read(reader, null);
                schemaSet.Add(schema);
            }

            return new XmlQualifiedName("IssuePolicyType", IssueConstants.Namespaces.Xmlns);
        }

        public virtual XmlSchema GetSchema()
        {
            return null;
        }

        public abstract void ReadXml(XmlReader reader);

        public abstract void WriteXml(XmlWriter writer);
    }
}