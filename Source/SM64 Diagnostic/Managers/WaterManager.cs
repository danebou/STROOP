﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SM64_Diagnostic.Structs;
using System.Windows.Forms;
using SM64_Diagnostic.Utilities;
using SM64_Diagnostic.Extensions;
using SM64_Diagnostic.Controls;
using SM64_Diagnostic.Structs.Configurations;

namespace SM64_Diagnostic.Managers
{
    public class WaterManager : DataManager
    {
        public WaterManager(List<WatchVariable> watchVariables, NoTearFlowLayoutPanel variableTable)
            : base(watchVariables, variableTable)
        {
        }

        protected override List<SpecialWatchVariable> _specialWatchVars { get; } = new List<SpecialWatchVariable>()
        {
            new SpecialWatchVariable("WaterAboveMedian"),
            new SpecialWatchVariable("MarioAboveWater"),
        };

        private void ProcessSpecialVars()
        {
            foreach (var specialVar in _specialDataControls)
            {
                switch(specialVar.SpecialName)
                {
                    case "WaterAboveMedian":
                        {
                            short waterLevel = Config.Stream.GetInt16(Config.Mario.StructAddress + Config.Mario.WaterLevelOffset);
                            short waterLevelMedian = Config.Stream.GetInt16(Config.WaterLevelMedianAddress);
                            (specialVar as DataContainer).Text = Math.Round((double)(waterLevel - waterLevelMedian), 3).ToString();
                            break;
                        }

                    case "MarioAboveWater":
                        {
                            short waterLevel = Config.Stream.GetInt16(Config.Mario.StructAddress + Config.Mario.WaterLevelOffset);
                            float marioY = Config.Stream.GetSingle(Config.Mario.StructAddress + Config.Mario.YOffset);
                            (specialVar as DataContainer).Text = Math.Round(marioY - waterLevel, 3).ToString();
                            break;
                        }
                }
            }
        }

        public override void Update(bool updateView)
        {
            if (!updateView)
                return;

            base.Update();
            ProcessSpecialVars();
        }

    }
}
