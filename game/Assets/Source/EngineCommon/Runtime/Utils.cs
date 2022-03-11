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

    public static class MathHelper 
    {
        public static float ClampMagnitude(float desired, float min, float max)
        {
            float desiredChangeMagnitude = Mathf.Abs(desired);
            float actualChangeMagnitude = Mathf.Clamp(desiredChangeMagnitude, min, max);
            float actualChange = Mathf.Sign(desired) * actualChangeMagnitude;
            return actualChange;
        }

        public static float GetValueChangedByAtMost(float currentValue, float desiredValue, float maxChangeAllowed)
        {
            float desiredChange = desiredValue - currentValue;
            float actualChange = ClampMagnitude(desiredChange, 0, maxChangeAllowed);
            return currentValue + actualChange;
        }
    }
}