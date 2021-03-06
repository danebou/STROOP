﻿using SM64_Diagnostic.Structs;
using SM64_Diagnostic.Utilities;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using SM64_Diagnostic.Controls;
using SM64_Diagnostic.Extensions;
using SM64_Diagnostic.Structs.Configurations;
using static SM64_Diagnostic.Controls.AngleDataContainer;
using static SM64_Diagnostic.Structs.WatchVariable;
using static SM64_Diagnostic.Utilities.ControlUtilities;
using static SM64_Diagnostic.Structs.VarXUtilities;

namespace SM64_Diagnostic.Managers
{
    public class ObjectManager : DataManager
    {
        public static ObjectManager Instance = null;

        List<IDataContainer> _behaviorDataControls = new List<IDataContainer>();

        object _watchVarLocker = new object();

        string _slotIndex;
        string _slotPos;
        string _behavior;

        BinaryButton _releaseButton;
        BinaryButton _interactButton;
        BinaryButton _cloneButton;
        BinaryButton _unloadButton;

        Label _objAddressLabelValue;
        Label _objAddressLabel;
        Label _objSlotIndexLabel;
        Label _objSlotPositionLabel;
        Label _objBehaviorLabel;
        TextBox _objectNameTextBox;
        Panel _objectBorderPanel;
        IntPictureBox _objectImagePictureBox;

        // racing penguin vars
        int _racingPenguinPreviousTimer;
        int _racingPenguinCurrentTimer;
        double _racingPenguinPreviousProgressDiff;
        double _racingPenguinCurrentProgressDiff;

        #region Fields
        public void SetBehaviorWatchVariables(List<WatchVariable> value, Color color)
        {
            lock (_watchVarLocker)
            {
                // Remove old watchVars from list
                RemoveWatchVariables(_behaviorDataControls);
                _behaviorDataControls.Clear();

                // Add new watchVars
                _behaviorDataControls.AddRange(AddWatchVariables(value, color));
            }
        }

        List<uint> _currentAddresses = new List<uint>();
        public List<uint> CurrentAddresses
        {
            get
            {
                return _currentAddresses;
            }
            set
            {
                if (_currentAddresses.SequenceEqual(value))
                    return;

                _currentAddresses = value.ToList();

                if (_currentAddresses.Count > 1)
                    _objAddressLabelValue.Text = "";
                else if (_currentAddresses.Count > 0)
                    _objAddressLabelValue.Text = "0x" + _currentAddresses[0].ToString("X8");
                else
                    _objAddressLabelValue.Text = "";

                AddressChanged();
            }
        }

        public string SlotIndex
        {
            get
            {
                return _slotIndex;
            }
            set
            {
                if (_slotIndex != value)
                {
                    _slotIndex = value;
                    _objSlotIndexLabel.Text = _slotIndex;
                }
            }
        }

        public string SlotPos
        {
            get
            {
                return _slotPos;
            }
            set
            {
                if (_slotPos != value)
                {
                    _slotPos = value;
                    _objSlotPositionLabel.Text = _slotPos;
                }
            }
        }

        public string Behavior
        {
            get
            {
                return _behavior;
            }
            set
            {
                if (_behavior != value)
                {
                    _behavior = value;
                    _objBehaviorLabel.Text = value;
                }
            }
        }

        public string Name
        {
            get
            {
                return _objectNameTextBox.Text;
            }
            set
            {
                if (_objectNameTextBox.Text != value)
                    _objectNameTextBox.Text = value;
            }
        }

        public Color BackColor
        {
            set
            {
                if (_objectBorderPanel.BackColor != value)
                {
                    _objectBorderPanel.BackColor = value;
                    _objectImagePictureBox.BackColor = value.Lighten(0.7);
                }
            }
            get
            {
                return _objectBorderPanel.BackColor;
            }
        }

        public Image Image
        {
            get
            {
                return _objectImagePictureBox.Image;
            }
            set
            {
                if (_objectImagePictureBox.Image != value)
                    _objectImagePictureBox.Image = value;
            }
        }

        #endregion

