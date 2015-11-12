﻿// *********************************************************
//
// Copyright (c) Microsoft. All rights reserved.
//
// *********************************************************

namespace Microsoft.ProjectOxford.Face.Contract
{
    /// <summary>
    /// The verify result entity.
    /// </summary>
    public class VerifyResult
    {
        /// <summary>
        /// Gets or sets a value indicating whether this instance is same.
        /// </summary>
        /// <value>
        ///   <c>true</c> if this instance is same; otherwise, <c>false</c>.
        /// </value>
        public bool IsIdentical { get; set; }

        /// <summary>
        /// Gets or sets the confidence.
        /// </summary>
        /// <value>
        /// The confidence.
        /// </value>
        public double Confidence { get; set; }
    }
}