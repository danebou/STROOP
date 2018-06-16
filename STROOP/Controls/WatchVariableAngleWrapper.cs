﻿using STROOP.Extensions;
using STROOP.Managers;
using STROOP.Structs;
using STROOP.Structs.Configurations;
using STROOP.Utilities;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Reflection;
using System.Windows.Forms;

namespace STROOP.Controls
{
    public class WatchVariableAngleWrapper : WatchVariableNumberWrapper
    {
        private readonly bool _defaultSigned;
        private bool _signed;
        private Action<bool> _setSigned;

        private readonly AngleUnitType _defaultAngleUnitType;
        private AngleUnitType _angleUnitType;
        private Action<AngleUnitType> _setAngleUnitType;

        private bool _truncateToMultipleOf16;
        private bool _constrainToOneRevolution;

        private Type _effectiveType
        {
            get
            {
                if (_watchVar.ByteCount == 2 || _constrainToOneRevolution)
                    return _signed ? typeof(short) : typeof(ushort);
                else return _signed ? typeof(int) : typeof(uint);
            }
        }

        private readonly bool _isYaw;

        public WatchVariableAngleWrapper(
            WatchVariable watchVar,
            WatchVariableControl watchVarControl,
            Type displayType,
            bool? isYaw)
            : base(watchVar, watchVarControl, 0)
        {
            Type type = displayType ?? _watchVar.MemoryType;
            if (type == null) throw new ArgumentOutOfRangeException();

            _defaultSigned = type == typeof(short) || type == typeof(int);
            _signed = _defaultSigned;

            _defaultAngleUnitType = AngleUnitType.InGameUnits;
            _angleUnitType = _defaultAngleUnitType;

            _truncateToMultipleOf16 = false;

            _constrainToOneRevolution =
                displayType != null && TypeUtilities.TypeSize[displayType] == 2 &&
                watchVar.MemoryType != null && TypeUtilities.TypeSize[watchVar.MemoryType] == 4;

            _isYaw = isYaw ?? DEFAULT_IS_YAW;

            AddAngleContextMenuStripItems();
        }

        private void AddAngleContextMenuStripItems()
        {
            ToolStripMenuItem itemSigned = new ToolStripMenuItem("Signed");
            _setSigned = (bool signed) =>
            {
                _signed = signed;
                itemSigned.Checked = signed;
            };
            itemSigned.Click += (sender, e) => _setSigned(!_signed);
            itemSigned.Checked = _signed;

            ToolStripMenuItem itemUnits = new ToolStripMenuItem("Units...");
            _setAngleUnitType = ControlUtilities.AddCheckableDropDownItems(
                itemUnits,
                new List<string> { "In-Game Units", "HAU", "Degrees", "Radians", "Revolutions" },
                new List<AngleUnitType>
                {
                    AngleUnitType.InGameUnits,
                    AngleUnitType.HAU,
                    AngleUnitType.Degrees,
                    AngleUnitType.Radians,
                    AngleUnitType.Revolutions,
                },
                (AngleUnitType angleUnitType) => { _angleUnitType = angleUnitType; },
                _angleUnitType);

            ToolStripMenuItem itemTruncateToMultipleOf16 = new ToolStripMenuItem("Truncate to Multiple of 16");
            itemTruncateToMultipleOf16.Click += (sender, e) =>
            {
                _truncateToMultipleOf16 = !_truncateToMultipleOf16;
                itemTruncateToMultipleOf16.Checked = _truncateToMultipleOf16;
            };
            itemTruncateToMultipleOf16.Checked = _truncateToMultipleOf16;

            ToolStripMenuItem itemConstrainToOneRevolution = new ToolStripMenuItem("Constrain to One Revolution");
            itemConstrainToOneRevolution.Click += (sender, e) =>
            {
                _constrainToOneRevolution = !_constrainToOneRevolution;
                itemConstrainToOneRevolution.Checked = _constrainToOneRevolution;
            };
            itemConstrainToOneRevolution.Checked = _constrainToOneRevolution;

            _contextMenuStrip.AddToBeginningList(new ToolStripSeparator());
            _contextMenuStrip.AddToBeginningList(itemSigned);
            _contextMenuStrip.AddToBeginningList(itemUnits);
            _contextMenuStrip.AddToBeginningList(itemTruncateToMultipleOf16);
            _contextMenuStrip.AddToBeginningList(itemConstrainToOneRevolution);
        }

