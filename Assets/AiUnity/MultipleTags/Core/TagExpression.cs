// ***********************************************************************
// Assembly   : Assembly-CSharp
// Company    : AiUnity
// Author     : AiDesigner
//
// Created    : 07-30-2017
// Modified   : 08-04-2017
// ***********************************************************************
using System.Collections.Generic;

namespace AiUnity.MultipleTags.Core
{
    /// <summary>
    /// Holds the expression state when a stack is created to evaluate
    /// a tag expression with parenthesis grouping.
    /// </summary>
    public class TagExpression
    {
        #region Fields
        public TagLogic Operation;

        public IEnumerable<IEnumerable<string>> TagPaths;
        #endregion

        #region Constructors
        /// <summary>
        /// Initializes a new instance of the <see cref="TagExpression" /> class.
        /// </summary>
        /// <param name="operation">The tag logic operation.</param>
        /// <param name="operands">The tagPath operands.</param>
        public TagExpression(TagLogic operation, IEnumerable<IEnumerable<string>> operands)
        {
            this.Operation = operation;
            this.TagPaths = operands;
        }
        #endregion
    }
}