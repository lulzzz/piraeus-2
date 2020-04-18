using System;
using System.Collections.Generic;
using System.Security.Claims;
using System.Xml;
using System.Xml.Schema;
using System.Xml.Serialization;

namespace Capl.Authorization
{
    [Serializable]
    public abstract class TransformBase : IXmlSerializable
    {
        public virtual XmlSchema GetSchema()
        {
            return null;
        }

        public abstract void ReadXml(XmlReader reader);

        public abstract void WriteXml(XmlWriter writer);

        /// <summary>
        ///     Transforms a set of claims.
        /// </summary>
        /// <param name="claimSet">Set of claims to transform.</param>
        /// <returns>Transformed set of claims.</returns>
        public abstract IEnumerable<Claim> TransformClaims(IEnumerable<Claim> claims);
    }
}