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
        // if you want to put this script into the game, start copying from this line ...
        #region mdk preserve
        ///////////////////////////////////////////////////////
        //  Based on Xionphs stabilized mouse-turret script  //
        ///////////////////////////////////////////////////////
        //              EDITABLE VARIABLES:                  //
        ///////////////////////////////////////////////////////

        // This script works with rotor, advanced rotor, and hinge
        // You can find the not-minified version on my github:
        // https://github.com/agneovo123/space-engineers-scripts/blob/main/SpaceEngineersScriptingProject/2-plane-gun-stabilization/Program.cs

        // sets your sensitivity
        const double sensitivity = 0.05;
        // how fast the horizontal rotor can go value: 0 to 60
        const double horizontalSpeedLimit = 60;
        // how fast the vertical rotor can go value: 0 to 60
        const double verticalSpeedLimit = 60;
        // how much the turret can turn left (set to 360 to make it unlimited)
        const double limitLeft = 360;
        // how much the turret can turn right (set to 360 to make it unlimited)
        const double limitRight = 360;
        // how much the turret can turn up (set to 360 to make it unlimited)
        const double limitUp = 40;
        // how much the turret can turn down (set to 360 to make it unlimited)
        const double limitDown = 12;

        // the name of your regular/industrial/rover/buggy cockpit
        const string CockpitName = "aTankDriver";
        // the name of your horizontal (left-right) rotor
        const string HorizName = "Rotor Horizontal";
        // the name of your vertical (up-down) rotor
        const string VertName = "Rotor Vertical";
        #endregion
        ////////////////////////////////////////////////////////////////////////////////
        //   DO NOT EDIT ANYTHING BELOW THIS LINE UNLESS YOU KNOW WHAT YOU'RE DOING   //
        ////////////////////////////////////////////////////////////////////////////////
        public Program()
        {
            Runtime.UpdateFrequency = UpdateFrequency.Update1;
        }

        bool setup, errors, firstSetup = true, rightOverTurn = false, leftOverTurn = false;
        double userVert, userHoriz, angleVert, angleHoriz, aVertDifference, aHorizDifference;
        const double mod = 360;
        IMyShipController Control;
        IMyMotorStator Horiz, Vert;
        int timer;
        Vector3D x, y, z, // forward, right, up
            xp, yp, zp; // vectors of previous frame
        Vector3D nulla = new Vector3D(0, 0, 0);
        const string echoStr = "Agneovo's 2 plane gun stabilizer script \nrunning",
            wtnStr = " with the name ",
            misStr = "` is missing.";
        public void Main(string args)
        {
            if (setup)
            {
                xp = (x == nulla) ? Control.WorldMatrix.Forward : x;
                yp = (y == nulla) ? Control.WorldMatrix.Right : y;
                zp = (z == nulla) ? Control.WorldMatrix.Up : z;

                x = Control.WorldMatrix.Forward;
                y = Control.WorldMatrix.Right;
                z = Control.WorldMatrix.Up;

                double dx = GetADif(xp, x);
                double dy = GetADif(yp, y);
                double dz = GetADif(zp, z);

                aVertDifference += ((Math.Cos(Horiz.Angle) * (dx + dz - dy) + Math.Sin(Horiz.Angle) * (dy + dz - dx)) / 2) * (GetADif(x, zp) < GetADif(xp, zp) ? 1 : -1);
                aHorizDifference += (dx + dy - dz) / 2 * (GetADif(xp, y) > GetADif(xp, yp) ? 1 : -1);

                userHoriz = Control.RotationIndicator.Y * sensitivity;
                userVert = Control.RotationIndicator.X * sensitivity;
                angleHoriz += userHoriz;
                angleVert -= userVert;

                Angle(Vert, (-aVertDifference + angleVert), -limitDown, limitUp);
                Angle2(Horiz, (-aHorizDifference + angleHoriz), -limitLeft, limitRight);

                timer++;
                if (timer > 80) { timer = 0; }
                if (timer < 20) { Echo(echoStr); }
                else if (timer < 40)
                {
                    Echo(echoStr + " .");
                    if (firstSetup) { firstSetup = false; Horiz.ApplyAction("OnOff_On"); Vert.ApplyAction("OnOff_On"); } // turns rotors back on after 20 ticks
                }
                else if (timer < 60) { Echo(echoStr + " .."); }
                else if (timer < 80) { Echo(echoStr + " ..."); }
            }
            else
            {
                errors = false;
                Control = (IMyShipController)GetBlock(CockpitName);
                Horiz = (IMyMotorStator)GetBlock(HorizName);
                Vert = (IMyMotorStator)GetBlock(VertName);
                if (Control == null) { Echo("Cockpit" + wtnStr + "`" + CockpitName + misStr); }
                if (Vert == null) { Echo("Rotor"+ wtnStr + "`" + HorizName + misStr); }
                if (Horiz == null) { Echo("Rotor" + wtnStr + "`" + VertName + misStr); }
                if (!errors)
                {
                    setup = true;
                    firstSetup = true;
                    Horiz.ApplyAction("OnOff_Off"); // turns off rotors to prevent the startup bug
                    Vert.ApplyAction("OnOff_Off"); // turns off rotors to prevent the startup bug
                }
                timer = 0;
            }
        }
        double Acos(double d) { return ToDeg(Math.Acos(d)); }
        double ToDeg(double angle) { return angle * (180.0 / Math.PI); }
        double ClampHSpeed(double number)
        {
            if (number > horizontalSpeedLimit) { return horizontalSpeedLimit; }
            if (number < -horizontalSpeedLimit) { return -horizontalSpeedLimit; }
            return number;
        }
        double ClampVSpeed(double number)
        {
            if (number > verticalSpeedLimit) { return verticalSpeedLimit; }
            if (number < -verticalSpeedLimit) { return -verticalSpeedLimit; }
            return number;
        }
        double Abs(double n) { if (n < 0) { return -n; } return n; }
        /// <summary> Get angle difference </summary>
        double GetADif(Vector3D a, Vector3D b)
        {
            if (a == b) { return 0; }
            double fak1 = (a.X * b.X) + (a.Y * b.Y) + (a.Z * b.Z);
            double aMagnitude = Math.Sqrt(a.X * a.X + a.Y * a.Y + a.Z * a.Z);
            double bMagnitude = Math.Sqrt(b.X * b.X + b.Y * b.Y + b.Z * b.Z);
            return Acos(fak1 / (aMagnitude * bMagnitude));
        }
        public void Angle(IMyMotorStator motor, double ang, double lowLimit, double highLimit)
        {
            double motorCurrentAngle = ToDeg(motor.Angle);
            if (ang > highLimit)
            {
                angleVert += userVert;
                motor.SetValueFloat("LowerLimit", (float)highLimit);
                motor.SetValueFloat("UpperLimit", (float)highLimit);
            }
            else if (ang < lowLimit)
            {
                angleVert += userVert;
                motor.SetValueFloat("LowerLimit", (float)lowLimit);
                motor.SetValueFloat("UpperLimit", (float)lowLimit);
            }
            else if (ang > motorCurrentAngle)
            {
                motor.SetValueFloat("LowerLimit", (float)lowLimit);
                motor.SetValueFloat("UpperLimit", (float)ang);
            }
            else
            {
                motor.SetValueFloat("LowerLimit", (float)ang);
                motor.SetValueFloat("UpperLimit", (float)highLimit);
            }
            motor.SetValueFloat("Velocity", (float)ClampVSpeed((ang - motorCurrentAngle) * 6f));
        }
        public void Angle2(IMyMotorStator motor, double ang, double lowLimit, double highLimit)
        {
            double motorCurrentAngle = ToDeg(motor.Angle);
            if (ang > 360 || rightOverTurn)
            {
                motorCurrentAngle %= mod;
                motor.SetValueFloat("Velocity", (float)ClampHSpeed((mod - motorCurrentAngle + (ang % mod)) * 6f));
                motor.SetValueFloat("LowerLimit", float.MinValue);
                motor.SetValueFloat("UpperLimit", float.MaxValue);
                if (!rightOverTurn)
                {
                    aHorizDifference += mod;
                }
                rightOverTurn = true;
                if (motorCurrentAngle < 60)
                {
                    rightOverTurn = false;
                }
                return;
            }
            if (ang < -360 || leftOverTurn)
            {
                motorCurrentAngle = -(motorCurrentAngle % mod);
                motor.SetValueFloat("Velocity", (float)ClampHSpeed((motorCurrentAngle - mod - (ang % mod)) * 6f));
                motor.SetValueFloat("LowerLimit", float.MinValue);
                motor.SetValueFloat("UpperLimit", float.MaxValue);
                if (!leftOverTurn)
                {
                    aHorizDifference -= mod;
                }
                leftOverTurn = true;
                if (motorCurrentAngle > -60)
                {
                    leftOverTurn = false;
                }
                return;
            }
            if (ang >= highLimit)
            {
                motor.SetValueFloat("LowerLimit", (float)0);
                motor.SetValueFloat("UpperLimit", (float)0);
            }
            else if (ang <= lowLimit)
            {
                motor.SetValueFloat("LowerLimit", (float)-0);
                motor.SetValueFloat("UpperLimit", (float)-0);
            }
            else if (ang > motorCurrentAngle)
            {
                motor.SetValueFloat("LowerLimit", (float)lowLimit);
                motor.SetValueFloat("UpperLimit", (float)ang);
            }
            else
            {
                motor.SetValueFloat("LowerLimit", (float)ang);
                motor.SetValueFloat("UpperLimit", (float)highLimit);
            }
            motor.SetValueFloat("Velocity", (float)ClampHSpeed((ang - motorCurrentAngle) * 6f));
        }
        public IMyTerminalBlock GetBlock(string name)
        {
            IMyTerminalBlock block = GridTerminalSystem.GetBlockWithName(name);
            if (block == null) { errors = true; }
            return block;
        }
        // [...] and stop copying on this line.
    }
}
