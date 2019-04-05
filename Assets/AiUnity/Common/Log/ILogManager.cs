// ***********************************************************************
// Assembly         : Assembly-CSharp
// Author           : AiDesigner
// Created          : 06-22-2016
// Modified         : 07-23-2018
// ***********************************************************************
#if AIUNITY_CODE

using System;

namespace AiUnity.Common.Log
{
    /// <summary>
    /// Interface a Log Manager must implement
    /// </summary>
    public interface ILogManager
    {
        /// <summary>
        /// Gets the logger.
        /// </summary>
        /// <param name="name">The name.</param>
        /// <param name="context">The context.</param>
        /// <param name="formatProvider">The format provider.</param>
        /// <returns>ILogger.</returns>
        ILogger GetLogger(string name, UnityEngine.Object context, IFormatProvider formatProvider = null);
    }
}
#endif