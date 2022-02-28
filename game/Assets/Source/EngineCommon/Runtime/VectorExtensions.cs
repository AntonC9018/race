using System.Runtime.CompilerServices;
using UnityEngine;

namespace EngineCommon
{
    public static class VectorExtensions
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector2 Rotate(this Vector2 vec, float radians)
        {
            var sin = Mathf.Sin(radians);
            var cos = Mathf.Cos(radians);
            return new Vector2(cos * vec.x - sin * vec.y, sin * vec.x + cos * vec.y);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector2 RotateAround(this Vector2 vec, float radians, Vector2 point)
        {
            var offset = vec - point;
            return Rotate(offset, radians) + point;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector2 DropZ(this Vector3 vec)
        {
            return new Vector2(vec.x, vec.y);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector3 ZeroZ(this Vector2 vec)
        {
            return new Vector3(vec.x, vec.y, 0);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector3 ToRBGVector(this Color color)
        {
            return new Vector3(color.r, color.g, color.b);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Color FromRGBVector(this Vector3 rgb)
        {
            return new Color(rgb.x, rgb.y, rgb.z, 1);
        }
    }
}