using System;
using System.Collections;
using System.Collections.Generic;

namespace Capl.Authorization.Operations
{
    /// <summary>
    ///     A dictionary that contains operations that can be identified by their respective URIs for their operations.
    /// </summary>
    public class OperationsDictionary : IDictionary<string, Operation>
    {
        private static OperationsDictionary defaultInstance;

        /// <summary>
        ///     Local dictionary of operations.
        /// </summary>
        private readonly Dictionary<string, Operation> operations;

        /// <summary>
        ///     Initializes a new instance of the <see cref="OperationsDictionary" /> class.
        /// </summary>
        public OperationsDictionary()
        {
            operations = new Dictionary<string, Operation>();
        }

        public static OperationsDictionary Default
        {
            get
            {
                if (defaultInstance != null)
                {
                    return defaultInstance;
                }

                Action<Type, OperationsDictionary> addOpAsType;
                Action<Operation, OperationsDictionary> addOpAsInstance;
                OperationsDictionary dict = new OperationsDictionary();

                addOpAsType = (typeRef, op) =>
                {
                    Operation operation = (Operation)Activator.CreateInstance(Type.GetType(typeRef.FullName));
                    op.Add(operation.Uri.ToString(), operation);
                };

                addOpAsInstance = (instance, op) =>
                {
                    op.Add(instance.Uri.ToString(), instance);
                };

                addOpAsType(typeof(EqualDateTimeOperation), dict);
                addOpAsType(typeof(EqualNumericOperation), dict);
                addOpAsType(typeof(EqualOperation), dict);
                addOpAsType(typeof(NotEqualOperation), dict);
                addOpAsType(typeof(ExistsOperation), dict);
                addOpAsType(typeof(GreaterThanDateTimeOperation), dict);
                addOpAsType(typeof(GreaterThanOperation), dict);
                addOpAsType(typeof(GreaterThanOrEqualDateTimeOperation), dict);
                addOpAsType(typeof(GreaterThanOrEqualOperation), dict);
                addOpAsType(typeof(LessThanDateTimeOperation), dict);
                addOpAsType(typeof(LessThanOperation), dict);
                addOpAsType(typeof(LessThanOrEqualDateTimeOperation), dict);
                addOpAsType(typeof(LessThanOrEqualOperation), dict);
                addOpAsType(typeof(BetweenDateTimeOperation), dict);
                addOpAsType(typeof(ContainsOperation), dict);

                defaultInstance = dict;

                return defaultInstance;
            }
        }

        /// <summary>
        ///     Gets the number of items in the dictionary.
        /// </summary>
        public int Count => operations.Count;

        /// <summary>
        ///     Gets a value indicating whether the dictionary is read-only.
        /// </summary>
        public bool IsReadOnly => false;

        /// <summary>
        ///     Gets a collection of keys.
        /// </summary>
        public ICollection<string> Keys => operations.Keys;

        /// <summary>
        ///     Gets a collection of values.
        /// </summary>
        public ICollection<Operation> Values => operations.Values;

        /// <summary>
        ///     Indexer to get or set a value.
        /// </summary>
        /// <param name="key">The key of the value of get or set.</param>
        /// <returns>The value of the key.</returns>
        public Operation this[string key]
        {
            get => operations[key];
            set => operations[key] = value;
        }

        /// <summary>
        ///     Adds a new operation.
        /// </summary>
        /// <param name="key">The key that identifies the operation.</param>
        /// <param name="value">The operation instance.</param>
        public void Add(string key, Operation value)
        {
            operations.Add(key, value);
        }

        /// <summary>
        ///     Adds a key and value pair to the dictionary.
        /// </summary>
        /// <param name="item">The key-value pair to add.</param>
        public void Add(KeyValuePair<string, Operation> item)
        {
            ((ICollection<KeyValuePair<string, Operation>>)operations).Add(item);
        }

        /// <summary>
        ///     Clears the items from the dictionary.
        /// </summary>
        public void Clear()
        {
            operations.Clear();
        }

        /// <summary>
        ///     Determines if a key-value pair exists in the dictionary.
        /// </summary>
        /// <param name="item">Key-value pair to determine existence.</param>
        /// <returns>True, if the item exist; otherwise false.</returns>
        public bool Contains(KeyValuePair<string, Operation> item)
        {
            return ((ICollection<KeyValuePair<string, Operation>>)operations).Contains(item);
        }

        /// <summary>
        ///     Determines whether an operation's key exists.
        /// </summary>
        /// <param name="key">The key that identifies the operation.</param>
        /// <returns>True, if the key exists; otherwise false.</returns>
        public bool ContainsKey(string key)
        {
            return operations.ContainsKey(key);
        }

        /// <summary>
        ///     Copies values to an array.
        /// </summary>
        /// <param name="array">Array to copy items.</param>
        /// <param name="arrayIndex">Index to begin the copy.</param>
        public void CopyTo(KeyValuePair<string, Operation>[] array, int arrayIndex)
        {
            ((ICollection<KeyValuePair<string, Operation>>)operations).CopyTo(array, arrayIndex);
        }

        /// <summary>
        ///     Gets an enumerator for the dictionary.
        /// </summary>
        /// <returns>An enumerator of key-value pairs.</returns>
        public IEnumerator<KeyValuePair<string, Operation>> GetEnumerator()
        {
            return operations.GetEnumerator();
        }

        /// <summary>
        ///     Get an enumerator for the dictionary.
        /// </summary>
        /// <returns>An enumerator for the dictionary.</returns>
        IEnumerator IEnumerable.GetEnumerator()
        {
            return operations.GetEnumerator();
        }

        /// <summary>
        ///     Removes the operation.
        /// </summary>
        /// <param name="key">Key of the operation.</param>
        /// <returns>True, if the operation is removed; otherwise false.</returns>
        public bool Remove(string key)
        {
            return operations.Remove(key);
        }

        /// <summary>
        ///     Removes a key-value pair from the dictionary.
        /// </summary>
        /// <param name="item">Item to remove.</param>
        /// <returns>True, if the key-value pair is removed; otherwise false.</returns>
        public bool Remove(KeyValuePair<string, Operation> item)
        {
            return ((ICollection<KeyValuePair<string, Operation>>)operations).Remove(item);
        }

        /// <summary>
        ///     Gets a value associated with a specific key.
        /// </summary>
        /// <param name="key">The key of the value to get.</param>
        /// <param name="value">The value of the key to get.</param>
        /// <returns>If the key is found, then the return is the value of the key; otherwise an initialized type of the value.</returns>
        public bool TryGetValue(string key, out Operation value)
        {
            return operations.TryGetValue(key, out value);
        }
    }
}