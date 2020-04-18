/*
Claims Authorization Policy Langugage SDK ver. 3.0
Copyright (c) Matt Long labskunk@gmail.com
All rights reserved.
MIT License
*/

using System;

namespace Capl.Authorization.Operations
{
    /// <summary>
    ///     Determines if a string is a substring on another.
    /// </summary>
    public class ContainsOperation : Operation
    {
        public static Uri OperationUri => new Uri(AuthorizationConstants.OperationUris.Contains);

        public override Uri Uri => new Uri(AuthorizationConstants.OperationUris.Contains);

        public override bool Execute(string left, string right)
        {
            return left.Contains(right);
        }
    }
}