        private double GetAngleUnitTypeMaxValue(AngleUnitType? angleUnitTypeNullable = null)
        {
            AngleUnitType angleUnitType = angleUnitTypeNullable ?? _angleUnitType;
            switch (angleUnitType)
            {
                case AngleUnitType.InGameUnits:
                    return 65536;
                case AngleUnitType.HAU:
                    return 4096;
                case AngleUnitType.Degrees:
                    return 360;
                case AngleUnitType.Radians:
                    return 2 * Math.PI;
                case AngleUnitType.Revolutions:
                    return 1;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        private double GetAngleUnitTypeAndMaybeSignedMaxValue(AngleUnitType? angleUnitTypeNullable = null, bool? signedNullable = null)
        {
            AngleUnitType angleUnitType = angleUnitTypeNullable ?? _angleUnitType;
            bool signed = signedNullable ?? _signed;
            double maxValue = GetAngleUnitTypeMaxValue(angleUnitType);
            return signed ? maxValue / 2 : maxValue;
        }

        private double GetAngleUnitTypeAndMaybeSignedMinValue(AngleUnitType? angleUnitTypeNullable = null, bool? signedNullable = null)
        {
            AngleUnitType angleUnitType = angleUnitTypeNullable ?? _angleUnitType;
            bool signed = signedNullable ?? _signed;
            double maxValue = GetAngleUnitTypeMaxValue(angleUnitType);
            return signed ? -1 * maxValue / 2 : 0;
        }

        protected override object HandleAngleConverting(object value)
        {
            double? doubleValueNullable = ParsingUtilities.ParseDoubleNullable(value);
            if (!doubleValueNullable.HasValue) return value;
            double doubleValue = doubleValueNullable.Value;

            if (_truncateToMultipleOf16)
            {
                doubleValue = MoreMath.TruncateToMultipleOf16(doubleValue);
            }
            doubleValue = MoreMath.NormalizeAngleUsingType(doubleValue, _effectiveType);
            doubleValue = (doubleValue / 65536) * GetAngleUnitTypeMaxValue();

            return doubleValue;
        }

        protected override object HandleAngleUnconverting(object value)
        {
            double? doubleValueNullable = ParsingUtilities.ParseDoubleNullable(value);
            if (!doubleValueNullable.HasValue) return value;
            double doubleValue = doubleValueNullable.Value;

            doubleValue = (doubleValue / GetAngleUnitTypeMaxValue()) * 65536;

            return doubleValue;
        }

        protected override object HandleAngleRoundingOut(object value)
        {
            double? doubleValueNullable = ParsingUtilities.ParseDoubleNullable(value);
            if (!doubleValueNullable.HasValue) return value;
            double doubleValue = doubleValueNullable.Value;

            if (doubleValue == GetAngleUnitTypeAndMaybeSignedMaxValue())
                doubleValue = GetAngleUnitTypeAndMaybeSignedMinValue();

            return doubleValue;
        }

        protected override int? GetHexDigitCount()
        {
            return _constrainToOneRevolution ? 4 : base.GetHexDigitCount();
        }

        public override void ApplySettings(WatchVariableControlSettings settings)
        {
            base.ApplySettings(settings);
            if (settings.ChangeAngleSigned)
            {
                if (settings.ChangeAngleSignedToDefault)
                    _setSigned(_defaultSigned);
                else
                    _setSigned(settings.NewAngleSigned);
            }
            if (settings.ChangeYawSigned && _isYaw)
            {
                if (settings.ChangeYawSignedToDefault)
                    _setSigned(_defaultSigned);
                else
                    _setSigned(settings.NewYawSigned);
            }
            if (settings.ChangeAngleUnits)
            {
                if (settings.ChangeAngleUnitsToDefault)
                    _setAngleUnitType(_defaultAngleUnitType);
                else
                    _setAngleUnitType(settings.NewAngleUnits);
            }
            if (settings.ChangeAngleHex)
            {
                if (settings.ChangeAngleHexToDefault)
                    _setDisplayAsHex(_defaultDisplayAsHex);
                else
                    _setDisplayAsHex(settings.NewAngleHex);
            }
        }
    }
}