using System.Runtime.CompilerServices;
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
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float ClampMagnitude(float desired, float min, float max)
        {
            float desiredChangeMagnitude = Mathf.Abs(desired);
            float actualChangeMagnitude = Mathf.Clamp(desiredChangeMagnitude, min, max);
            float actualChange = Mathf.Sign(desired) * actualChangeMagnitude;
            return actualChange;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float GetValueChangedByAtMost(float currentValue, float desiredValue, float maxChangeAllowed)
        {
            float desiredChange = desiredValue - currentValue;
            float actualChange = ClampMagnitude(desiredChange, 0, maxChangeAllowed);
            return currentValue + actualChange;
        }
    }

    public static class CircleHelper
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector2 GetNormal(float angle)
        {
            return new Vector2(Mathf.Cos(angle), Mathf.Sin(angle));
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector2 RotateCounterClockwiseQuarterCircle(this Vector2 normal)
        {
            return new Vector2(-normal.y, normal.x);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static CircleInfo GetCircleInfo(this RectTransform transform)
        {
            var bounds = transform.rect;
            var radius = Mathf.Min(bounds.yMax - bounds.yMin, bounds.xMax - bounds.xMin) / 2;
            return new CircleInfo
            {
                radius = radius,
                center = bounds.center,
            };
        }

        public struct RadialInterpolation
        {
            internal float _startAngle;
            internal float _anglePerIteration;
            internal int _count;
            internal int _i;

            public RadialInterpolation GetEnumerator() => this;
            public float Current => _startAngle + _anglePerIteration * _i;
            public bool MoveNext() => ++_i != _count;
            public static RadialInterpolation Empty => new RadialInterpolation{ _count = 0, _i = -1 };
            public int Index => _i;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static RadialInterpolation InterpolateAngles(float startAngle, float increaseAngle, int count)
        {
            Assertions.assert(count >= 0);

            return new RadialInterpolation
            {
                _startAngle = startAngle,
                _anglePerIteration = increaseAngle,
                _count = count,
                _i = -1
            };
        }
    }

    [System.Serializable]
    public struct CircleInfo
    {
        public float radius;
        public Vector2 center;
    }
}