        protected override List<SpecialWatchVariable> _specialWatchVars { get; } = new List<SpecialWatchVariable>()
        {
            new SpecialWatchVariable("MarioDistanceToObject"),
            new SpecialWatchVariable("MarioHorizontalDistanceToObject"),
            new SpecialWatchVariable("MarioVerticalDistanceToObject"),
            new SpecialWatchVariable("MarioDistanceToObjectHome"),
            new SpecialWatchVariable("MarioHorizontalDistanceToObjectHome"),
            new SpecialWatchVariable("MarioVerticalDistanceToObjectHome"),
            new SpecialWatchVariable("AngleObjectToMario", true),
            new SpecialWatchVariable("DeltaAngleObjectToMario", true, AngleViewModeType.Signed),
            new SpecialWatchVariable("AngleMarioToObject", true),
            new SpecialWatchVariable("DeltaAngleMarioToObject", true, AngleViewModeType.Signed),
            new SpecialWatchVariable("AngleObjectToHome", true),
            new SpecialWatchVariable("DeltaAngleObjectToHome", true, AngleViewModeType.Signed),
            new SpecialWatchVariable("AngleHomeToObject", true),
            new SpecialWatchVariable("ObjectDistanceToHome"),
            new SpecialWatchVariable("HorizontalObjectDistanceToHome"),
            new SpecialWatchVariable("VerticalObjectDistanceToHome"),
            new SpecialWatchVariable("MarioHitboxAwayFromObject"),
            new SpecialWatchVariable("MarioHitboxAboveObject"),
            new SpecialWatchVariable("MarioHitboxBelowObject"),
            new SpecialWatchVariable("MarioHitboxOverlapsObject"),

            // Pendulum vars
            new SpecialWatchVariable("PendulumAmplitude"),
            new SpecialWatchVariable("PendulumSwingIndex"),

            // Waypoint vars
            new SpecialWatchVariable("ObjectDotProductToWaypoint"),
            new SpecialWatchVariable("ObjectDistanceToWaypointPlane"),
            new SpecialWatchVariable("ObjectDistanceToWaypoint"),

            // Racing penguin vars
            new SpecialWatchVariable("RacingPenguinEffortTarget"),
            new SpecialWatchVariable("RacingPenguinEffortChange"),
            new SpecialWatchVariable("RacingPenguinMinHSpeed"),
            new SpecialWatchVariable("RacingPenguinHSpeedTarget"),
            new SpecialWatchVariable("RacingPenguinDiffHSpeedTarget"),
            new SpecialWatchVariable("RacingPenguinProgress"),
            new SpecialWatchVariable("RacingPenguinProgressDiff"),
            new SpecialWatchVariable("RacingPenguinProgressDiffDelta"),

            // Koopa the Quick vars
            new SpecialWatchVariable("KoopaTheQuickHSpeedTarget"),
            new SpecialWatchVariable("KoopaTheQuickHSpeedChange"),
            new SpecialWatchVariable("KoopaTheQuick1Progress"),
            new SpecialWatchVariable("KoopaTheQuick2Progress"),

            // Fly Guy vars
            new SpecialWatchVariable("FlyGuyZone"),
            new SpecialWatchVariable("FlyGuyRelativeHeight"),
            new SpecialWatchVariable("FlyGuyNextHeightDiff"),
            new SpecialWatchVariable("FlyGuyMinHeight"),
            new SpecialWatchVariable("FlyGuyMaxHeight"),

            // Bobomb vars
            new SpecialWatchVariable("BobombBloatSize"),
            new SpecialWatchVariable("BobombRadius"),
            new SpecialWatchVariable("BobombSpaceBetween"),

            // Scuttlebug vars
            new SpecialWatchVariable("ScuttlebugDeltaAngleToTarget", true, AngleViewModeType.Signed),

            // Mario ghost vars
            new SpecialWatchVariable("MarioGhostVerticalDistance"),
            new SpecialWatchVariable("MarioGhostHorizontalDistance"),
            new SpecialWatchVariable("MarioGhostForwardsDistance"),
            new SpecialWatchVariable("MarioGhostSidewaysDistance"),

            // Hacked vars
            new SpecialWatchVariable("RngCallsPerFrame"),
        };

