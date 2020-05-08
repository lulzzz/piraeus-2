using System;
using System.Collections;
using System.Collections.Generic;

namespace Capl.Authorization.Transforms
{
    /// <summary>
    ///     A dictionary that contains transforms that can be identified by their respective URIs for their operations.
    /// </summary>
    public class TransformsDictionary : IDictionary<string, TransformAction>
    {
        private static TransformsDictionary defaultInstance;

        private readonly Dictionary<string, TransformAction> transforms;

        /// <summary>
        ///     Creates an instance of the object.
        /// </summary>
        public TransformsDictionary()
        {
            transforms = new Dictionary<string, TransformAction>();
        }

        public static TransformsDictionary Default
        {
            get
            {
                if (defaultInstance != null)
                {
                    return defaultInstance;
                }

                TransformsDictionary dict = new TransformsDictionary();

                static void addTranAsType(Type typeRef, TransformsDictionary op)
                {
                    TransformAction operation =
                        (TransformAction)Activator.CreateInstance(Type.GetType(typeRef.FullName));
                    op.Add(operation.Uri.ToString(), operation);
                }

                addTranAsType(typeof(AddTransformAction), dict);
                addTranAsType(typeof(RemoveTransformAction), dict);
                addTranAsType(typeof(ReplaceTransformAction), dict);

                defaultInstance = dict;
                return defaultInstance;
            }
        }

        #region IEnumerable<KeyValuePair<string,TransformAction>> Members

        public IEnumerator<KeyValuePair<string, TransformAction>> GetEnumerator()
        {
            return transforms.GetEnumerator();
        }

        #endregion IEnumerable<KeyValuePair<string,TransformAction>> Members

        #region IEnumerable Members

        IEnumerator IEnumerable.GetEnumerator()
        {
            return transforms.GetEnumerator();
        }

        #endregion IEnumerable Members

        #region IDictionary<string,TransformAction> Members

        public ICollection<string> Keys => transforms.Keys;

        public ICollection<TransformAction> Values => transforms.Values;

        public TransformAction this[string key]
        {
            get => transforms[key];
            set => transforms[key] = value;
        }

        /// <summary>
        ///     Adds a new transform.
        /// </summary>
        /// <param name="key">The key that identifies the tranform.</param>
        /// <param name="value">The transform instance.</param>
        public void Add(string key, TransformAction value)
        {
            transforms.Add(key, value);
        }

        public bool ContainsKey(string key)
        {
            return transforms.ContainsKey(key);
        }

        public bool Remove(string key)
        {
            return transforms.Remove(key);
        }

        public bool TryGetValue(string key, out TransformAction value)
        {
            return transforms.TryGetValue(key, out value);
        }

        #endregion IDictionary<string,TransformAction> Members

        #region ICollection<KeyValuePair<string,TransformAction>> Members

        public int Count => transforms.Count;

        public bool IsReadOnly => false;

        public void Add(KeyValuePair<string, TransformAction> item)
        {
            ((ICollection<KeyValuePair<string, TransformAction>>)transforms).Add(item);
        }

        public void Clear()
        {
            transforms.Clear();
        }

        public bool Contains(KeyValuePair<string, TransformAction> item)
        {
            return ((ICollection<KeyValuePair<string, TransformAction>>)transforms).Contains(item);
        }

        public void CopyTo(KeyValuePair<string, TransformAction>[] array, int arrayIndex)
        {
            ((ICollection<KeyValuePair<string, TransformAction>>)transforms).CopyTo(array, arrayIndex);
        }

        public bool Remove(KeyValuePair<string, TransformAction> item)
        {
            return ((ICollection<KeyValuePair<string, TransformAction>>)transforms).Remove(item);
        }

        #endregion ICollection<KeyValuePair<string,TransformAction>> Members
    }
}