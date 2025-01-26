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
using VRage.Game.VisualScripting;
using VRageMath;

namespace IngameScript
{
    partial class Program : MyGridProgram
    {
        #region mdk preserve
        //////////////////////////////////////////////////////////////////////
        // DOESN'T WORK DOESN'T WORK DOESN'T WORK DOESN'T WORK DOESN'T WORK //
        // DOESN'T WORK DOESN'T WORK DOESN'T WORK DOESN'T WORK DOESN'T WORK //
        // DOESN'T WORK DOESN'T WORK DOESN'T WORK DOESN'T WORK DOESN'T WORK //
        // DOESN'T WORK DOESN'T WORK DOESN'T WORK DOESN'T WORK DOESN'T WORK //
        // DOESN'T WORK DOESN'T WORK DOESN'T WORK DOESN'T WORK DOESN'T WORK //
        // DOESN'T WORK DOESN'T WORK DOESN'T WORK DOESN'T WORK DOESN'T WORK //
        // DOESN'T WORK DOESN'T WORK DOESN'T WORK DOESN'T WORK DOESN'T WORK //
        //////////////////////////////////////////////////////////////////////
        // pistons really don't like to behave like suspensions DO NOT USE THIS
        const string groupName = "pistons";
        const double setPoint = 1;
        const double P = 0.01; // gain (between 0 and 1)
        const double I = 0; // i don't know / isn't used
        const double D = 0.1; // damping gain (in seconds)
        const float tartStrength = 100;
        const float strengthMult = 100;
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
            Runtime.UpdateFrequency = UpdateFrequency.Update10;
        }
        bool odd = false;
        public void Main(string argument)
        {
            List<IMyTerminalBlock> blocks = GetBlockGroup(groupName);

            PID[] pids = new PID[blocks.Count];
            double driveCommand, currentValue, currentForce, driveCmd2;
            for (int i = 0; i < blocks.Count; i++)
            {
                pids[i] = new PID(P, I, D, setPoint);
                currentValue = (blocks[i] as IMyExtendedPistonBase).CurrentPosition;
                currentForce = blocks[i].GetValue<float>("MaxImpulseAxis");
                driveCommand = pids[i].getDriveCommand(currentValue);

                if (odd)
                {
                    driveCommand = -driveCommand / 2;
                }
                Echo(String.Format("{0} | {1:0.000} | {2:00.000} | {3:0.0}", i, currentValue, driveCommand, currentForce));
                (blocks[i] as IMyExtendedPistonBase).Velocity = (float)driveCommand;

                driveCmd2 = tartStrength + (float)Math.Abs(driveCommand) * strengthMult;
                blocks[i].SetValue<float>("MaxImpulseAxis", (float)driveCmd2);
                odd = !odd;
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
