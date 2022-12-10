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
    partial class program7 : MyGridProgram
    {
        public Program()
        {
            Runtime.UpdateFrequency = UpdateFrequency.Update1;
        }
        double sensitivity = 0.05; // lower value = lower sensitivity
        double motorSpeedLimit = 60; // value: 0 to 60
        double verticalSpeedLimit = 60; // value: 0 to 60

        bool setup, errors, firstSetup = true, rightOverTurn = false, leftOverTurn = false;
        double angleVert, angleHoriz;
        double aVertDifference = 0;
        double aHorizDifference = 0;
        IMyShipController Control;
        IMyMotorStator Horiz, Vert;
        IMyTextPanel debugLCD;
        int timer;
        Vector3D x, y, z, w, //forward, right up, left
            xp, yp, zp; //previous vectors
        public void Main(string args)
        {
            if (setup)
            {
                xp = (x == null ? Control.WorldMatrix.Forward : x);
                yp = (y == null ? Control.WorldMatrix.Right : y);
                zp = (z == null ? Control.WorldMatrix.Up : z);

                x = Control.WorldMatrix.Forward;
                y = Control.WorldMatrix.Right;
                z = Control.WorldMatrix.Up;

                double dx = GetADif(xp, x);
                double dy = GetADif(yp, y);
                double dz = GetADif(zp, z);

                if (double.IsNaN(aVertDifference))
                {
                    aVertDifference = ((Math.Cos(Horiz.Angle) * (dx + dz - dy) + Math.Sin(Horiz.Angle) * (dy + dz - dx)) / 2) * (GetADif(x, zp) < GetADif(xp, zp) ? 1 : -1);
                }
                else
                {
                    aVertDifference += ((Math.Cos(Horiz.Angle) * (dx + dz - dy) + Math.Sin(Horiz.Angle) * (dy + dz - dx)) / 2) * (GetADif(x, zp) < GetADif(xp, zp) ? 1 : -1);
                }
                if (double.IsNaN(aHorizDifference))
                {
                    aHorizDifference = (dx + dy - dz) / 2 * (GetADif(xp, y) > GetADif(xp, yp) ? 1 : -1);
                }
                else
                {
                    aHorizDifference += (dx + dy - dz) / 2 * (GetADif(xp, y) > GetADif(xp, yp) ? 1 : -1);
                }

                double rHoriz = Control.RotationIndicator.Y * sensitivity;
                double rVert = Control.RotationIndicator.X * sensitivity;
                angleHoriz += rHoriz;
                angleVert -= rVert;

                debugLCD.WriteText("", false);
                //debugLCD.WriteText("\n aVertDifference: " + aVertDifference, true);
                //debugLCD.WriteText("\n aHorizDifference: " + aHorizDifference, true);
                //debugLCD.WriteText("\n", true);
                //debugLCD.WriteText("\n angleHoriz: " + angleHoriz, true);
                //debugLCD.WriteText("\n angleVert: " + angleVert, true);
                //debugLCD.WriteText("\n", true);
                //debugLCD.WriteText("\n H multi: " + (GetADif(xp, y) > GetADif(xp, yp) ? 1 : -1), true);
                //debugLCD.WriteText("\n V multi: " + (GetADif(x, zp) < GetADif(xp, zp) ? 1 : -1), true);
                //debugLCD.WriteText("\n", true);
                //debugLCD.WriteText("\n angle with: " + (-aVertDifference + angleVert), true);
                debugLCD.WriteText("\n aHorizDifference: " + aHorizDifference, true);
                debugLCD.WriteText("\n angleHoriz: " + angleHoriz, true);
                debugLCD.WriteText("\n angle2 with: " + (-aHorizDifference + angleHoriz), true);
                //debugLCD.WriteText("\n", true);
                //debugLCD.WriteText("\n dx: " + dx, true);
                //debugLCD.WriteText("\n dy: " + dy, true);
                //debugLCD.WriteText("\n dz: " + dz, true);

                Angle(Vert, (-aVertDifference + angleVert), -30, 60);
                Angle2(Horiz, (-aHorizDifference + angleHoriz), -360, 360);

                debugLCD.WriteText("\n", true);
                debugLCD.WriteText("\n Vert.Angle: " + ToDeg(Vert.Angle), true);
                debugLCD.WriteText("\n Horiz.Angle: " + ToDeg(Horiz.Angle), true);

                timer++;
                if (timer > 80) { timer = 0; }
                if (timer < 20) { Echo("Xionphs stabilized mouse-turret script \n status: running"); }
                else if (timer < 40)
                {
                    Echo("Xionphs stabilized mouse-turret script \n status: running .");
                    if (firstSetup) { firstSetup = false; Horiz.ApplyAction("OnOff_On"); Vert.ApplyAction("OnOff_On"); } // turns rotors back on after 20 ticks
                }
                else if (timer < 60) { Echo("Xionphs stabilized mouse-turret script \n status: running .."); }
                else if (timer < 80) { Echo("Xionphs stabilized mouse-turret script \n status: running ..."); }
            }
            else
            {
                errors = false;
                Control = (IMyShipController)GetBlock("aDriver");
                Horiz = (IMyMotorStator)GetBlock("Rotor Horiz");
                Vert = (IMyMotorStator)GetBlock("Rotor Vert");
                debugLCD = (IMyTextPanel)GetBlock("LCD");
                if (Control == null) { Echo("ShipController with the name `aDriver` is missing."); }
                if (Vert == null) { Echo("Rotor with the name `Elevation` is missing."); }
                if (Horiz == null) { Echo("Rotor with the name `Azimuth` is missing."); }
                if (debugLCD == null) { Echo("LCD with the name `LCD` is missing."); }
                if (!errors) { 
                    setup = true;
                    angleVert = 0;
                    angleHoriz = 0;
                    aVertDifference = 0;
                    aHorizDifference = 0;
                    Horiz.ApplyAction("OnOff_Off"); // turns off rotors to prevent the startup bug
                    Vert.ApplyAction("OnOff_Off"); // turns off rotors to prevent the startup bug
                }
                timer = 0;
            }
        }
        double Asin(double d) { return ToDeg(Math.Asin(d)); }
        double Acos(double d) { return ToDeg(Math.Acos(d)); }
        double Cos(double d) { return Math.Cos(ToRad(d)); }
        double ToRad(double angle) { return angle * (Math.PI / 180.0); }
        double ToDeg(double angle) { return angle * (180.0 / Math.PI); }
        double ClampHSpeed(double number)
        {
            if (number > motorSpeedLimit) { return motorSpeedLimit; }
            if (number < -motorSpeedLimit) { return -motorSpeedLimit; }
            return number;
        }
        double ClampVSpeed(double number)
        {
            if (number > verticalSpeedLimit) { return verticalSpeedLimit; }
            if (number < -verticalSpeedLimit) { return -verticalSpeedLimit; }
            return number;
        }
        double Abs(double n) { if (n<0) { return -n; } return n; }
        /// <summary> Get angle difference </summary>
        double GetADif(Vector3D a, Vector3D b)
        {
            double fak1 = (a.X * b.X) + (a.Y * b.Y) + (a.Z * b.Z);
            double aMagnitude = Math.Sqrt(a.X * a.X + a.Y * a.Y + a.Z * a.Z);
            double bMagnitude = Math.Sqrt(b.X * b.X + b.Y * b.Y + b.Z * b.Z);
            return Acos(fak1 / (aMagnitude * bMagnitude));
        }
        public void Angle(IMyMotorStator motor, double ang, double lowLimit, double highLimit)
        {
            double motorCurrentAngle = ToDeg(motor.Angle);
            //if (ang > highLimit) { ang = highLimit; }
            //else if (ang < lowLimit) { ang = lowLimit; }
            if (ang > motorCurrentAngle)
            {
                motor.SetValueFloat("LowerLimit", (float)lowLimit);
                motor.SetValueFloat("UpperLimit", (float)ang);
            }
            else
            {
                motor.SetValueFloat("LowerLimit", (float)ang);
                motor.SetValueFloat("UpperLimit", (float)highLimit);
            }
            motor.SetValueFloat("Velocity", (float)(ang - motorCurrentAngle) * 6f);
        }
        public void Angle2(IMyMotorStator motor, double ang, double lowLimit, double highLimit)
        {
            double mod = 360;
            double motorCurrentAngle = ToDeg(motor.Angle) % mod;
            debugLCD.WriteText("\n", true);
            debugLCD.WriteText("\n ang: " + ang, true);
            debugLCD.WriteText("\n motorCurrentAngle: " + motorCurrentAngle, true);
            debugLCD.WriteText("\n ModC: " + ((mod - motorCurrentAngle + Abs((ang % mod)-mod)) * 6f), true);
            debugLCD.WriteText("\n ModCRev: " + ((motorCurrentAngle - mod - (ang % mod)) * 6f), true);
            debugLCD.WriteText("\n V1: " + motor.TargetVelocityRPM, true);
            debugLCD.WriteText("\n righttriggered: " + rightOverTurn, true);
            if (ang > 360 || rightOverTurn)
            {
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
                //ang %= mod;
                //motorCurrentAngle %= mod;
                //angleHoriz %= mod;
            }
            else if (ang <= lowLimit)
            {
                motor.SetValueFloat("LowerLimit", (float)-0);
                motor.SetValueFloat("UpperLimit", (float)-0);
                //ang %= mod;
                //motorCurrentAngle %= mod;
                //angleHoriz %= mod;
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
            debugLCD.WriteText("\n V2: " + motor.TargetVelocityRPM, true);
            motor.SetValueFloat("Velocity", (float)ClampHSpeed((ang - motorCurrentAngle) * 6f));
        }
        public IMyTerminalBlock GetBlock(string name)
        {
            IMyTerminalBlock r = GridTerminalSystem.GetBlockWithName(name);
            if (r == null) { errors = true; }
            return r;
        }
    }
}
