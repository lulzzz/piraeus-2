using System;
using System.Collections;
using System.Xml;

namespace Capl.Authorization.Operations
{
    /// <summary>
    ///     Compares two DateTime types by UTC.
    /// </summary>
    public class DateTimeComparer : IComparer
    {
        #region IComparer Members

        /// <summary>
        ///     Compares two DateTime types by UTC.
        /// </summary>
        /// <param name="x">LHS datetime parameter to test.</param>
        /// <param name="y">RHS datatime parameter to test.</param>
        /// <returns>0 for equality; 1 for x greater than y; -1 for x less than y.</returns>
        public int Compare(object x, object y)
        {
            DateTime left = XmlConvert.ToDateTime((string)x, XmlDateTimeSerializationMode.Utc);
            DateTime right = XmlConvert.ToDateTime((string)y, XmlDateTimeSerializationMode.Utc);

            if (left == right)
            {
                return 0;
            }

            if (left < right)
            {
                return -1;
            }

            return 1;
        }

        #endregion IComparer Members
    }
}