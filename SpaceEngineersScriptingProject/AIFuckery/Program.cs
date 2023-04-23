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
        public Program() { Runtime.UpdateFrequency = UpdateFrequency.Update1; }

        string cockpitName = "Cockpit";
        string turretMainRotorName = "TurretMainRotor";
        string turretGunRotorName = "TurretGunRotor";
        float sensitivity = 2.0f;
        float torque = 0.1f;

        IMyShipController cockpit;
        IMyMotorStator turretMainRotor;
        IMyMotorStator turretGunRotor;

        Vector2 mouseInput;

        public void Main(string args)
        {
            FindBlocks();

            // Get mouse input
            mouseInput = cockpit.RotationIndicator;

            // Stabilize turret
            Vector3D turretForward = turretMainRotor.WorldMatrix.Backward;
            Vector3D turretDown = turretMainRotor.WorldMatrix.Right;
            Vector3D gravity = cockpit.GetNaturalGravity();
            Vector3D stabilizer = Vector3D.Normalize(Vector3D.Cross(gravity, turretForward));
            //Vector3D turretTorque = Vector3D.Cross(stabilizer, turretForward) * turretMainRotor.TargetVelocityRPM * torque;
            Vector3D e = Vector3D.Cross(gravity, stabilizer);
            float turretTorque = torque * ((GetADif(e, turretDown) > GetADif(e, gravity)) ? 1 : -1);
            //turretMainRotor.ApplyForce(turretTorque);
            turretMainRotor.SetValueFloat("Velocity", turretTorque);

            // Limit turret elevation
            float maxElevation = MathHelper.ToRadians(25);
            float minElevation = MathHelper.ToRadians(-10);
            float elevation = MathHelper.Clamp(turretGunRotor.Angle, minElevation, maxElevation);

            // Calculate turret rotation and elevation angles
            float yaw = MathHelper.ToRadians((float)(mouseInput.Y * sensitivity));
            float pitch = MathHelper.ToRadians((float)(-mouseInput.X * sensitivity));

            // Apply rotation and elevation to rotors
            turretMainRotor.TargetVelocityRad = yaw;
            turretGunRotor.TargetVelocityRad = pitch;
            //turretGunRotor.TargetAngle = elevation;
            //turretGunRotor.SetValueFloat("LowerLimit", (float)elevation);
            //turretGunRotor.SetValueFloat("UpperLimit", (float)elevation);
        }
        double GetADif(Vector3D a, Vector3D b)
        {
            if (a == b) { return 0; }
            double fak1 = (a.X * b.X) + (a.Y * b.Y) + (a.Z * b.Z);
            double aMagnitude = Math.Sqrt(a.X * a.X + a.Y * a.Y + a.Z * a.Z);
            double bMagnitude = Math.Sqrt(b.X * b.X + b.Y * b.Y + b.Z * b.Z);
            return Math.Acos(fak1 / (aMagnitude * bMagnitude));
        }
        void FindBlocks()
        {
            // Find cockpit block
            cockpit = (IMyShipController)GridTerminalSystem.GetBlockWithName(cockpitName);
            Echo($"cockpit block '{cockpit.CustomName}' not found!");
            if (cockpit == null)
            {
                Echo($"Error: cockpit block '{cockpitName}' not found!");
            }

            // Find turret main rotor block
            turretMainRotor = (IMyMotorStator)GridTerminalSystem.GetBlockWithName("TurretMainRotor");
            Echo($"\nturret main rotor block '{Convert.ToString(GridTerminalSystem.GetBlockWithName("TurretMainRotor").CustomName)}'\n");
            if (turretMainRotor == null)
            {
                Echo($"Error: turret main rotor block '{turretMainRotorName}' not found!");
            }

            // Find turret gun rotor block
            turretGunRotor = (IMyMotorStator)GridTerminalSystem.GetBlockWithName("TurretGunRotor");
            if (turretGunRotor == null)
            {
                Echo($"Error: turret gun rotor block '{turretGunRotorName}' not found!");
            }

            // Check if all components are found
            if (cockpit == null || turretMainRotor == null || turretGunRotor == null)
            {
                Echo("Error: missing required components, exiting script.");
                return;
            }
        }
    }
}
