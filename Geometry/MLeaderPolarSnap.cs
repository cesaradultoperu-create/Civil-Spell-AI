using System;

namespace CivilSpellAI.Geometry
{
    internal static class MLeaderPolarSnap
    {
        public static bool IsArrowGrip(
            double arrowX,
            double arrowY,
            double arrowZ,
            double gripX,
            double gripY,
            double gripZ,
            double radius)
        {
            if (!IsFinite(arrowX) ||
                !IsFinite(arrowY) ||
                !IsFinite(arrowZ) ||
                !IsFinite(gripX) ||
                !IsFinite(gripY) ||
                !IsFinite(gripZ) ||
                !IsFinite(radius) ||
                radius <= 0.0)
            {
                return false;
            }

            double deltaX = arrowX - gripX;
            double deltaY = arrowY - gripY;
            double deltaZ = arrowZ - gripZ;
            double tolerance = Math.Max(1e-6, radius * 1e-6);
            return ((deltaX * deltaX) + (deltaY * deltaY) + (deltaZ * deltaZ)) <=
                   (tolerance * tolerance);
        }

        public static bool ShouldUseRightPivot(
            double leftPivotX,
            double rightPivotX,
            double cursorX,
            bool preferRight)
        {
            if (!IsFinite(leftPivotX) ||
                !IsFinite(rightPivotX) ||
                !IsFinite(cursorX))
            {
                return preferRight;
            }

            double centerX = (leftPivotX / 2.0) + (rightPivotX / 2.0);
            if (cursorX < centerX)
            {
                return false;
            }

            if (cursorX > centerX)
            {
                return true;
            }

            return preferRight;
        }

        public static bool TryCalculate(
            double pivotX,
            double pivotY,
            double radius,
            double rawX,
            double rawY,
            double angularIncrementDegrees,
            out MLeaderPolarSnapResult result)
        {
            result = default(MLeaderPolarSnapResult);

            if (!IsFinite(pivotX) ||
                !IsFinite(pivotY) ||
                !IsFinite(radius) ||
                !IsFinite(rawX) ||
                !IsFinite(rawY) ||
                !IsFinite(angularIncrementDegrees) ||
                radius <= 0.0 ||
                angularIncrementDegrees <= 0.0 ||
                angularIncrementDegrees > 360.0)
            {
                return false;
            }

            double deltaX = rawX - pivotX;
            double deltaY = rawY - pivotY;
            if ((deltaX * deltaX) + (deltaY * deltaY) < 1e-16)
            {
                return false;
            }

            double incrementRadians = angularIncrementDegrees * Math.PI / 180.0;
            double rawAngle = Math.Atan2(deltaY, deltaX);
            double snappedAngle = Math.Round(
                rawAngle / incrementRadians,
                MidpointRounding.AwayFromZero) * incrementRadians;
            double displayAngle = snappedAngle * 180.0 / Math.PI;
            displayAngle %= 360.0;
            if (displayAngle < 0.0)
            {
                displayAngle += 360.0;
            }

            if (Math.Abs(displayAngle - 360.0) < 1e-8)
            {
                displayAngle = 0.0;
            }

            result = new MLeaderPolarSnapResult(
                pivotX + (radius * Math.Cos(snappedAngle)),
                pivotY + (radius * Math.Sin(snappedAngle)),
                snappedAngle,
                displayAngle);
            return true;
        }

        private static bool IsFinite(double value)
        {
            return !double.IsNaN(value) && !double.IsInfinity(value);
        }
    }

    internal struct MLeaderPolarSnapResult
    {
        public MLeaderPolarSnapResult(
            double x,
            double y,
            double angleRadians,
            double displayAngleDegrees)
        {
            X = x;
            Y = y;
            AngleRadians = angleRadians;
            DisplayAngleDegrees = displayAngleDegrees;
        }

        public double X { get; private set; }

        public double Y { get; private set; }

        public double AngleRadians { get; private set; }

        public double DisplayAngleDegrees { get; private set; }
    }
}
