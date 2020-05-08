using System;
using System.Collections;
using System.Collections.Generic;
using System.Security.Claims;
using System.Xml;
using System.Xml.Serialization;

namespace Capl.Authorization
{
    [Serializable]
    [XmlSchemaProvider(null, IsAny = true)]
    public class TransformCollection : TransformBase, IList<Transform>, IXmlSerializable
    {
        private readonly List<Transform> transforms;

        public TransformCollection()
        {
            transforms = new List<Transform>();
        }

        #region IEnumerable<ScopeTransform> Members

        public IEnumerator<Transform> GetEnumerator()
        {
            return transforms.GetEnumerator();
        }

        #endregion IEnumerable<ScopeTransform> Members

        #region IEnumerable Members

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        #endregion IEnumerable Members

        public static TransformCollection Load(XmlReader reader)
        {
            TransformCollection transforms = new TransformCollection();
            transforms.ReadXml(reader);

            return transforms;
        }

        #region ITransform Members

        public override IEnumerable<Claim> TransformClaims(IEnumerable<Claim> claims)
        {
            _ = claims ?? throw new ArgumentNullException(nameof(claims));

            foreach (Transform transform in this)
                claims = transform.TransformClaims(claims);

            return claims;
        }

        #endregion ITransform Members

        #region IList<ScopeTransform> Members

        public Transform this[int index]
        {
            get => transforms[index];
            set => transforms[index] = value;
        }

        public int IndexOf(Transform item)
        {
            return transforms.IndexOf(item);
        }

        public void Insert(int index, Transform item)
        {
            transforms.Insert(index, item);
        }

        public void RemoveAt(int index)
        {
            transforms.RemoveAt(index);
        }

        #endregion IList<ScopeTransform> Members

        #region ICollection<ScopeTransform> Members

        public int Count => transforms.Count;

        public bool IsReadOnly => false;

        public void Add(Transform item)
        {
            transforms.Add(item);
        }

        public void Clear()
        {
            transforms.Clear();
        }

        public bool Contains(Transform item)
        {
            return transforms.Contains(item);
        }

        public void CopyTo(Transform[] array, int arrayIndex)
        {
            transforms.CopyTo(array, arrayIndex);
        }

        public bool Remove(Transform item)
        {
            return transforms.Remove(item);
        }

        #endregion ICollection<ScopeTransform> Members

        #region IXmlSerializable Members

        public override void ReadXml(XmlReader reader)
        {
            _ = reader ?? throw new ArgumentNullException(nameof(reader));

            reader.MoveToRequiredStartElement(AuthorizationConstants.Elements.Transforms);

            while (reader.Read())
            {
                if (reader.IsRequiredStartElement(AuthorizationConstants.Elements.Transform))
                {
                    Add(ClaimTransform.Load(reader));
                }

                if (reader.IsRequiredEndElement(AuthorizationConstants.Elements.Transforms))
                {
                    break;
                }
            }
        }

        public override void WriteXml(XmlWriter writer)
        {
            _ = writer ?? throw new ArgumentNullException(nameof(writer));

            if (Count == 0)
            {
                return;
            }

            writer.WriteStartElement(AuthorizationConstants.Elements.Transforms,
                AuthorizationConstants.Namespaces.Xmlns);

            foreach (ClaimTransform transform in this)
                transform.WriteXml(writer);

            writer.WriteEndElement();
        }

        #endregion IXmlSerializable Members
    }
}