﻿using SM64_Diagnostic.Structs.Configurations;
using SM64_Diagnostic.Utilities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SM64_Diagnostic.Structs
{
    public class TriangleStruct
    {
        public readonly uint Address;

        public readonly ushort SurfaceType;
        public readonly byte ExertionForceIndex;
        public readonly byte ExertionAngle;
        public readonly byte Flags;
        public readonly byte Room;

        public readonly short YMin;
        public readonly short YMax;

        public readonly short X1;
        public readonly short Y1;
        public readonly short Z1;
        public readonly short X2;
        public readonly short Y2;
        public readonly short Z2;
        public readonly short X3;
        public readonly short Y3;
        public readonly short Z3;

        public readonly float NormX;
        public readonly float NormY;
        public readonly float NormZ;
        public readonly float NormOffset;

        public readonly uint AssociatedObject;

        public readonly TriangleClassification Classification;

        public readonly bool XProjection;
        public readonly bool BelongsToObject;
        public readonly bool NoCamCollision;

        public readonly static List<string> FieldNameList = new List<string> {
                "Address",
                "Classification",
                "SurfaceType",
                "ExertionForceIndex",
                "ExertionAngle",
                "Flags",
                "XProjection",
                "BelongsToObject",
                "NoCamCollision",
                "Room",
                "YMin",
                "YMax",
                "X1",
                "Y1",
                "Z1",
                "X2",
                "Y2",
                "Z2",
                "X3",
                "Y3",
                "Z3",
                "NormX",
                "NormY",
                "NormZ",
                "NormOffset",
                "AssociatedObject",
            };

        private readonly List<Object> FieldValueList;

        public TriangleStruct(uint triangleAddress)
        {
            Address = triangleAddress;

            SurfaceType = Config.Stream.GetUInt16(triangleAddress + Config.TriangleOffsets.SurfaceType);
            ExertionForceIndex = Config.Stream.GetByte(triangleAddress + Config.TriangleOffsets.ExertionForceIndex);
            ExertionAngle = Config.Stream.GetByte(triangleAddress + Config.TriangleOffsets.ExertionAngle);
            Flags = Config.Stream.GetByte(triangleAddress + Config.TriangleOffsets.Flags);
            Room = Config.Stream.GetByte(triangleAddress + Config.TriangleOffsets.Room);

            YMin = Config.Stream.GetInt16(triangleAddress + Config.TriangleOffsets.YMin);
            YMax = Config.Stream.GetInt16(triangleAddress + Config.TriangleOffsets.YMax);

            X1 = Config.Stream.GetInt16(triangleAddress + Config.TriangleOffsets.X1);
            Y1 = Config.Stream.GetInt16(triangleAddress + Config.TriangleOffsets.Y1);
            Z1 = Config.Stream.GetInt16(triangleAddress + Config.TriangleOffsets.Z1);
            X2 = Config.Stream.GetInt16(triangleAddress + Config.TriangleOffsets.X2);
            Y2 = Config.Stream.GetInt16(triangleAddress + Config.TriangleOffsets.Y2);
            Z2 = Config.Stream.GetInt16(triangleAddress + Config.TriangleOffsets.Z2);
            X3 = Config.Stream.GetInt16(triangleAddress + Config.TriangleOffsets.X3);
            Y3 = Config.Stream.GetInt16(triangleAddress + Config.TriangleOffsets.Y3);
            Z3 = Config.Stream.GetInt16(triangleAddress + Config.TriangleOffsets.Z3);

            NormX = Config.Stream.GetSingle(triangleAddress + Config.TriangleOffsets.NormX);
            NormY = Config.Stream.GetSingle(triangleAddress + Config.TriangleOffsets.NormY);
            NormZ = Config.Stream.GetSingle(triangleAddress + Config.TriangleOffsets.NormZ);
            NormOffset = Config.Stream.GetSingle(triangleAddress + Config.TriangleOffsets.NormOffset);

            AssociatedObject = Config.Stream.GetUInt32(triangleAddress + Config.TriangleOffsets.AssociatedObject);

            Classification = TriangleUtilities.CalculateClassification(NormY);

            XProjection = (Flags & Config.TriangleOffsets.ProjectionMask) != 0;
            BelongsToObject = (Flags & Config.TriangleOffsets.BelongsToObjectMask) != 0;
            NoCamCollision = (Flags & Config.TriangleOffsets.NoCamCollisionMask) != 0;

            FieldValueList = new List<object> {
                "0x" + Address.ToString("X8"),
                Classification,
                SurfaceType,
                ExertionForceIndex,
                ExertionAngle,
                "0x" + Flags.ToString("X2"),
                XProjection,
                BelongsToObject,
                NoCamCollision,
                Room,
                YMin,
                YMax,
                X1,
                Y1,
                Z1,
                X2,
                Y2,
                Z2,
                X3,
                Y3,
                Z3,
                NormX,
                NormY,
                NormZ,
                NormOffset,
                "0x" + AssociatedObject.ToString("X8"),
            };
        }

        public override string ToString()
        {
            return String.Join("\t", FieldValueList);
        }

        public static string GetFieldNameString()
        {
            return String.Join("\t", FieldNameList);
        }

        public bool IsWall()
        {
            return this.Classification == TriangleClassification.Wall;
        }

        public bool IsFloor()
        {
            return this.Classification == TriangleClassification.Floor;
        }

        public bool IsCeiling()
        {
            return this.Classification == TriangleClassification.Ceiling;
        }
    }
}