        public ObjectManager(List<WatchVariable> objectData, Control objectControl, NoTearFlowLayoutPanel variableTable)
            : base(objectData, variableTable)
        {
            Instance = this;

            SplitContainer splitContainerObject = objectControl.Controls["splitContainerObject"] as SplitContainer;

            _objAddressLabelValue = splitContainerObject.Panel1.Controls["labelObjAddValue"] as Label;
            _objAddressLabel = splitContainerObject.Panel1.Controls["labelObjAdd"] as Label;
            _objSlotIndexLabel = splitContainerObject.Panel1.Controls["labelObjSlotIndValue"] as Label;
            _objSlotPositionLabel = splitContainerObject.Panel1.Controls["labelObjSlotPosValue"] as Label;
            _objBehaviorLabel = splitContainerObject.Panel1.Controls["labelObjBhvValue"] as Label;
            _objectNameTextBox = splitContainerObject.Panel1.Controls["textBoxObjName"] as TextBox;
            _objectBorderPanel = splitContainerObject.Panel1.Controls["panelObjectBorder"] as Panel;
            _objectImagePictureBox = _objectBorderPanel.Controls["pictureBoxObject"] as IntPictureBox;

            _objAddressLabelValue.Click += ObjAddressLabel_Click;
            _objAddressLabel.Click += ObjAddressLabel_Click;

            Panel objPanel = splitContainerObject.Panel1.Controls["panelObj"] as Panel;

            var goToButton = objPanel.Controls["buttonObjGoto"] as Button;
            goToButton.Click += (sender, e) => ButtonUtilities.GotoObjects(_currentAddresses);
            ControlUtilities.AddContextMenuStripFunctions(
                goToButton,
                new List<string>() { "Goto", "Goto Laterally", "Goto X", "Goto Y", "Goto Z" },
                new List<Action>() {
                    () => ButtonUtilities.GotoObjects(_currentAddresses, (true, true, true)),
                    () => ButtonUtilities.GotoObjects(_currentAddresses, (true, false, true)),
                    () => ButtonUtilities.GotoObjects(_currentAddresses, (true, false, false)),
                    () => ButtonUtilities.GotoObjects(_currentAddresses, (false, true, false)),
                    () => ButtonUtilities.GotoObjects(_currentAddresses, (false, false, true)),
                });

            var retrieveButton = objPanel.Controls["buttonObjRetrieve"] as Button;
            retrieveButton.Click += (sender, e) => ButtonUtilities.RetrieveObjects(_currentAddresses);
            ControlUtilities.AddContextMenuStripFunctions(
                retrieveButton,
                new List<string>() { "Retrieve", "Retrieve Laterally", "Retrieve X", "Retrieve Y", "Retrieve Z" },
                new List<Action>() {
                    () => ButtonUtilities.RetrieveObjects(_currentAddresses, (true, true, true)),
                    () => ButtonUtilities.RetrieveObjects(_currentAddresses, (true, false, true)),
                    () => ButtonUtilities.RetrieveObjects(_currentAddresses, (true, false, false)),
                    () => ButtonUtilities.RetrieveObjects(_currentAddresses, (false, true, false)),
                    () => ButtonUtilities.RetrieveObjects(_currentAddresses, (false, false, true)),
                });

            var goToHomeButton = objPanel.Controls["buttonObjGotoHome"] as Button;
            goToHomeButton.Click += (sender, e) => ButtonUtilities.GotoObjectsHome(_currentAddresses);
            ControlUtilities.AddContextMenuStripFunctions(
                goToHomeButton,
                new List<string>() { "Goto Home", "Goto Home Laterally", "Goto Home X", "Goto Home Y", "Goto Home Z" },
                new List<Action>() {
                    () => ButtonUtilities.GotoObjectsHome(_currentAddresses, (true, true, true)),
                    () => ButtonUtilities.GotoObjectsHome(_currentAddresses, (true, false, true)),
                    () => ButtonUtilities.GotoObjectsHome(_currentAddresses, (true, false, false)),
                    () => ButtonUtilities.GotoObjectsHome(_currentAddresses, (false, true, false)),
                    () => ButtonUtilities.GotoObjectsHome(_currentAddresses, (false, false, true)),
                });

            var retrieveHomeButton = objPanel.Controls["buttonObjRetrieveHome"] as Button;
            retrieveHomeButton.Click += (sender, e) => ButtonUtilities.RetrieveObjectsHome(_currentAddresses);
            ControlUtilities.AddContextMenuStripFunctions(
                retrieveHomeButton,
                new List<string>() { "Retrieve Home", "Retrieve Home Laterally", "Retrieve Home X", "Retrieve Home Y", "Retrieve Home Z" },
                new List<Action>() {
                    () => ButtonUtilities.RetrieveObjectsHome(_currentAddresses, (true, true, true)),
                    () => ButtonUtilities.RetrieveObjectsHome(_currentAddresses, (true, false, true)),
                    () => ButtonUtilities.RetrieveObjectsHome(_currentAddresses, (true, false, false)),
                    () => ButtonUtilities.RetrieveObjectsHome(_currentAddresses, (false, true, false)),
                    () => ButtonUtilities.RetrieveObjectsHome(_currentAddresses, (false, false, true)),
                });

            _releaseButton = objPanel.Controls["buttonObjRelease"] as BinaryButton;
            _releaseButton.Initialize(
                "Release",
                "UnRelease",
                () => ButtonUtilities.ReleaseObject(_currentAddresses),
                () => ButtonUtilities.UnReleaseObject(_currentAddresses),
                () => _currentAddresses.Count > 0 && _currentAddresses.All(
                    address =>
                    {
                        uint releasedValue = Config.Stream.GetUInt32(address + Config.ObjectSlots.ReleaseStatusOffset);
                        return releasedValue == Config.ObjectSlots.ReleaseStatusThrownValue || releasedValue == Config.ObjectSlots.ReleaseStatusDroppedValue;
                    }));
            ControlUtilities.AddContextMenuStripFunctions(
                _releaseButton,
                new List<string>() { "Release by Throwing", "Release by Dropping", "UnRelease" },
                new List<Action>() {
                    () => ButtonUtilities.ReleaseObject(_currentAddresses, true),
                    () => ButtonUtilities.ReleaseObject(_currentAddresses, false),
                    () => ButtonUtilities.UnReleaseObject(_currentAddresses),
                });

            _interactButton = objPanel.Controls["buttonObjInteract"] as BinaryButton;
            _interactButton.Initialize(
                "Interact",
                "UnInteract",
                () => ButtonUtilities.InteractObject(_currentAddresses),
                () => ButtonUtilities.UnInteractObject(_currentAddresses),
                () => _currentAddresses.Count > 0 && _currentAddresses.All(
                    address => Config.Stream.GetUInt32(address + Config.ObjectSlots.InteractionStatusOffset) != 0));
            ControlUtilities.AddContextMenuStripFunctions(
                _interactButton,
                new List<string>() { "Interact", "UnInteract" },
                new List<Action>() {
                    () => ButtonUtilities.InteractObject(_currentAddresses),
                    () => ButtonUtilities.UnInteractObject(_currentAddresses),
                });

            _cloneButton = objPanel.Controls["buttonObjClone"] as BinaryButton;
            _cloneButton.Initialize(
                "Clone",
                "UnClone",
                () => ButtonUtilities.CloneObject(_currentAddresses[0]),
                () => ButtonUtilities.UnCloneObject(),
                () => _currentAddresses.Count == 1 && _currentAddresses.Contains(
                    Config.Stream.GetUInt32(Config.Mario.StructAddress + Config.Mario.HeldObjectPointerOffset)));
            ControlUtilities.AddContextMenuStripFunctions(
                _cloneButton,
                new List<string>() {
                    "Clone with Action Update",
                    "Clone without Action Update",
                    "UnClone with Action Update",
                    "UnClone without Action Update",
                },
                new List<Action>() {
                    () => ButtonUtilities.CloneObject(_currentAddresses[0], true),
                    () => ButtonUtilities.CloneObject(_currentAddresses[0], false),
                    () => ButtonUtilities.UnCloneObject(true),
                    () => ButtonUtilities.UnCloneObject(false),
                });

            _unloadButton = objPanel.Controls["buttonObjUnload"] as BinaryButton;
            _unloadButton.Initialize(
                "Unload",
                "Revive",
                () => ButtonUtilities.UnloadObject(_currentAddresses),
                () => ButtonUtilities.ReviveObject(_currentAddresses),
                () => _currentAddresses.Count > 0 && _currentAddresses.All(
                    address => Config.Stream.GetUInt16(address + Config.ObjectSlots.ObjectActiveOffset) == 0x0000));
            ControlUtilities.AddContextMenuStripFunctions(
                _unloadButton,
                new List<string>() { "Unload", "Revive" },
                new List<Action>() {
                    () => ButtonUtilities.UnloadObject(_currentAddresses),
                    () => ButtonUtilities.ReviveObject(_currentAddresses),
                });

            var objPosGroupBox = objPanel.Controls["groupBoxObjPos"] as GroupBox;
            ControlUtilities.InitializeThreeDimensionController(
                CoordinateSystem.Euler,
                objPosGroupBox,
                objPosGroupBox.Controls["buttonObjPosXn"] as Button,
                objPosGroupBox.Controls["buttonObjPosXp"] as Button,
                objPosGroupBox.Controls["buttonObjPosZn"] as Button,
                objPosGroupBox.Controls["buttonObjPosZp"] as Button,
                objPosGroupBox.Controls["buttonObjPosXnZn"] as Button,
                objPosGroupBox.Controls["buttonObjPosXnZp"] as Button,
                objPosGroupBox.Controls["buttonObjPosXpZn"] as Button,
                objPosGroupBox.Controls["buttonObjPosXpZp"] as Button,
                objPosGroupBox.Controls["buttonObjPosYp"] as Button,
                objPosGroupBox.Controls["buttonObjPosYn"] as Button,
                objPosGroupBox.Controls["textBoxObjPosXZ"] as TextBox,
                objPosGroupBox.Controls["textBoxObjPosY"] as TextBox,
                objPosGroupBox.Controls["checkBoxObjPosRelative"] as CheckBox,
                (float hOffset, float vOffset, float nOffset, bool useRelative) =>
                {
                    ButtonUtilities.TranslateObjects(
                        _currentAddresses,
                        hOffset,
                        nOffset,
                        -1 * vOffset,
                        useRelative);
                });

            var objAngleGroupBox = objPanel.Controls["groupBoxObjAngle"] as GroupBox;
            ControlUtilities.InitializeScalarController(
                objAngleGroupBox.Controls["buttonObjAngleYawN"] as Button,
                objAngleGroupBox.Controls["buttonObjAngleYawP"] as Button,
                objAngleGroupBox.Controls["textBoxObjAngleYaw"] as TextBox,
                (float yawValue) =>
                {
                    ButtonUtilities.RotateObjects(_currentAddresses, (int)Math.Round(yawValue), 0, 0);
                });
            ControlUtilities.InitializeScalarController(
                objAngleGroupBox.Controls["buttonObjAnglePitchN"] as Button,
                objAngleGroupBox.Controls["buttonObjAnglePitchP"] as Button,
                objAngleGroupBox.Controls["textBoxObjAnglePitch"] as TextBox,
                (float pitchValue) =>
                {
                    ButtonUtilities.RotateObjects(_currentAddresses, 0, (int)Math.Round(pitchValue), 0);
                });
            ControlUtilities.InitializeScalarController(
                objAngleGroupBox.Controls["buttonObjAngleRollN"] as Button,
                objAngleGroupBox.Controls["buttonObjAngleRollP"] as Button,
                objAngleGroupBox.Controls["textBoxObjAngleRoll"] as TextBox,
                (float rollValue) =>
                {
                    ButtonUtilities.RotateObjects(_currentAddresses, 0, 0, (int)Math.Round(rollValue));
                });

            var objScaleGroupBox = objPanel.Controls["groupBoxObjScale"] as GroupBox;
            ControlUtilities.InitializeScaleController(
                objScaleGroupBox.Controls["buttonObjScaleWidthN"] as Button,
                objScaleGroupBox.Controls["buttonObjScaleWidthP"] as Button,
                objScaleGroupBox.Controls["buttonObjScaleHeightN"] as Button,
                objScaleGroupBox.Controls["buttonObjScaleHeightP"] as Button,
                objScaleGroupBox.Controls["buttonObjScaleDepthN"] as Button,
                objScaleGroupBox.Controls["buttonObjScaleDepthP"] as Button,
                objScaleGroupBox.Controls["buttonObjScaleAggregateN"] as Button,
                objScaleGroupBox.Controls["buttonObjScaleAggregateP"] as Button,
                objScaleGroupBox.Controls["textBoxObjScaleWidth"] as TextBox,
                objScaleGroupBox.Controls["textBoxObjScaleHeight"] as TextBox,
                objScaleGroupBox.Controls["textBoxObjScaleDepth"] as TextBox,
                objScaleGroupBox.Controls["textBoxObjScaleAggregate"] as TextBox,
                objScaleGroupBox.Controls["checkBoxObjScaleAggregate"] as CheckBox,
                objScaleGroupBox.Controls["checkBoxObjScaleMultiply"] as CheckBox,
                (float widthChange, float heightChange, float depthChange, bool multiply) =>
                {
                    ButtonUtilities.ScaleObjects(_currentAddresses, widthChange, heightChange, depthChange, multiply);
                });

            var objHomeGroupBox = objPanel.Controls["groupBoxObjHome"] as GroupBox;
            ControlUtilities.InitializeThreeDimensionController(
                CoordinateSystem.Euler,
                objHomeGroupBox,
                objHomeGroupBox.Controls["buttonObjHomeXn"] as Button,
                objHomeGroupBox.Controls["buttonObjHomeXp"] as Button,
                objHomeGroupBox.Controls["buttonObjHomeZn"] as Button,
                objHomeGroupBox.Controls["buttonObjHomeZp"] as Button,
                objHomeGroupBox.Controls["buttonObjHomeXnZn"] as Button,
                objHomeGroupBox.Controls["buttonObjHomeXnZp"] as Button,
                objHomeGroupBox.Controls["buttonObjHomeXpZn"] as Button,
                objHomeGroupBox.Controls["buttonObjHomeXpZp"] as Button,
                objHomeGroupBox.Controls["buttonObjHomeYp"] as Button,
                objHomeGroupBox.Controls["buttonObjHomeYn"] as Button,
                objHomeGroupBox.Controls["textBoxObjHomeXZ"] as TextBox,
                objHomeGroupBox.Controls["textBoxObjHomeY"] as TextBox,
                objHomeGroupBox.Controls["checkBoxObjHomeRelative"] as CheckBox,
                (float hOffset, float vOffset, float nOffset, bool useRelative) =>
                {
                    ButtonUtilities.TranslateObjectHomes(
                        _currentAddresses,
                        hOffset,
                        nOffset,
                        -1 * vOffset,
                        useRelative);
                });

            GroupBox variableListFilterGroupBox = objPanel.Controls["groupBoxVariableListFilter"] as GroupBox;
            RadioButton variableListFilterSimpleRadioButton =
                variableListFilterGroupBox.Controls["radioButtonVariableListFilterSimple"] as RadioButton;
            variableListFilterSimpleRadioButton.Click +=
                (sender, e) => ApplyVariableListFilter(
                    new List<VariableGroup> { VariableGroup.Simple, VariableGroup.ObjectSpecific });
            RadioButton variableListFilterExpandedRadioButton =
                variableListFilterGroupBox.Controls["radioButtonVariableListFilterExpanded"] as RadioButton;
            variableListFilterExpandedRadioButton.Click += 
                (sender, e) => ApplyVariableListFilter(
                    new List<VariableGroup> { VariableGroup.Simple, VariableGroup.Expanded, VariableGroup.ObjectSpecific });
            RadioButton variableListFilterCollisionRadioButton =
                variableListFilterGroupBox.Controls["radioButtonVariableListFilterCollision"] as RadioButton;
            variableListFilterCollisionRadioButton.Click +=
                (sender, e) => ApplyVariableListFilter(
                    new List<VariableGroup> { VariableGroup.Collision });
        }

