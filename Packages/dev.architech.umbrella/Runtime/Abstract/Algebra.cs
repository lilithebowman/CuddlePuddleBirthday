using UnityEngine;
using System.Runtime.CompilerServices;

[assembly: InternalsVisibleTo("ArchiTech.Umbrella.Editor")]

namespace ArchiTech
{
    public static class Algebra
    {
        public static Vector3 GetParabolicPoint(Vector3 start, Vector3 end, float height, float t)
        {
            float arc = t * 2 - 1; // normalize 0,1 to -1,1
            Vector3 travelDirection = end - start;
            Vector3 result = start + t * travelDirection;
            result.y += (-arc * arc + 1) * height;
            return result;
        }

        public static Vector3 GetParabolicApex(Vector3 start, Vector3 end, float height)
        {
            float t = (end.y - start.y + 4 * height) / (8 * height);
            return GetParabolicPoint(start, end, height, t);
        }

        public static Vector3 GetQuadraticBezierPoint(Vector3 p0, Vector3 p12, Vector3 p3, float t, float w)
        {
            return GetCubicBezierPoint(p0, p12, p12, p3, t, w);
        }

        public static Vector3 GetCubicBezierPoint(Vector3 p0, Vector3 p1, Vector3 p2, Vector3 p3, float t, float w)
        {
            float tt = t * t;
            float ttt = t * tt;
            float u = 1.0f - t;
            float uu = u * u;
            float uuu = u * uu;

            Vector3 B = new Vector3();
            B = uuu * p0;
            B += 3.0f * uu * t * p1;
            B += 3.0f * u * tt * p2;
            B += ttt * p3;

            return B;
        }

        public static Vector3 GetHermiteCurvePoint(Vector3 p0, Vector3 p1, Vector3 p2, Vector3 p3, float t)
        {
            // Hermite curve formula:
            // (2t^3 - 3t^2 + 1) * p0 + (t^3 - 2t^2 + t) * m0 + (-2t^3 + 3t^2) * p1 + (t^3 - t^2) * m1

            float tt = t * t;
            float ttt = t * tt;
            float ttt2 = 2f * ttt;
            float tt3 = 3f * tt;

            Vector3 point = p0 * (ttt2 - tt3 + 1.0f);
            point += p1 * (ttt - 2.0f * tt + t);
            point += p2 * (ttt - tt);
            point += p3 * (-ttt2 + tt3);

            // Vector3 position = (ttt2 - tt3 + 1.0f) * p0
            //                    + (ttt - 2.0f * tt + t) * p1
            //                    + (ttt - tt) * p2
            //                    + (-ttt2 + tt3) * p3;

            return point;
        }

        public static void MoveTowards(this Rigidbody body, Vector3 position)
        {
            body.velocity = (position - body.position) / Time.deltaTime;
        }

        public static void SpinTowards(this Rigidbody body, Quaternion rotation)
        {
            // Rotations stack right to left,
            // so first we undo our rotation, then apply the target.
            var delta = rotation * Quaternion.Inverse(body.rotation);
            delta.ToAngleAxis(out float angle, out Vector3 axis);

            // We get an infinite axis in the event that our rotation is already aligned.
            if (float.IsInfinity(axis.x)) return;
            if (angle > 180f) angle -= 360f;

            // Here I drop down to 0.98f times the desired movement,
            // since we'd rather undershoot and ease into the correct angle
            // than overshoot and oscillate around it in the event of errors.
            Vector3 angular = (0.98f * Mathf.Deg2Rad * angle / Time.deltaTime) * axis.normalized;

            body.angularVelocity = angular;
        }
    }
}