using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;
using Sandbox.Game.EntityComponents;
using Sandbox.ModAPI.Ingame;
using Sandbox.ModAPI.Interfaces;
using SpaceEngineers.Game.ModAPI.Ingame;
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
            Runtime.UpdateFrequency = UpdateFrequency.Update1;
        }

        Vector3 gunMatrixNew;
        Vector3 gunMatrixOld;

        Vector3 remoteNew;
        Vector3 remoteOld;
        public void Main(string argument, UpdateType updateSource)
        {
            IMySmallGatlingGun gun = GridTerminalSystem.GetBlockWithName("Autocannon") as IMySmallGatlingGun;
            //IMyMotorRotor horizontal = GridTerminalSystem.GetBlockWithName("Rotor Horiz") as IMyMotorRotor;
            IMyMotorStator horizontal = GridTerminalSystem.GetBlockWithName("Rotor Horiz") as IMyMotorStator;
            IMyRemoteControl remote = GridTerminalSystem.GetBlockWithName("Remote Control") as IMyRemoteControl;
            //shoot direction: towards -Z
            //Echo(gun.CubeGrid.WorldMatrix.Forward.ToString());
            gunMatrixOld = gunMatrixNew;
            gunMatrixNew = gun.CubeGrid.WorldMatrix.Forward;
            remoteOld = remoteNew;
            remoteNew = remote.CubeGrid.WorldMatrix.Forward;
            float rd1 = remoteNew.X - remoteOld.X;
            float rd2 = remoteNew.Y - remoteOld.Y;
            float rd3 = remoteNew.Z - remoteOld.Z;
            float maxrd = max3(rd1, rd2, rd3);

            float d1 = gunMatrixNew.X - gunMatrixOld.X;
            float d2 = gunMatrixNew.Y - gunMatrixOld.Y;
            float d3 = gunMatrixNew.Z - gunMatrixOld.Z;
            float maxd = max3(d1, d2, d3);
            if (maxrd > 0)
            {
                horizontal.TargetVelocityRPM = min(30, maxrd);
                Echo(horizontal.TargetVelocityRPM.ToString());
            }
            if (maxrd < 0)
            {
                horizontal.TargetVelocityRPM = min(-30, -maxrd);
                Echo(horizontal.TargetVelocityRPM.ToString());
            }
            //Echo(gun.GetPosition().ToString());
        }
        private float max3(float a, float b, float c)
        {
            if (a > b && a > c)
                return a;
            if (b > a && b > c)
                return b;
            if (c > a && c > b)
                return c;
            return 0;
        }
        private float max(float a, float b)
        {
            if (a > b)
                return a;
            return b;
        }
        private float min(float a, float b)
        {
            if (a < b)
                return a;
            return b;
        }
    }
}
