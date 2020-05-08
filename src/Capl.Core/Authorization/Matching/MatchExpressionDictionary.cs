using System;
using System.Collections;
using System.Collections.Generic;

namespace Capl.Authorization.Matching
{
    public class MatchExpressionDictionary : IDictionary<string, MatchExpression>
    {
        private static MatchExpressionDictionary defaultInstance;

        private readonly Dictionary<string, MatchExpression> expressions;

        public MatchExpressionDictionary()
        {
            expressions = new Dictionary<string, MatchExpression>();
        }

        public static MatchExpressionDictionary Default
        {
            get
            {
                if (defaultInstance != null)
                {
                    return defaultInstance;
                }

                Action<Type, MatchExpressionDictionary> addOpAsType;
                //Action<MatchExpression, MatchExpressionDictionary> addOpAsInstance;
                MatchExpressionDictionary dict = new MatchExpressionDictionary();
                addOpAsType = (typeRef, op) =>
                {
                    MatchExpression matchExpression =
                        (MatchExpression)Activator.CreateInstance(Type.GetType(typeRef.FullName));
                    op.Add(matchExpression.Uri.ToString(), matchExpression);
                };

                //addOpAsInstance = (instance, op) =>
                //{
                //    op.Add(instance.Uri.ToString(), instance);
                //};

                addOpAsType(typeof(LiteralMatchExpression), dict);
                addOpAsType(typeof(PatternMatchExpression), dict);
                addOpAsType(typeof(ComplexTypeMatchExpression), dict);
                addOpAsType(typeof(UnaryMatchExpression), dict);

                defaultInstance = dict;
                return defaultInstance;
            }
        }

        #region IEnumerable<KeyValuePair<string,MatchExpression>> Members

        public IEnumerator<KeyValuePair<string, MatchExpression>> GetEnumerator()
        {
            return expressions.GetEnumerator();
        }

        #endregion IEnumerable<KeyValuePair<string,MatchExpression>> Members

        #region IEnumerable Members

        IEnumerator IEnumerable.GetEnumerator()
        {
            return expressions.GetEnumerator();
        }

        #endregion IEnumerable Members

        #region IDictionary<string,MatchExpression> Members

        public ICollection<string> Keys => expressions.Keys;

        public ICollection<MatchExpression> Values => expressions.Values;

        public MatchExpression this[string key]
        {
            get => expressions[key];
            set => expressions[key] = value;
        }

        /// <summary>
        ///     Adds a new match expression.
        /// </summary>
        /// <param name="key">The key that identifies the match expression.</param>
        /// <param name="value">The match expression instance.</param>
        public void Add(string key, MatchExpression value)
        {
            expressions.Add(key, value);
        }

        public bool ContainsKey(string key)
        {
            return expressions.ContainsKey(key);
        }

        public bool Remove(string key)
        {
            return expressions.Remove(key);
        }

        public bool TryGetValue(string key, out MatchExpression value)
        {
            return expressions.TryGetValue(key, out value);
        }

        #endregion IDictionary<string,MatchExpression> Members

        #region ICollection<KeyValuePair<string,MatchExpression>> Members

        public int Count => expressions.Count;

        public bool IsReadOnly => false;

        public void Add(KeyValuePair<string, MatchExpression> item)
        {
            ((ICollection<KeyValuePair<string, MatchExpression>>)expressions).Add(item);
        }

        public void Clear()
        {
            expressions.Clear();
        }

        public bool Contains(KeyValuePair<string, MatchExpression> item)
        {
            return ((ICollection<KeyValuePair<string, MatchExpression>>)expressions).Contains(item);
        }

        public void CopyTo(KeyValuePair<string, MatchExpression>[] array, int arrayIndex)
        {
            ((ICollection<KeyValuePair<string, MatchExpression>>)expressions).CopyTo(array, arrayIndex);
        }

        public bool Remove(KeyValuePair<string, MatchExpression> item)
        {
            return ((ICollection<KeyValuePair<string, MatchExpression>>)expressions).Remove(item);
        }

        #endregion ICollection<KeyValuePair<string,MatchExpression>> Members
    }
}