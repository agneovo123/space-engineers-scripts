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
        public Program()
        {
            // The constructor, called only once every session and
            // always before any other method is called. Use it to
            // initialize your script. 
            //     
            // The constructor is optional and can be removed if not
            // needed.
            // 
            // It's recommended to set Runtime.UpdateFrequency 
            // here, which will allow your script to run itself without a 
            // timer block.
        }
        MyCommandLine _commandLine = new MyCommandLine();
        const double degToRadMultiplier = (Math.PI / 180.0);
        List<IMyTerminalBlock> blocks = new List<IMyTerminalBlock>();
        public void Main(string argument)
        {
            Echo("Last action:");
            if (_commandLine.TryParse(argument))
            {
                // get the group
                IMyBlockGroup group = GridTerminalSystem.GetBlockGroupWithName(_commandLine.Argument(0));
                if (group == null)
                {
                    Echo("ERROR: Group not found");
                    return;
                }
                group.GetBlocks(blocks);
                Echo($"group: \"{group.Name}\" (contains {blocks.Count()} blocks):");

                // debug print
                //foreach (var block in blocks) { Echo($"- {block.CustomName}"); }

                // loop through all arguments
                for (int i = 1; i < _commandLine.ArgumentCount; i++)
                {
                    string keyword = _commandLine.Argument(i).Split(':')[0];
                    float value = Convert.ToSingle(_commandLine.Argument(i).Split(':')[1]);
                    // apply changes based on keywords
                    switch (keyword)
                    {
                        case "friction": SetFriction(value); break;
                        case "height": SetHeight(value); break;
                        case "maxsteer": SetMaxSteerAngle(value); break;
                        case "power": SetPower(value); break;
                        case "strength": SetStrength(value); break;
                        // 1 letter args
                        case "f": SetFriction(value); break;
                        case "h": SetHeight(value); break;
                        case "m": SetMaxSteerAngle(value); break;
                        case "p": SetPower(value); break;
                        case "s": SetStrength(value); break;
                    }
                    // Friction 0-100
                    // Height (in meters)
                    // MaxSteerAngle radians
                    // Power 0-100%
                    // Strength 0-100%
                }
            }
            else { Echo("there was no argument"); }
        }
        public void SetFriction(float friction)
        {
            Echo($"setting friction to: [{friction}]");
            foreach (IMyMotorSuspension block in blocks.Cast<IMyMotorSuspension>())
            {
                block.Friction = friction;
            }
        }
        public void SetHeight(float height)
        {
            Echo($"setting height to: [{height}]");
            foreach (IMyMotorSuspension block in blocks.Cast<IMyMotorSuspension>())
            {
                block.Height = height;
            }
        }
        public void SetMaxSteerAngle(float maxSteer)
        {
            Echo($"setting maxSteerAngle to: [{maxSteer}]");
            foreach (IMyMotorSuspension block in blocks.Cast<IMyMotorSuspension>())
            {
                block.MaxSteerAngle = maxSteer * Convert.ToSingle(degToRadMultiplier);
            }
        }
        public void SetPower(float power)
        {
            Echo($"setting power to: [{power}]");
            foreach (IMyMotorSuspension block in blocks.Cast<IMyMotorSuspension>())
            {
                block.Power = power;
            }
        }
        public void SetStrength(float strength)
        {
            Echo($"setting strength to: [{strength}]");
            foreach (IMyMotorSuspension block in blocks.Cast<IMyMotorSuspension>())
            {
                block.Strength = strength;
            }
        }
    }
}
