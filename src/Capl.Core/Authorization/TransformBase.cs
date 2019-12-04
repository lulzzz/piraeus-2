﻿/*
Claims Authorization Policy Langugage SDK ver. 3.0 
Copyright (c) Matt Long labskunk@gmail.com 
All rights reserved. 
MIT License
*/

namespace Capl.Authorization
{
    using System;
    using System.Collections.Generic;
    using System.Security.Claims;
    using System.Xml;
    using System.Xml.Schema;
    using System.Xml.Serialization;

    [Serializable]
    public abstract class TransformBase : IXmlSerializable
    {

        /// <summary>
        /// Transforms a set of claims.
        /// </summary>
        /// <param name="claimSet">Set of claims to transform.</param>
        /// <returns>Transformed set of claims.</returns>
        public abstract IEnumerable<Claim> TransformClaims(IEnumerable<Claim> claims);

        public virtual XmlSchema GetSchema()
        {
            return null;
        }

        public abstract void ReadXml(XmlReader reader);

        public abstract void WriteXml(XmlWriter writer);
    }
}
