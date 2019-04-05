// ***********************************************************************
// Assembly         : Assembly-CSharp
// Author           : AiDesigner
// Created          : 06-20-2016
// Modified         : 07-23-2018
// ***********************************************************************
#if AIUNITY_CODE

using System;

namespace AiUnity.Common.Log
{
    /// <summary>
    /// Assert Exception fired if exceptions enabled for asserts.
    /// </summary>
    /// <seealso cref="System.Exception" />
    public class AssertException : Exception
    {
        #region Constructors
        /// <summary>
        /// Initializes a new instance of the <see cref="AssertException"/> class.
        /// </summary>
        /// <param name="message">The message.</param>
        /// <param name="innerException">The inner exception.</param>
        public AssertException(string message, Exception innerException = null) : base(message, innerException)
        {

        }
        #endregion
    }
}
#endif