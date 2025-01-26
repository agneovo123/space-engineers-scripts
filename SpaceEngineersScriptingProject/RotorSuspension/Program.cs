﻿using Sandbox.Game.EntityComponents;
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
using VRage.Game.VisualScripting;
using VRageMath;

namespace IngameScript
{
    partial class Program : MyGridProgram
    {
        #region mdk preserve
        ////////////////////////////////////////////////////////////////
        //         inspired by Rdav's Guided Missile Script           //
        ////////////////////////////////////////////////////////////////
        const string GroupName = "rotors";
        const double setPoint = 0;
        const double P = 0.01; // gain (between 0 and 1)
        const double I = 0; // i don't know / isn't used
        const double D = 0.1; // damping gain (in seconds)
        ////////////////////////////////////////////////////////////////
        //                DON'T EDIT BELOW THIS LINE                  //
        ////////////////////////////////////////////////////////////////
        #endregion
        const double Timestep = 0.016; // how many seconds in a timestep
        const double InverseTimestep = 60;   // how many timesteps in a second
        class PID
        {
            double error;
            double integral; // idk
            double derivative;
            double errorPrev;

            double KP;
            double KI;
            double KD;
            double setPoint;

            public PID(double P, double I, double D, double setPoint_in)
            {
                KP = P;
                KI = I;
                KD = D;
                setPoint = setPoint_in;
            }

            public double getDriveCommand(double CurrentValue)
            {
                // update errorprev
                errorPrev = error;
                // update error
                error = setPoint - CurrentValue;
                // update integral
                integral += error * Timestep;
                // update derivative
                //derivative = CurrentValue + (KD * InverseTimestep) * (errorPrev - error); // why did I add to CV, and *KD ??? what
                derivative = InverseTimestep * (errorPrev - error);

                // create driveCommand
                double driveCommand = -(KP * error + KI * integral + KD * derivative);
                //if (Math.Abs(error) < 0.01 && Math.Abs(driveCommand) < 0.01) { return 0; }
                return driveCommand;
            }
        }
        public Program()
        {
            Runtime.UpdateFrequency = UpdateFrequency.Update1;
        }

        public void Main(string argument)
        {
            List<IMyTerminalBlock> blocks = GetBlockGroup(GroupName);
            PID[] pids = new PID[blocks.Count];
            double driveCommand;
            double currentValue;
            for (int i = 0; i < blocks.Count; i++)
            {
                pids[i] = new PID(P, I, D, setPoint);
                currentValue = ToDeg((blocks[i] as IMyMotorStator).Angle);
                driveCommand = pids[i].getDriveCommand(currentValue);
                Echo(String.Format("{0} - {1:00.000} - {2:00.000}", i, currentValue, driveCommand));
                (blocks[i] as IMyMotorStator).TargetVelocityRad = (float)ToRad(driveCommand);
            }
        }
        float ToDeg(float rad) { return MathHelper.ToDegrees(rad); }
        double ToDeg(double rad) { return MathHelper.ToDegrees(rad); }
        float ToRad(float deg) { return MathHelper.ToRadians(deg); }
        double ToRad(double deg) { return MathHelper.ToRadians(deg); }

        public List<IMyTerminalBlock> GetBlockGroup(string name)
        {
            List<IMyTerminalBlock> blocks = new List<IMyTerminalBlock>();
            GridTerminalSystem.GetBlockGroupWithName(name).GetBlocks(blocks);
            //for (int i = 0; i < blocks.Count; i++) { Echo(blocks[i].CustomName); }
            return blocks;
        }
        public IMyTerminalBlock GetBlock(string name)
        {
            IMyTerminalBlock block = GridTerminalSystem.GetBlockWithName(name);
            return block;
        }
    }
}
