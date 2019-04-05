// ***********************************************************************
// Assembly         : Assembly-CSharp-Editor
// Author           : AiDesigner
// Created          : 05-27-2017
// Modified         : 08-29-2017
// ***********************************************************************
using AiUnity.Common.InternalLog;
using AiUnity.Common.Extensions;
using AiUnity.Common.Log;

namespace AiUnity.MultipleTags.Common
{
    /// <summary>
    /// MultipleTags internal logger.
    /// </summary>
#if UNITY_EDITOR
    [UnityEditor.InitializeOnLoad]
#endif
    public class MultipleTagsInternalLogger : InternalLogger<MultipleTagsInternalLogger>
    {
        static MultipleTagsInternalLogger()
        {
            Instance.Assert(true, "This log statement is executed prior to unity editor serialization due to InitializeOnLoad attribute.  The allows MultipleTags logger to work in all phases of Unity Editor compile (ie. serialization).");
            CommonInternalLogger.Instance.Assert(true, "This log statement is executed prior to unity editor serialization due to InitializeOnLoad attribute.  The allows Common logger to work in all phases of Unity Editor compile (ie. serialization).");
        }
    }
}