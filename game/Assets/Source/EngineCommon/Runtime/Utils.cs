using UnityEngine;

namespace EngineCommon
{
    /// <summary>
    /// Defines convenient Java-like assertion functions.
    /// To use `assert()`, do `using static EngineCommon.Assertions` at the top of the file.
    /// The reason these exist is because Unity's `Debug.Assert` does not throw
    /// and so the code after it will continue execution, which I never want.
    /// </summary>
    public static class Assertions
    {
        public class AssertionException : System.Exception
        {
            public AssertionException(string message = null) : base(message){}
        }

        [System.Diagnostics.Conditional("UNITY_ASSERTIONS")]
        public static void Assert(bool condition, string message)
        {
            Debug.Assert(condition, message);
            if (!condition)
                throw new AssertionException(message);
        }
        
        [System.Diagnostics.Conditional("UNITY_ASSERTIONS")]
        public static void Assert(bool condition)
        {
            Debug.Assert(condition);
            if (!condition)
                throw new AssertionException();
        }

        [System.Diagnostics.Conditional("UNITY_ASSERTIONS")]
        public static void assert(bool condition, string message)
        {
            Debug.Assert(condition, message);
            if (!condition)
                throw new AssertionException(message);
        }
        
        [System.Diagnostics.Conditional("UNITY_ASSERTIONS")]
        public static void assert(bool condition)
        {
            Debug.Assert(condition);
            if (!condition)
                throw new AssertionException();
        }
    }
}