        private void ApplyVariableListFilter(List<VariableGroup> variableGroups)
        {
            // TODO implement this
        }

        private void AddressChanged()
        {
            var test = _dataControls.Where(d => d is WatchVariableControl);
            foreach (WatchVariableControl dataControl in test)
                dataControl.EditMode = false;

            if (CurrentAddresses.Count <= 1)
            {
                _cloneButton.Enabled = true;
            }
            else
            {
                _cloneButton.Enabled = false;
            }
        }

        private void ObjAddressLabel_Click(object sender, EventArgs e)
        {
            if (_currentAddresses.Count == 0)
                return;

            var variableTitle = "Object Address" + (_currentAddresses.Count > 1 ? " (First of Multiple)" : ""); 
            var variableInfo = new VariableViewerForm(variableTitle, "Object",
                String.Format("0x{0:X8}", _currentAddresses[0]), String.Format("0x{0:X8}", (_currentAddresses[0] & ~0x80000000) + Config.Stream.ProcessMemoryOffset.ToInt64()));
            variableInfo.ShowDialog();
        }

        private void ProcessSpecialVars()
        {
            // Get Mario position
            float mX = Config.Stream.GetSingle(Config.Mario.StructAddress + Config.Mario.XOffset);
            float mY = Config.Stream.GetSingle(Config.Mario.StructAddress + Config.Mario.YOffset);
            float mZ = Config.Stream.GetSingle(Config.Mario.StructAddress + Config.Mario.ZOffset);
            ushort mFacing = Config.Stream.GetUInt16(Config.Mario.StructAddress + Config.Mario.YawFacingOffset);

            // Get Mario object position
            var marioObjRef = Config.Stream.GetUInt32(Config.Mario.ObjectReferenceAddress);
            float mObjX = Config.Stream.GetSingle(marioObjRef + Config.ObjectSlots.ObjectXOffset);
            float mObjY = Config.Stream.GetSingle(marioObjRef + Config.ObjectSlots.ObjectYOffset);
            float mObjZ = Config.Stream.GetSingle(marioObjRef + Config.ObjectSlots.ObjectZOffset);

            // Get Mario object hitbox variables
            float mObjHitboxRadius = Config.Stream.GetSingle(marioObjRef + Config.ObjectSlots.HitboxRadius);
            float mObjHitboxHeight = Config.Stream.GetSingle(marioObjRef + Config.ObjectSlots.HitboxHeight);
            float mObjHitboxDownOffset = Config.Stream.GetSingle(marioObjRef + Config.ObjectSlots.HitboxDownOffset);
            float mObjHitboxBottom = mObjY - mObjHitboxDownOffset;
            float mObjHitboxTop = mObjY + mObjHitboxHeight - mObjHitboxDownOffset;

            bool firstObject = true;

            foreach (var objAddress in _currentAddresses)
            { 
                // Get object position
                float objX = Config.Stream.GetSingle(objAddress + Config.ObjectSlots.ObjectXOffset);
                float objY = Config.Stream.GetSingle(objAddress + Config.ObjectSlots.ObjectYOffset);
                float objZ = Config.Stream.GetSingle(objAddress + Config.ObjectSlots.ObjectZOffset);
                ushort objFacing = Config.Stream.GetUInt16(objAddress + Config.ObjectSlots.YawFacingOffset);

                // Get object position
                float objHomeX = Config.Stream.GetSingle(objAddress + Config.ObjectSlots.HomeXOffset);
                float objHomeY = Config.Stream.GetSingle(objAddress + Config.ObjectSlots.HomeYOffset);
                float objHomeZ = Config.Stream.GetSingle(objAddress + Config.ObjectSlots.HomeZOffset);

                double angleObjectToMario = MoreMath.AngleTo_AngleUnits(objX, objZ, mX, mZ);
                double angleObjectToHome = MoreMath.AngleTo_AngleUnits(objX, objZ, objHomeX, objHomeZ);

                // Get object hitbox variables
                float objHitboxRadius = Config.Stream.GetSingle(objAddress + Config.ObjectSlots.HitboxRadius);
                float objHitboxHeight = Config.Stream.GetSingle(objAddress + Config.ObjectSlots.HitboxHeight);
                float objHitboxDownOffset = Config.Stream.GetSingle(objAddress + Config.ObjectSlots.HitboxDownOffset);
                float objHitboxBottom = objY - objHitboxDownOffset;
                float objHitboxTop = objY + objHitboxHeight - objHitboxDownOffset;

                // Compute hitbox distances between Mario obj and obj
                double marioHitboxAwayFromObject = MoreMath.GetDistanceBetween(mObjX, mObjZ, objX, objZ) - mObjHitboxRadius - objHitboxRadius;
                double marioHitboxAboveObject = mObjHitboxBottom - objHitboxTop;
                double marioHitboxBelowObject = objHitboxBottom - mObjHitboxTop;

                foreach (IDataContainer specialVar in _specialDataControls)
                {
                    var newText = "";
                    double? newAngle = null;
                    switch (specialVar.SpecialName)
                    {
                        case "MarioDistanceToObject":
                            newText = Math.Round(MoreMath.GetDistanceBetween(mX, mY, mZ, objX, objY, objZ),3).ToString();
                            break;

                        case "MarioHorizontalDistanceToObject":
                            newText = Math.Round(MoreMath.GetDistanceBetween(mX, mZ, objX, objZ), 3).ToString();
                            break;

                        case "MarioVerticalDistanceToObject":
                            newText = Math.Round(mY - objY, 3).ToString();
                            break;

                        case "MarioDistanceToObjectHome":
                            newText = Math.Round(MoreMath.GetDistanceBetween(mX, mY, mZ, objHomeX, objHomeY, objHomeZ), 3).ToString();
                            break;

                        case "MarioHorizontalDistanceToObjectHome":
                            newText = Math.Round(MoreMath.GetDistanceBetween(mX, mZ, objHomeX, objHomeZ), 3).ToString();
                            break;

                        case "MarioVerticalDistanceToObjectHome":
                            newText = Math.Round(mY - objHomeY, 3).ToString();
                            break;

                        case "ObjectDistanceToHome":
                            newText = Math.Round(MoreMath.GetDistanceBetween(objX, objY, objZ, objHomeX, objHomeY, objHomeZ), 3).ToString();
                            break;

                        case "HorizontalObjectDistanceToHome":
                            newText = Math.Round(MoreMath.GetDistanceBetween(objX, objZ, objHomeX, objHomeZ), 3).ToString();
                            break;

                        case "VerticalObjectDistanceToHome":
                            newText = Math.Round(objY - objHomeY, 3).ToString();
                            break;

                        case "AngleMarioToObject":
                            newAngle = MoreMath.ReverseAngle(angleObjectToMario);
                            break;

                        case "DeltaAngleMarioToObject":
                            newAngle = mFacing - MoreMath.ReverseAngle(angleObjectToMario);
                            break;

                        case "AngleObjectToMario":
                            newAngle = angleObjectToMario;
                            break;

                        case "DeltaAngleObjectToMario":
                            newAngle = objFacing - angleObjectToMario;
                            break;

                        case "AngleObjectToHome":
                            newAngle = angleObjectToHome;
                            break;

                        case "DeltaAngleObjectToHome":
                            newAngle = objFacing - angleObjectToHome;
                            break;

                        case "AngleHomeToObject":
                            newAngle = MoreMath.ReverseAngle(angleObjectToHome);
                            break;

                        case "MarioHitboxAwayFromObject":
                            newText = Math.Round(marioHitboxAwayFromObject, 3).ToString();
                            break;

                       case "MarioHitboxAboveObject":
                            newText = Math.Round(marioHitboxAboveObject, 3).ToString();
                            break;

                        case "MarioHitboxBelowObject":
                            newText = Math.Round(marioHitboxBelowObject, 3).ToString();
                            break;

                        case "MarioHitboxOverlapsObject":
                            if (marioHitboxAwayFromObject < 0 &&
                                marioHitboxAboveObject <= 0 &&
                                marioHitboxBelowObject <= 0)
                            {
                                newText = "True";
                            }
                            else
                            {
                                newText = "False";
                            }
                            break;

                        case "PendulumAmplitude":
                            newText = MoreMath.GetPendulumAmplitude(objAddress).ToString();
                            break;

                        case "PendulumSwingIndex":
                            int? pendulumSwingIndex = Config.PendulumSwings.GetPendulumSwingIndex((int)MoreMath.GetPendulumAmplitude(objAddress));
                            newText = pendulumSwingIndex == null ? "Unknown Index" : pendulumSwingIndex.ToString();
                            break;

                        case "ObjectDotProductToWaypoint":
                            {
                                (double temp, _, _) = MoreMath.GetWaypointSpecialVars(objAddress);
                                newText = Math.Round(temp, 3).ToString();
                                break;
                            }

                        case "ObjectDistanceToWaypointPlane":
                            {
                                (_, double temp, _) = MoreMath.GetWaypointSpecialVars(objAddress);
                                newText = Math.Round(temp, 3).ToString();
                                break;
                            }

                        case "ObjectDistanceToWaypoint":
                            {
                                (_, _, double temp) = MoreMath.GetWaypointSpecialVars(objAddress);
                                newText = Math.Round(temp, 3).ToString();
                                break;
                            }

                        case "RacingPenguinEffortTarget":
                            {
                                (double temp, _, _, _) = MoreMath.GetRacingPenguinSpecialVars(objAddress);
                                newText = Math.Round(temp, 3).ToString();
                                break;
                            }

                        case "RacingPenguinEffortChange":
                            {
                                (_, double temp, _, _) = MoreMath.GetRacingPenguinSpecialVars(objAddress);
                                newText = Math.Round(temp, 3).ToString();
                                break;
                            }

                        case "RacingPenguinMinHSpeed":
                            {
                                (_, _, double temp, _) = MoreMath.GetRacingPenguinSpecialVars(objAddress);
                                newText = Math.Round(temp, 3).ToString();
                                break;
                            }

                        case "RacingPenguinHSpeedTarget":
                            {
                                (_, _, _, double temp) = MoreMath.GetRacingPenguinSpecialVars(objAddress);
                                newText = Math.Round(temp, 3).ToString();
                                break;
                            }

                        case "RacingPenguinDiffHSpeedTarget":
                            {
                                (_, _, _, double targetHSpeed) = MoreMath.GetRacingPenguinSpecialVars(objAddress);
                                float hSpeed = Config.Stream.GetSingle(objAddress + Config.ObjectSlots.HSpeedOffset);
                                newText = Math.Round(hSpeed - targetHSpeed, 3).ToString();
                                break;
                            }

                        case "RacingPenguinProgress":
                            {
                                double progress = Config.RacingPenguinWaypoints.GetProgress(objAddress);
                                newText = Math.Round(progress, 3).ToString();
                                break;
                            }

                        case "RacingPenguinProgressDiff":
                            {
                                TestingManager testingManager = TestingManager.Instance;
                                Dictionary<int, TestingManager.VarState> dictionary = testingManager.VarStateDictionary;
                                var currentTimer = Config.Stream.GetInt32(Config.SwitchRomVersion(0x803493DC, 0x803463EC));
                                if (!dictionary.ContainsKey(currentTimer))
                                {
                                    newText = "N/A";
                                    break;
                                }
                                TestingManager.VarState varState = dictionary[currentTimer];
                                if (!(varState is TestingManager.VarStatePenguin))
                                {
                                    newText = "N/A";
                                    break;
                                }
                                TestingManager.VarStatePenguin varStatePenguin = varState as TestingManager.VarStatePenguin;
                                double varStateProgress = varStatePenguin.Progress;

                                double currentProgress = Config.RacingPenguinWaypoints.GetProgress(objAddress);
                                double progressDiff = currentProgress - varStateProgress;
                                newText = Math.Round(progressDiff, 3).ToString();
                                break;
                            }

                        case "RacingPenguinProgressDiffDelta":
                            {
                                TestingManager testingManager = TestingManager.Instance;
                                Dictionary<int, TestingManager.VarState> dictionary = testingManager.VarStateDictionary;
                                var currentTimer = Config.Stream.GetInt32(Config.SwitchRomVersion(0x803493DC, 0x803463EC));
                                if (!dictionary.ContainsKey(currentTimer))
                                {
                                    newText = "N/A";
                                    break;
                                }
                                TestingManager.VarState varState = dictionary[currentTimer];
                                if (!(varState is TestingManager.VarStatePenguin))
                                {
                                    newText = "N/A";
                                    break;
                                }
                                TestingManager.VarStatePenguin varStatePenguin = varState as TestingManager.VarStatePenguin;
                                double varStateProgress = varStatePenguin.Progress;

                                double currentProgress = Config.RacingPenguinWaypoints.GetProgress(objAddress);
                                double progressDiff = currentProgress - varStateProgress;

                                if (currentTimer != _racingPenguinCurrentTimer)
                                {
                                    _racingPenguinPreviousTimer = _racingPenguinCurrentTimer;
                                    _racingPenguinPreviousProgressDiff = _racingPenguinCurrentProgressDiff;
                                    _racingPenguinCurrentTimer = currentTimer;
                                    _racingPenguinCurrentProgressDiff = progressDiff;
                                }

                                newText = Math.Round(_racingPenguinCurrentProgressDiff - _racingPenguinPreviousProgressDiff, 3).ToString();
                                break;
                            }

                        case "KoopaTheQuickHSpeedTarget":
                            {
                                (double temp, _) = MoreMath.GetKoopaTheQuickSpecialVars(objAddress);
                                newText = Math.Round(temp, 3).ToString();
                                break;
                            }

                        case "KoopaTheQuickHSpeedChange":
                            {
                                (_, double temp) = MoreMath.GetKoopaTheQuickSpecialVars(objAddress);
                                newText = Math.Round(temp, 3).ToString();
                                break;
                            }

                        case "KoopaTheQuick1Progress":
                            {
                                double progress = Config.KoopaTheQuick1Waypoints.GetProgress(objAddress);
                                newText = Math.Round(progress, 3).ToString();
                                break;
                            }

                        case "KoopaTheQuick2Progress":
                            {
                                double progress = Config.KoopaTheQuick2Waypoints.GetProgress(objAddress);
                                newText = Math.Round(progress, 3).ToString();
                                break;
                            }

                        case "FlyGuyZone":
                            {
                                double heightDiff = mY - objY;
                                if (heightDiff < -400) newText = "Low";
                                else if (heightDiff > -200) newText = "High";
                                else newText = "Medium";
                                break;
                            }

                        case "FlyGuyRelativeHeight":
                            {
                                int flyGuyOscillationTimer = Config.Stream.GetInt32(objAddress + Config.ObjectSlots.FlyGuyOscillationTimerOffset);
                                double temp = Config.FlyGuyData.GetRelativeHeight(flyGuyOscillationTimer);
                                newText = Math.Round(temp, 3).ToString();
                                break;
                            }

                        case "FlyGuyNextHeightDiff":
                            {
                                int flyGuyOscillationTimer = Config.Stream.GetInt32(objAddress + Config.ObjectSlots.FlyGuyOscillationTimerOffset);
                                double temp = Config.FlyGuyData.GetNextHeightDiff(flyGuyOscillationTimer);
                                newText = Math.Round(temp, 3).ToString();
                                break;
                            }

                        case "FlyGuyMinHeight":
                            {
                                int flyGuyOscillationTimer = Config.Stream.GetInt32(objAddress + Config.ObjectSlots.FlyGuyOscillationTimerOffset);
                                double temp = Config.FlyGuyData.GetMinHeight(flyGuyOscillationTimer, objY);
                                newText = Math.Round(temp, 3).ToString();
                                break;
                            }

                        case "FlyGuyMaxHeight":
                            {
                                int flyGuyOscillationTimer = Config.Stream.GetInt32(objAddress + Config.ObjectSlots.FlyGuyOscillationTimerOffset);
                                double temp = Config.FlyGuyData.GetMaxHeight(flyGuyOscillationTimer, objY);
                                newText = Math.Round(temp, 3).ToString();
                                break;
                            }

                        case "BobombBloatSize":
                            {
                                float scale = Config.Stream.GetSingle(objAddress + Config.ObjectSlots.ScaleWidthOffset);
                                switch(scale)
                                {
                                    case 1.0f:
                                        newText = "B0";
                                        break;
                                    case 1.2f:
                                        newText = "B1";
                                        break;
                                    case 1.4f:
                                        newText = "B2";
                                        break;
                                    case 1.6f:
                                        newText = "B3";
                                        break;
                                    case 1.8f:
                                        newText = "B4";
                                        break;
                                    default:
                                        newText = "Unknown Bloat Size";
                                        break;
                                }
                                break;
                            }

                        case "BobombRadius":
                            {
                                float scale = Config.Stream.GetSingle(objAddress + Config.ObjectSlots.ScaleWidthOffset);
                                float radius = 32 + scale * 65;
                                newText = Math.Round(radius, 3).ToString();
                                break;
                            }

                        case "BobombSpaceBetween":
                            {
                                float scale = Config.Stream.GetSingle(objAddress + Config.ObjectSlots.ScaleWidthOffset);
                                float radius = 32 + scale * 65;
                                double horizontalDistance = MoreMath.GetDistanceBetween(mX, mZ, objX, objZ);
                                newText = Math.Round(horizontalDistance - radius, 3).ToString();
                                break;
                            }

                        case "ScuttlebugDeltaAngleToTarget":
                            {
                                uint targetAngle = Config.Stream.GetUInt16(objAddress + Config.ObjectSlots.ScuttlebugTargetAngleOffset);
                                newAngle = objFacing - targetAngle;
                                break;
                            }

                        case "MarioGhostVerticalDistance":
                            {
                                float ghostY = Config.Stream.GetSingle(objAddress + Config.ObjectSlots.GraphicsYOffset);
                                newText = Math.Round(mY - ghostY, 3).ToString();
                                break;
                            }

                        case "MarioGhostHorizontalDistance":
                            {
                                float ghostX = Config.Stream.GetSingle(objAddress + Config.ObjectSlots.GraphicsXOffset);
                                float ghostZ = Config.Stream.GetSingle(objAddress + Config.ObjectSlots.GraphicsZOffset);
                                double hDistToGhost = MoreMath.GetDistanceBetween(mX, mZ, ghostX, ghostZ);
                                newText = Math.Round(hDistToGhost, 3).ToString();
                                break;
                            }

                        case "MarioGhostForwardsDistance":
                            {
                                float ghostX = Config.Stream.GetSingle(objAddress + Config.ObjectSlots.GraphicsXOffset);
                                float ghostZ = Config.Stream.GetSingle(objAddress + Config.ObjectSlots.GraphicsZOffset);
                                double hDistToGhost = MoreMath.GetDistanceBetween(mX, mZ, ghostX, ghostZ);
                                double angleFromGhost = MoreMath.AngleTo_AngleUnits(ghostX, ghostZ, mX, mZ);
                                (double movementSideways, double movementForwards) = MoreMath.GetComponentsFromVectorRelatively(hDistToGhost, angleFromGhost, mFacing);
                                newText = Math.Round(movementForwards, 3).ToString();
                                break;
                            }

                        case "MarioGhostSidewaysDistance":
                            {
                                float ghostX = Config.Stream.GetSingle(objAddress + Config.ObjectSlots.GraphicsXOffset);
                                float ghostZ = Config.Stream.GetSingle(objAddress + Config.ObjectSlots.GraphicsZOffset);
                                double hDistToGhost = MoreMath.GetDistanceBetween(mX, mZ, ghostX, ghostZ);
                                double angleFromGhost = MoreMath.AngleTo_AngleUnits(ghostX, ghostZ, mX, mZ);
                                (double movementSideways, double movementForwards) = MoreMath.GetComponentsFromVectorRelatively(hDistToGhost, angleFromGhost, mFacing);
                                newText = Math.Round(movementSideways, 3).ToString();
                                break;
                            }
                        
                        case "RngCallsPerFrame":
                            newText = GetNumRngCalls(objAddress).ToString();
                            break;
                    }

                    if (specialVar is AngleDataContainer)
                    {
                        var angleContainer = specialVar as AngleDataContainer;
                        if (firstObject)
                        {
                            angleContainer.ValueExists = newAngle.HasValue;
                            if (newAngle.HasValue)
                            {
                                newAngle = MoreMath.NormalizeAngleDouble(newAngle.Value);
                                angleContainer.AngleValue = newAngle.Value;
                            }
                        }

                        // Check when multiple objects have different values
                        angleContainer.ValueExists &= newAngle == angleContainer.AngleValue;
                    }
                    else if (specialVar is DataContainer)
                    {
                        var dataContainer = specialVar as DataContainer;
                        if (firstObject)
                            dataContainer.Text = newText;
                        // Check when multiple objects have different values
                        else if (dataContainer.Text != newText)
                            dataContainer.Text = "";
                    }
                }

                firstObject = false;
            }

            if (_currentAddresses.Count == 0)
            {
                foreach (IDataContainer specialVar in _specialDataControls)
                {
                    if (specialVar is AngleDataContainer)
                    {
                        var angleContainer = specialVar as AngleDataContainer;
                        angleContainer.ValueExists = false;
                    }
                    else if (specialVar is DataContainer)
                    {
                        var dataContainer = specialVar as DataContainer;
                        dataContainer.Text = "";
                    }
                }
            }
        }

        public override void Update(bool updateView)
        {
            if (!updateView)
                return;

            _releaseButton.UpdateButton();
            _interactButton.UpdateButton();
            _cloneButton.UpdateButton();
            _unloadButton.UpdateButton();

            base.Update(updateView);
            ProcessSpecialVars();
        }

        private int GetNumRngCalls(uint objAddress)
        {
            var numberOfRngObjs = Config.Stream.GetUInt32(Config.HackedAreaAddress);

            int numOfCalls = 0;

            for (int i = 0; i < Math.Min(numberOfRngObjs, Config.ObjectSlots.MaxSlots); i++)
            {
                uint rngStructAdd = (uint)(Config.HackedAreaAddress + 0x30 + 0x08 * i);
                var address = Config.Stream.GetUInt32(rngStructAdd + 0x04);
                if (address != objAddress)
                    continue;

                var preRng = Config.Stream.GetUInt16(rngStructAdd + 0x00);
                var postRng = Config.Stream.GetUInt16(rngStructAdd + 0x02);

                numOfCalls = RngIndexer.GetRngIndexDiff(preRng, postRng);
                break;
            }

            return numOfCalls;
        }
    }
}
