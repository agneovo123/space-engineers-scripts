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
        #region mdk preserve
        // Select your programmable block from a controller(cockpit/chair/etc)'s the G menu with the 'run' option.
        // It will ask for arguments
        // note: arguments are separated with spaces ' ', if your programmable block has has spaces in it's name, just put it in quotes example: "Programmable Block Gun stab"
        // DO NOT use ';' in your arguments
        // the first argument HAS TO BE the name of your programmable block
        // the rest of the arguments will be written into your programmable block's customData field.
        // to pass a flag/switch you write '-' before it, example: '"Programmable Block Gun stab" -resetGun'
        // will put '-resetGun' in "Programmable Block Gun stab"'s CustomData
        ////////////////////////////////////////////////////////////////////////////////
        //                 DO NOT EDIT ANYTHING BELOW THIS LINE                       //
        ////////////////////////////////////////////////////////////////////////////////
        public Program(){}
        #endregion
        MyCommandLine _commandLine = new MyCommandLine();
        public void Main(string argument, UpdateType updateSource)
        {
            if (_commandLine.TryParse(argument))
            {
                if (_commandLine.Argument(0) == null)
                {
                    Echo("No argument! Refer to the beginning of the script for instructions.");
                }
            }
            if (argument != null && argument != "")
            {
                if (_commandLine.Argument(0).Contains(" "))
                {
                    argument = argument.Substring(_commandLine.Argument(0).Length + 3, argument.Length - _commandLine.Argument(0).Length - 3);
                }
            }
            string name = _commandLine.Argument(0);
            string arguments = "";
            for (int i = 1; i < _commandLine.ArgumentCount; i++)
            {
                arguments += _commandLine.Argument(i);
                if (i + 1 < _commandLine.ArgumentCount)
                {
                    arguments += ";";
                }
            }
            string[] switches = _commandLine.Switches.ToArray();
            for (int i = 0; i < switches.Length; i++)
            {
                if (i == 0)
                {
                    arguments += ";";
                }
                arguments += "-" + switches[i];
                if (i + 1 < switches.Length)
                {
                    arguments += ";";
                }
            }

            IMyProgrammableBlock block = (IMyProgrammableBlock)GridTerminalSystem.GetBlockWithName(name);
            if (block == null)
            {
                Echo("Programmable block with name \'" + name + "\' not found!");
            }
            else
            {
                block.CustomData = arguments;
                string validArgs = block.CustomData;
                if (arguments == validArgs)
                {
                    Echo("Arguments passed to \'" + name + "\': " + arguments);
                }
                else
                {
                    Echo("Arguments unsuccessfully passed to \'" + name + "\'");
                }
            }
        }
    }
}
