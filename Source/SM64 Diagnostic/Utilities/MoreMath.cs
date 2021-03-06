﻿using SM64_Diagnostic.Structs.Configurations;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SM64_Diagnostic.Utilities
{
    public static class MoreMath
    {
        public static double GetHypotenuse(double x, double y)
        {
            return Math.Sqrt(x * x + y * y);
        }

        public static double GetHypotenuse(double x, double y, double z)
        {
            return Math.Sqrt(x * x + y * y + z * z);
        }

        public static double GetDistanceBetween(double x1, double y1, double z1, double x2, double y2, double z2)
        {
            double dx, dy, dz;
            dx = x1 - x2;
            dy = y1 - y2;
            dz = z1 - z2;
            return Math.Sqrt(dx * dx + dy * dy + dz * dz);
        }

        public static double GetDistanceBetween(double x1, double z1, double x2, double z2)
        {
            double dx, dz;
            dx = x1 - x2;
            dz = z1 - z2;
            return Math.Sqrt(dx * dx + dz * dz);
        }

        public static (double xDist, double zDist) GetComponentsFromVector(double magnitude, double angle)
        {
            double radians = AngleUnitsToRadians(angle);
            double xComponent = Math.Sin(radians);
            double zComponent = Math.Cos(radians);
            return (magnitude * xComponent, magnitude * zComponent);
        }

        public static (double sidewaysDist, double forwardsDist) GetComponentsFromVectorRelatively(double magnitude, double vectorAngle, double baseAngle)
        {
            double rotatedAngle = NormalizeAngleDouble(vectorAngle - baseAngle);
            (double xComponent, double zComponent) = GetComponentsFromVector(magnitude, rotatedAngle);
            return (-1 * xComponent, zComponent);
        }

        public static (double magnitude, double angle) GetVectorFromComponents(double xDist, double zDist)
        {
            double magnitude = Math.Sqrt(xDist * xDist + zDist * zDist);
            double angle = AngleTo_AngleUnits(0, 0, xDist, zDist);
            return (magnitude, angle);
        }

        public static double GetDistanceFromPointToLine(
            double pX, double pZ, double v1X, double v1Z, double v2X, double v2Z)
        {
            double numerator = Math.Abs((v2Z - v1Z) * pX - (v2X - v1X) * pZ + v2X * v1Z - v2Z * v1X);
            double denominator = GetDistanceBetween(v1X, v1Z, v2X, v2Z);
            return numerator / denominator;
        }

        public static double GetSignedDistanceFromPointToLine(
            double pX, double pZ, double v1X, double v1Z, double v2X, double v2Z, double v3X, double v3Z, int p1Index, int p2Index, bool? misalignmentOffsetNullable = null)
        {
            double[] vX = new double[] { v1X, v2X, v3X };
            double[] vZ = new double[] { v1Z, v2Z, v3Z };

            double p1X = vX[p1Index - 1];
            double p1Z = vZ[p1Index - 1];
            double p2X = vX[p2Index - 1];
            double p2Z = vZ[p2Index - 1];

            double dist = MoreMath.GetDistanceFromPointToLine(pX, pZ, p1X, p1Z, p2X, p2Z);
            bool leftOfLine = MoreMath.IsPointLeftOfLine(pX, pZ, p1X, p1Z, p2X, p2Z);
            bool floorTri = MoreMath.IsPointLeftOfLine(v3X, v3Z, v1X, v1Z, v2X, v2Z);
            bool onSideOfLineTowardsTri = floorTri == leftOfLine;
            double signedDist = dist * (onSideOfLineTowardsTri ? 1 : -1);

            bool misalignmentOffset = misalignmentOffsetNullable ?? Config.UseMisalignmentOffsetForDistanceToLine;
            if (misalignmentOffset)
            {
                if (p1X == p2X)
                {
                    bool thirdPointOnLeft = p1Z >= p2Z == floorTri;
                    if ((thirdPointOnLeft && p1X >= 0) || (!thirdPointOnLeft && p1X <= 0))
                    {
                        signedDist += 1;
                    }
                }
                else if (p1Z == p2Z)
                {
                    bool thirdPointOnTop = p1X <= p2X == floorTri;
                    if ((thirdPointOnTop && p1Z >= 0) || (!thirdPointOnTop && p1Z <= 0))
                    {
                        signedDist += 1;
                    }
                }
            }

            return signedDist;
        }

        public static bool IsPointInsideTriangle(
            double pX, double pZ, double v1X, double v1Z, double v2X, double v2Z, double v3X, double v3Z)
        {
            bool leftOf12 = IsPointLeftOfLine(pX, pZ, v1X, v1Z, v2X, v2Z);
            bool leftOf23 = IsPointLeftOfLine(pX, pZ, v2X, v2Z, v3X, v3Z);
            bool leftOf31 = IsPointLeftOfLine(pX, pZ, v3X, v3Z, v1X, v1Z);
            return leftOf12 == leftOf23 && leftOf23 == leftOf31;
        }

        public static bool IsPointLeftOfLine(
            double pX, double pZ, double v1X, double v1Z, double v2X, double v2Z)
        {
            return (v1Z - pZ) * (v2X - v1X) >= (v1X - pX) * (v2Z - v1Z);
        }

        public static double GetPlaneDistanceBetweenPoints(
            double pointX, double pointY, double pointZ, double startX, double startY, double startZ, double endX, double endY, double endZ)
        {
            double startToPointX = pointX - startX;
            double startToPointY = pointY - startY;
            double startToPointZ = pointZ - startZ;
            double startToEndX = endX - startX;
            double startToEndY = endY - startY;
            double startToEndZ = endZ - startZ;

            double dotProduct = GetDotProduct(startToPointX, startToPointY, startToPointZ, startToEndX, startToEndY, startToEndZ);
            double prevToNextDist = GetDistanceBetween(startX, startY, startZ, endX, endY, endZ);
            double planeDistance = dotProduct / prevToNextDist;

            return planeDistance;
        }

        public static double NormalizeAngleDouble(double angle)
        {
            return NonNegativeModulus(angle, 65536);
        }

        public static ushort NormalizeAngleUshort(double angle)
        {
            double nonNegative = NormalizeAngleDouble(angle);
            return (ushort)(Math.Round(nonNegative) % 65536);
        }

        public static short NormalizeAngleShort(double angle)
        {
            ushort angleUshort = NormalizeAngleUshort(angle);
            short angleShort;
            if (angleUshort > 32767)
            {
                angleShort = (short)(angleUshort - 65536);
            }
            else
            {
                angleShort = (short)angleUshort;
            }
            return angleShort;
        }

        public static ushort NormalizeAngleTruncated(double angle)
        {
            angle = NormalizeAngleDouble(angle);
            ushort angleUshort = (ushort)angle;
            ushort angleTruncated = (ushort)(angleUshort - (angleUshort % 16));
            return angleTruncated;
        }

        public static double AngleTo_Radians(double xFrom, double zFrom, double xTo, double zTo)
        {
            return Math.Atan2(xTo - xFrom, zTo - zFrom);
        }

        public static double AngleTo_Radians(double xTo, double zTo)
        {
            return AngleTo_Radians(0, 0, xTo, zTo);
        }

        public static double AngleTo_AngleUnits(double xFrom, double zFrom, double xTo, double zTo)
        {
            return RadiansToAngleUnits(AngleTo_Radians(xFrom, zFrom, xTo, zTo));
        }

        public static double AngleTo_AngleUnits(double xTo, double zTo)
        {
            return AngleTo_AngleUnits(0, 0, xTo, zTo);
        }

        public static double? AngleTo_AngleUnitsNullable(double xTo, double zTo)
        {
            if (xTo == 0 && zTo == 0) return null;
            return AngleTo_AngleUnits(0, 0, xTo, zTo);
        }

        public static ushort AngleTo_AngleUnitsRounded(double xFrom, double zFrom, double xTo, double zTo)
        {
            return RadiansToAngleUnitsRounded(AngleTo_Radians(xFrom, zFrom, xTo, zTo));
        }

        public static ushort AngleTo_AngleUnitsRounded(double xTo, double zTo)
        {
            return AngleTo_AngleUnitsRounded(0, 0, xTo, zTo);
        }

        public static (double radius, double theta, double phi) EulerToSpherical_Radians(double x, double y, double z)
        {
            double radius = Math.Sqrt(x * x + y * y + z * z);
            double theta = Math.Atan2(x, z);
            double phi = radius == 0 ? 0 : Math.Acos(y / radius);
            return (radius, theta, phi);
        }

        public static (double radius, double theta, double phi) EulerToSpherical_AngleUnits(double x, double y, double z)
        {
            double radius, thetaRadians, phiRadians;
            (radius, thetaRadians, phiRadians) = EulerToSpherical_Radians(x, y, z);
            double thetaAngleUnits = RadiansToAngleUnits(thetaRadians);
            double phiAngleUnits = RadiansToAngleUnits(phiRadians);
            return (radius, thetaAngleUnits, phiAngleUnits);
        }

        public static (double x, double y, double z) SphericalToEuler_Radians(double radius, double theta, double phi)
        {
            double x = radius * Math.Sin(theta) * Math.Sin(phi);
            double y = radius * Math.Cos(phi);
            double z = radius * Math.Cos(theta) * Math.Sin(phi);
            return (x, y, z);
        }

        public static (double x, double y, double z) SphericalToEuler_AngleUnits(double radius, double thetaAngleUnits, double phiAngleUnits)
        {
            double thetaRadians = AngleUnitsToRadians(thetaAngleUnits);
            double phiRadians = AngleUnitsToRadians(phiAngleUnits);
            return SphericalToEuler_Radians(radius, thetaRadians, phiRadians);
        }

        public static (double radius, double theta, double height) EulerToCylindrical_Radians(double x, double y, double z)
        {
            double radius = Math.Sqrt(x * x + z * z);
            double theta = Math.Atan2(x, z);
            double height = y;
            return (radius, theta, height);
        }

        public static (double x, double y, double z) CylindricalToEuler_Radians(double radius, double theta, double height)
        {
            double x = radius * Math.Sin(theta);
            double y = height;
            double z = radius * Math.Cos(theta);
            return (x, y, z);
        }

        public static (double radius, double thetaAngleUnits, double height) EulerToCylindrical_AngleUnits(double x, double y, double z)
        {
            double radius, thetaRadians, height;
            (radius, thetaRadians, height) = EulerToCylindrical_Radians(x, y, z);
            double thetaAngleUnits = RadiansToAngleUnits(thetaRadians);
            return (radius, thetaAngleUnits, height);
        }

        public static (double x, double y, double z) CylindricalToEuler_AngleUnits(double radius, double thetaAngleUnits, double height)
        {
            double thetaRadians = AngleUnitsToRadians(thetaAngleUnits);
            return CylindricalToEuler_Radians(radius, thetaRadians, height);
        }

        public static (double radius, double thetaAngleUnits, double height) EulerToCylindricalAboutPivot(
            double x, double y, double z, double pivotX, double pivotY, double pivotZ)
        {
            return EulerToCylindrical_AngleUnits(x - pivotX, y - pivotY, z - pivotZ);
        }

        public static double RadiansToAngleUnits(double radians)
        {
            double angleUnits = radians / (2 * Math.PI) * 65536;
            return NonNegativeModulus(angleUnits, 65536);
        }

        public static ushort RadiansToAngleUnitsRounded(double radians)
        {
            double angleUnits = radians / (2 * Math.PI) * 65536;
            double nonNegative = NonNegativeModulus(angleUnits, 65536);
            return (ushort)(Math.Round(nonNegative) % 65536);
        }

        public static double AngleUnitsToRadians(double angleUnits)
        {
            double radians = angleUnits / 65536 * (2 * Math.PI);
            return NonNegativeModulus(radians, 2 * Math.PI);
        }

        public static double AngleUnitsToDegrees(double angleUnits)
        {
            double radians = angleUnits / 65536 * 360;
            return NonNegativeModulus(radians, 360);
        }

        public static double RotateAngleCCW(double angleUnits, double rotationDiff)
        {
            return NormalizeAngleDouble(angleUnits + rotationDiff);
        }

        public static double RotateAngleCW(double angleUnits, double rotationDiff)
        {
            return RotateAngleCCW(angleUnits, -1 * rotationDiff);
        }

        public static double ReverseAngle(double angleUnits)
        {
            return RotateAngleCCW(angleUnits, 32768);
        }

        public static (double x, double y, double z) OffsetSpherically(
            double x, double y, double z, double radiusChange, double thetaChangeAngleUnits, double phiChangeAngleUnits)
        {
            double oldRadius, oldTheta, oldPhi;
            (oldRadius, oldTheta, oldPhi) = EulerToSpherical_AngleUnits(x, y, z);

            double newRadius = Math.Max(oldRadius + radiusChange, 0);
            double newTheta = NonNegativeModulus(oldTheta + thetaChangeAngleUnits, 65536);
            double newPhi = OffsetAngleUnitsCappedAt32768(oldPhi, phiChangeAngleUnits);

            return SphericalToEuler_AngleUnits(newRadius, newTheta, newPhi);
        }

        public static (double x, double y, double z) OffsetSphericallyAboutPivot(
            double x, double y, double z, double radiusChange, double thetaChangeAngleUnits, double phiChangeAngleUnits,
            double pivotX, double pivotY, double pivotZ)
        {
            double oldRelX = x - pivotX;
            double oldRelY = y - pivotY;
            double oldRelZ = z - pivotZ;

            double newRelX, newRelY, newRelZ;
            (newRelX, newRelY, newRelZ) =
                OffsetSpherically(oldRelX, oldRelY, oldRelZ, radiusChange, thetaChangeAngleUnits, phiChangeAngleUnits);

            return (newRelX + pivotX, newRelY + pivotY, newRelZ + pivotZ);
        }

        public static double OffsetAngleUnitsCapped(double angleUnits, double change)
        {
            angleUnits = NonNegativeModulus(angleUnits, 65536);
            angleUnits = Clamp(angleUnits + change, 0, 65536);
            angleUnits = NonNegativeModulus(angleUnits, 65536);
            return angleUnits;
        }

        public static double OffsetAngleUnitsCappedAt32768(double angleUnits, double change)
        {
            angleUnits = NonNegativeModulus(angleUnits, 65536);
            angleUnits = Clamp(angleUnits + change, 0, 32768);
            return angleUnits;
        }

        /** Gets the value in [0, modulus). */
        public static int NonNegativeModulus(int value, int modulus)
        {
            value %= modulus;
            if (value < 0) value += modulus;
            return value;
        }

        /** Gets the value in [0, modulus). */
        public static double NonNegativeModulus(double value, double modulus)
        {
            value %= modulus;
            if (value < 0) value += modulus;
            return value;
        }

        /** Gets the value in [-modulus/2, modulus/2). */
        public static double MaybeNegativeModulus(double value, double modulus)
        {
            value %= modulus;
            if (value < 0) value += modulus;
            if (value >= modulus / 2) value -= modulus;
            return value;
        }

        public static double GetAngleDifference(double angle1, double angle2)
        {
            return MaybeNegativeModulus(angle2 - angle1, 32768);
        }

        public static double Clamp(double value, double min, double max)
        {
            return Math.Min(Math.Max(value, min), max);
        }

        public static (double scaledX, double scaledZ) ScaleValues(double xValue, double zValue)
        {
            double magnitude = Math.Max(Math.Abs(xValue), Math.Abs(zValue));
            double totalMagnitude = Math.Sqrt(xValue * xValue + zValue * zValue);
            double multiplier = totalMagnitude == 0 ? 1 : magnitude / totalMagnitude;
            return (xValue * multiplier, zValue * multiplier);
        }

        public static ushort getUphillAngle(double normX, double normY, double normZ)
        {
            var uphillRadians = Math.PI + Math.Atan2(normX, normZ);
            if (normY < -0.01)
                uphillRadians += Math.PI;
            if (normX == 0 && normZ == 0)
                uphillRadians = 0;
            return RadiansToAngleUnitsRounded(uphillRadians);
        }

        public static (double effectiveX, double effectiveY) GetEffectiveInput(double rawX, double rawY)
        {
            double effectiveX = rawX >= 8 ? rawX - 6 : rawX <= -8 ? rawX + 6 : 0;
            double effectiveY = rawY >= 8 ? rawY - 6 : rawY <= -8 ? rawY + 6 : 0;
            double hypotenuse = GetHypotenuse(effectiveX, effectiveY);
            if (hypotenuse > 64)
            {
                effectiveX *= 64 / hypotenuse;
                effectiveY *= 64 / hypotenuse;
            }
            return (effectiveX, effectiveY);
        }

        public static float GetPendulumAmplitude(uint pendulumAddress)
        {
            // Get pendulum variables
            float accelerationDirection = Config.Stream.GetSingle(pendulumAddress + Config.ObjectSlots.PendulumAccelerationDirection);
            float accelerationMagnitude = Config.Stream.GetSingle(pendulumAddress + Config.ObjectSlots.PendulumAccelerationMagnitude);
            float angularVelocity = Config.Stream.GetSingle(pendulumAddress + Config.ObjectSlots.PendulumAngularVelocity);
            float angle = Config.Stream.GetSingle(pendulumAddress + Config.ObjectSlots.PendulumAngle);
            float acceleration = accelerationDirection * accelerationMagnitude;

            // Calculate one frame forwards to see if pendulum is speeding up or slowing down
            float nextAccelerationDirection = accelerationDirection;
            if (angle > 0) nextAccelerationDirection = -1;
            if (angle < 0) nextAccelerationDirection = 1;
            float nextAcceleration = nextAccelerationDirection * accelerationMagnitude;
            float nextAngularVelocity = angularVelocity + nextAcceleration;
            float nextAngle = angle + nextAngularVelocity;
            bool speedingUp = Math.Abs(nextAngularVelocity) > Math.Abs(angularVelocity);

            // Calculate duration of speeding up phase
            float inflectionAngle = angle;
            float inflectionAngularVelocity = nextAngularVelocity;
            float speedUpDistance = 0;
            int speedUpDuration = 0;

            if (speedingUp)
            {
                // d = t * v + t(t-1)/2 * a
                // d = tv + (t^2)a/2-ta/2
                // d = t(v-a/2) + (t^2)a/2
                // 0 = (t^2)a/2 + t(v-a/2) + -d
                // t = (-B +- sqrt(B^2 - 4AC)) / (2A)
                float tentativeSlowDownStartAngle = nextAccelerationDirection;
                float tentativeSpeedUpDistance = tentativeSlowDownStartAngle - angle;
                float A = nextAcceleration / 2;
                float B = nextAngularVelocity - nextAcceleration / 2;
                float C = -1 * tentativeSpeedUpDistance;
                double tentativeSpeedUpDuration = (-B + nextAccelerationDirection * Math.Sqrt(B * B - 4 * A * C)) / (2 * A);
                speedUpDuration = (int)Math.Ceiling(tentativeSpeedUpDuration);

                // d = t * v + t(t-1)/2 * a
                speedUpDistance = speedUpDuration * nextAngularVelocity + speedUpDuration * (speedUpDuration - 1) / 2 * nextAcceleration;
                inflectionAngle = angle + speedUpDistance;

                // v_f = v_i + t * a
                inflectionAngularVelocity = nextAngularVelocity + (speedUpDuration - 2) * nextAcceleration;
            }

            // Calculate duration of slowing down phase

            // v_f = v_i + t * a
            // 0 = v_i + t * a
            // t = v_i / a
            int slowDownDuration = (int)Math.Abs(inflectionAngularVelocity / accelerationMagnitude);

            // d = t * (v_i + v_f)/2
            // d = t * (v_i + 0)/2
            // d = t * v_i/2
            float slowDownDistance = (slowDownDuration + 1) * inflectionAngularVelocity / 2;

            // Combine the results from the speeding up phase and the slowing down phase
            float totalDistance = speedUpDistance + slowDownDistance;
            float amplitude = angle + totalDistance;
            return amplitude;
        }

        public static byte ApplyValueToMaskedByte(byte currentValue, byte mask, byte valueToSet)
        {
            byte maskedValueToSet = (byte)(valueToSet & mask);
            byte unmaskedCurrentValue = (byte)(currentValue & ~mask);
            byte newValue = (byte)(unmaskedCurrentValue | maskedValueToSet);
            return newValue;
        }

        public static byte ApplyValueToMaskedByte(byte currentValue, byte mask, bool useWholeMask)
        {
            return ApplyValueToMaskedByte(currentValue, mask, useWholeMask ? mask : (byte)0);
        }

        public static double RotateAngleTowards(double angle1, double angle2, double cap)
        {
            angle1 = NormalizeAngleDouble(angle1);
            angle2 = NormalizeAngleDouble(angle2);
            double angle12Diff = NormalizeAngleDouble(angle1 - angle2);
            double angle21Diff = NormalizeAngleDouble(angle2 - angle1);
            double rotationDiff = Math.Min(cap, Math.Min(angle12Diff, angle21Diff));
            bool angle1Less = angle21Diff <= angle12Diff;
            double newAngle = angle1 + (angle1Less ? 1 : -1) * rotationDiff;
            return NormalizeAngleDouble(newAngle);
        }

        public static double MoveNumberTowards(double start, double end, double cap)
        {
            bool startLessThanEnd = start < end;
            double diff = Math.Abs(end - start);
            double cappedDiff = Math.Min(diff, cap);
            double moved = start + (startLessThanEnd ? 1 : -1) * cappedDiff;
            return moved;
        }

        public static double GetDotProduct(double v1X, double v1Y, double v1Z, double v2X, double v2Y, double v2Z)
        {
            return v1X * v2X + v1Y * v2Y + v1Z * v2Z;
        }

        public static (double dotProduct, double distToWaypointPlane, double distToWaypoint)
            GetWaypointSpecialVars(uint objAddress)
        {
            float objX = Config.Stream.GetSingle(objAddress + Config.ObjectSlots.ObjectXOffset);
            float objY = Config.Stream.GetSingle(objAddress + Config.ObjectSlots.ObjectYOffset);
            float objZ = Config.Stream.GetSingle(objAddress + Config.ObjectSlots.ObjectZOffset);

            uint prevWaypointAddress = Config.Stream.GetUInt32(objAddress + Config.ObjectSlots.WaypointOffset);
            short prevWaypointIndex = Config.Stream.GetInt16(prevWaypointAddress + Config.Waypoint.IndexOffset);
            short prevWaypointX = Config.Stream.GetInt16(prevWaypointAddress + Config.Waypoint.XOffset);
            short prevWaypointY = Config.Stream.GetInt16(prevWaypointAddress + Config.Waypoint.YOffset);
            short prevWaypointZ = Config.Stream.GetInt16(prevWaypointAddress + Config.Waypoint.ZOffset);
            uint nextWaypointAddress = prevWaypointAddress + Config.Waypoint.StructSize;
            short nextWaypointIndex = Config.Stream.GetInt16(nextWaypointAddress + Config.Waypoint.IndexOffset);
            short nextWaypointX = Config.Stream.GetInt16(nextWaypointAddress + Config.Waypoint.XOffset);
            short nextWaypointY = Config.Stream.GetInt16(nextWaypointAddress + Config.Waypoint.YOffset);
            short nextWaypointZ = Config.Stream.GetInt16(nextWaypointAddress + Config.Waypoint.ZOffset);

            float objToWaypointX = nextWaypointX - objX;
            float objToWaypointY = nextWaypointY - objY;
            float objToWaypointZ = nextWaypointZ - objZ;
            float prevToNextX = nextWaypointX - prevWaypointX;
            float prevToNextY = nextWaypointY - prevWaypointY;
            float prevToNextZ = nextWaypointZ - prevWaypointZ;

            double dotProduct = GetDotProduct(objToWaypointX, objToWaypointY, objToWaypointZ, prevToNextX, prevToNextY, prevToNextZ);
            double prevToNextDist = GetDistanceBetween(prevWaypointX, prevWaypointY, prevWaypointZ, nextWaypointX, nextWaypointY, nextWaypointZ);
            double distToWaypointPlane = dotProduct / prevToNextDist;
            double distToWaypoint = GetDistanceBetween(objX, objY, objZ, nextWaypointX, nextWaypointY, nextWaypointZ);

            return (dotProduct, distToWaypointPlane, distToWaypoint);
        }

        public static (double effortTarget, double effortChange, double minHSpeed, double hSpeedTarget)
            GetRacingPenguinSpecialVars(uint racingPenguinAddress)
        {
            double marioY = Config.Stream.GetSingle(Config.Mario.StructAddress + Config.Mario.YOffset);
            double objectY = Config.Stream.GetSingle(racingPenguinAddress + Config.ObjectSlots.ObjectYOffset);
            double heightDiff = marioY - objectY;

            uint prevWaypointAddress = Config.Stream.GetUInt32(racingPenguinAddress + Config.ObjectSlots.WaypointOffset);
            short prevWaypointIndex = Config.Stream.GetInt16(prevWaypointAddress);
            double effort = Config.Stream.GetSingle(racingPenguinAddress + Config.ObjectSlots.RacingPenguinEffortOffset);

            double effortTarget;
            double effortChange;
            double minHSpeed = 70;
            if (heightDiff > -100 || prevWaypointIndex >= 35)
            {
                if (prevWaypointIndex >= 35) minHSpeed = 60;
                effortTarget = -500;
                effortChange = 100;
            }
            else
            {
                effortTarget = 1000;
                effortChange = 30;
            }
            effort = MoreMath.MoveNumberTowards(effort, effortTarget, effortChange);

            double hSpeedTarget = (effort - heightDiff) * 0.1;
            hSpeedTarget = MoreMath.Clamp(hSpeedTarget, minHSpeed, 150);

            return (effortTarget, effortChange, minHSpeed, hSpeedTarget);
        }

        public static (double hSpeedTarget, double hSpeedChange)
            GetKoopaTheQuickSpecialVars(uint koopaTheQuickAddress)
        {
            double hSpeedMultiplier = Config.Stream.GetSingle(koopaTheQuickAddress + Config.ObjectSlots.KoopaTheQuickHSpeedMultiplierOffset);
            short pitchToWaypointAngleUnits = Config.Stream.GetInt16(koopaTheQuickAddress + Config.ObjectSlots.PitchToWaypointOffset);
            double pitchToWaypointRadians = AngleUnitsToRadians(pitchToWaypointAngleUnits);

            double hSpeedTarget = hSpeedMultiplier * (Math.Sin(pitchToWaypointRadians) + 1) * 6;
            double hSpeedChange = hSpeedMultiplier * 0.1;

            return (hSpeedTarget, hSpeedChange);
        }
    }
}