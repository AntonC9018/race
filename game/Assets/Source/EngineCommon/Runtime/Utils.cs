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
        [System.Diagnostics.Conditional("UNITY_ASSERTIONS")]
        public static void assert(bool condition, string message)
        {
            UnityEngine.Assertions.Assert.IsTrue(condition, message);
        }
        
        [System.Diagnostics.Conditional("UNITY_ASSERTIONS")]
        public static void assert(bool condition)
        {
            UnityEngine.Assertions.Assert.IsTrue(condition);
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

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int CeilDivide(int a, int b)
        {
            return (a + b - 1) / b;
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
            return new CircleInfo(radius, bounds.center);
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
    public readonly struct CircleInfo
    {
        public readonly float radius;
        public readonly Vector2 center;

        public CircleInfo(float radius, Vector2 center)
        {
            this.radius = radius;
            this.center = center;
        }
    }
}