using Sandbox.Game.EntityComponents;
using Sandbox.ModAPI.Ingame;
using Sandbox.ModAPI.Interfaces;
using SpaceEngineers.Game.ModAPI.Ingame;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;
using VRage;
using VRage.Collections;
using VRage.Game;
using VRage.Game.Components;
using VRage.Game.GUI.TextPanel;
using VRage.Game.ModAPI.Ingame;
using VRage.Game.ModAPI.Ingame.Utilities;
using VRage.Game.ObjectBuilders.Definitions;
using VRageMath;

namespace IngameScript
{
    partial class Program : MyGridProgram
    {

        //TODO:
        // - argument handling
        // - arguments for reset & sensitivity
        // if you want to put this script into the game, start copying from this line ...
        #region mdk preserve
        ///////////////////////////////////////////////////////
        //            CUSTOMIZABLE VARIABLES:                //
        ///////////////////////////////////////////////////////

        // You can find the not-minified version on my github:
        // https://github.com/agneovo123/space-engineers-scripts/

        // sets your sensitivity
        const double sensitivity = 0.05;
        // how fast the horizontal rotor can go value: 0 to 60
        const double horizontalSpeedLimit = 30;
        // how fast the vertical rotor can go value: 0 to 60
        const double verticalSpeedLimit = 30;
        // the next 4 variables range from -360 to 360
        // how much the turret can turn left (set to -360 to make it unlimited)
        const double limitLeft = -360;
        // how much the turret can turn right (set to 360 to make it unlimited)
        const double limitRight = 360;
        // how much the turret can turn up
        const double limitUp = 40;
        // how much the turret can turn down
        const double limitDown = -12;

        // the name of your regular/industrial/rover/buggy cockpit
        const string CockpitName = "aTankDriver";
        // the name of your horizontal (left-right) rotor
        const string HorizName = "Rotor Horizontal";
        // the name of your vertical (up-down) rotor
        const string VertName = "Rotor Vertical";
        ////////////////////////////////////////////////////////////////////////////////
        //                 DO NOT EDIT ANYTHING BELOW THIS LINE                       //
        ////////////////////////////////////////////////////////////////////////////////
        public Program() { Runtime.UpdateFrequency = UpdateFrequency.Update1; }
        #endregion
    }
}
