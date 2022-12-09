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
    partial class Program4_frames : MyGridProgram
    {
        public Program()
        {
            Runtime.UpdateFrequency = UpdateFrequency.Update1;
        }
        double sensitivity = 0.05; // lower value = lower sensitivity
        bool setup, errors;
        double angleVert = 0,
            angleHoriz = 0,
            aVertDifference = 0.0,
            aHorizDifference = 0.0,
            cosang = 0,
            sinang = 0;
        IMyShipController Control;
        IMyMotorStator Horiz, Vert;
        IMyTextPanel debugLCD;
        IMySmallGatlingGun Gun;
        int timer;
        Vector3D x, y, z, w, //forward, right up, left
            xp, yp, zp, wp, //previous vectors
            ax, ay, az, //absolute (starting) vectors
            nulla = new Vector3D(0.0, 0.0, 0.0); //nullvector
        double dxysign = 0, dxysingp = 0;
        bool trigger = false;
        public void Main(string args)
        {
            if (setup)
            {
                if (x == null)
                    xp = Control.WorldMatrix.Forward;
                else
                    xp = x;
                if (y == null)
                    yp = Control.WorldMatrix.Right;
                else
                    yp = y;
                if (z == null)
                    zp = Control.WorldMatrix.Up;
                else
                    zp = z;
                if (w == null)
                    wp = Control.WorldMatrix.Left;
                else
                    wp = w;

                x = Control.WorldMatrix.Forward;
                y = Control.WorldMatrix.Right;
                z = Control.WorldMatrix.Up;
                w = Control.WorldMatrix.Left;

                double dx = GetAngleDifference(x, xp);
                double dy = GetAngleDifference(y, yp);
                double dz = GetAngleDifference(z, zp);

                double hsign = 1;

                double dxs = GetAngleDifference(x, xp);
                double dys = GetAngleDifference(y, yp);
                double dzs = GetAngleDifference(z, zp);
                if (GetAngleDifference(x, yp) > GetAngleDifference(xp, yp))// && GetAngleDifference(y, xp) < GetAngleDifference(yp, xp))
                {
                    hsign = -1;
                }
                cosang = Math.Cos(Horiz.Angle);
                sinang = Math.Sin(Horiz.Angle);
                //if (cosang >= 0.5 && GetAngleDifference(z, xp) > GetAngleDifference(zp, xp))
                //{
                //    dzs = -dzs;
                //}
                //if (sinang >= 0.5 && GetAngleDifference(z, yp) > GetAngleDifference(zp, yp))
                //{
                //    dzs = -dzs;
                //}
                //if (cosang <= -0.5 && GetAngleDifference(z, xp) > GetAngleDifference(zp, xp))
                //{
                //    //dzs = dzs; stay the same
                //}
                //if (sinang <= -0.5 && GetAngleDifference(z, yp) > GetAngleDifference(zp, yp))
                //{
                //    //dzs = dzs;
                //}

                if (double.IsNaN(aVertDifference))
                    aVertDifference = (cosang * (dxs + dzs - dys) + sinang * (dys + dzs - dxs)) / 2;
                else
                    aVertDifference = (aVertDifference + (cosang * (dxs + dzs - dys) + sinang * (dys + dzs - dxs)) / 2);
                if (double.IsNaN(aHorizDifference))
                    aHorizDifference = (dx + dy - dz) * hsign / 2;
                else
                    aHorizDifference = (aHorizDifference + (dxs + dys - dzs) * hsign / 2);

                double rHorizInput = Control.RotationIndicator.Y * sensitivity;
                double rVertInput = Control.RotationIndicator.X * sensitivity;
                angleHoriz += rHorizInput;
                angleVert -= rVertInput;
                debugLCD.WriteText("", false);
                //debugLCD.WriteText("\n cosang: " + cosang, true);
                //debugLCD.WriteText("\n sinang: " + sinang, true);
                //debugLCD.WriteText("\n", true);
                //debugLCD.WriteText("\n dx: " + dx, true);
                //debugLCD.WriteText("\n dy: " + dy, true);
                //debugLCD.WriteText("\n dz: " + dz, true);
                //debugLCD.WriteText("\n dxs: " + dxs, true);
                //debugLCD.WriteText("\n dys: " + dys, true);
                //debugLCD.WriteText("\n dzs: " + dzs, true);
                //debugLCD.WriteText("\n", true);
                //debugLCD.WriteText("\n aVertDifference: " + aVertDifference, true);
                //debugLCD.WriteText("\n aHorizDifference: " + aHorizDifference, true);
                //debugLCD.WriteText("\n hsign: " + hsign, true);
                //debugLCD.WriteText("\n", true);
                //debugLCD.WriteText("\n double.IsNaN(aVertDifference): " + double.IsNaN(aVertDifference), true);
                //debugLCD.WriteText("\n double.IsNaN(aHorizDifference): " + double.IsNaN(aHorizDifference), true);
                Angle(Vert, (-aVertDifference + angleVert), -21, 60);
                Angle2(Horiz, (-aHorizDifference + angleHoriz), -360, 360);
                debugLCD.WriteText("\n", true);
                debugLCD.WriteText("\n Vert.Angle: " + ToDeg(Vert.Angle), true);
                debugLCD.WriteText("\n Horiz.Angle: " + ToDeg(Horiz.Angle), true);
                //debugLCD.WriteText("\n", true);
                //debugLCD.WriteText("\n dx: " + dx, true);
                //debugLCD.WriteText("\n dy: " + dy, true);
                //debugLCD.WriteText("\n dz: " + dz, true);
                //debugLCD.WriteText("\n", true);
                //debugLCD.WriteText("\n GetAngleDifference(ax, y): " + GetAngleDifference(ax, y), true);
                //debugLCD.WriteText("\n GetAngleDifference(ax, w): " + GetAngleDifference(ax, w), true);
                //debugLCD.WriteText("\n GetAngleDifference(ax, x): " + GetAngleDifference(ax, x), true);
                //debugLCD.WriteText("\n dxysign: " + dxysign, true);

                timer++;
                if (timer > 80) { timer = 0; }
                if (timer < 20) { Echo("Xionphs stabilized mouse-turret script \n status: running"); }
                else if (timer < 40) { Echo("Xionphs stabilized mouse-turret script \n status: running ."); }
                else if (timer < 60) { Echo("Xionphs stabilized mouse-turret script \n status: running .."); }
                else if (timer < 80) { Echo("Xionphs stabilized mouse-turret script \n status: running ..."); }
            }
            else
            {
                errors = false;
                Control = (IMyShipController)GetBlock("a Rover Cockpit");
                Horiz = (IMyMotorStator)GetBlock("Rotor Horiz");
                Vert = (IMyMotorStator)GetBlock("Rotor Vert");
                debugLCD = (IMyTextPanel)GetBlock("LCD");
                Gun = (IMySmallGatlingGun)GetBlock("Elite Gatling Gun");
                if (Control == null) { Echo("ShipController with the name `Driver` is missing."); }
                if (Vert == null) { Echo("Rotor with the name `Elevation` is missing."); }
                if (Horiz == null) { Echo("Rotor with the name `Azimuth` is missing."); }
                if (debugLCD == null) { Echo("LCD with the name `LCD` is missing."); }
                if (Gun == null) { Echo("Gun with the name `Elite Gatling Gun` is missing."); }
                if (!errors) { setup = true; }
                timer = 0;

            }
        }
        double Asin(double d) { return ToDeg(Math.Asin(d)); }
        double Acos(double d) { return ToDeg(Math.Acos(d)); }
        double Cos(double d) { return Math.Cos(ToRad(d)); }
        double ToRad(double angle) { return angle * (Math.PI / 180.0); }
        double ToDeg(double angle) { return angle * (180.0 / Math.PI); }
        double GetAngleDifference(Vector3D a, Vector3D b)
        {
            double fak1 = (a.X * b.X) + (a.Y * b.Y) + (a.Z * b.Z);
            double aMagnitude = Math.Sqrt(a.X * a.X + a.Y * a.Y + a.Z * a.Z);
            double bMagnitude = Math.Sqrt(b.X * b.X + b.Y * b.Y + b.Z * b.Z);
            return Acos(fak1 / (aMagnitude * bMagnitude));
        }
        public void Angle(IMyMotorStator motor, double ang, double lowLimit, double highLimit)
        {
            double motorCurrentAngle = ToDeg(motor.Angle);
            if (ang > highLimit) { ang = highLimit; }
            else if (ang < lowLimit) { ang = lowLimit; }
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
            //debugLCD.WriteText("", false);
            double motorCurrentAngle = ToDeg(motor.Angle);
            double mod = 360;
            debugLCD.WriteText("\nang1: " + (float)ang, true);
            debugLCD.WriteText("\nmotorCurrentAngle1: " + (float)motorCurrentAngle, true);
            if (ang >= highLimit)
            {
                motor.SetValueFloat("LowerLimit", (float)0);
                motor.SetValueFloat("UpperLimit", (float)0);
                ang %= mod;
                motorCurrentAngle %= mod;
                angleHoriz %= mod;
            }
            else if (ang <= lowLimit)
            {
                motor.SetValueFloat("LowerLimit", (float)-0);
                motor.SetValueFloat("UpperLimit", (float)-0);
                ang %= mod;
                motorCurrentAngle %= mod;
                angleHoriz %= mod;
            }
            else if (ang > motorCurrentAngle)
            {
                motor.SetValueFloat("LowerLimit", (float)lowLimit);
                motor.SetValueFloat("UpperLimit", (float)ang);
                ang %= mod;
                motorCurrentAngle %= mod;
                angleHoriz %= mod;
            }
            else
            {
                motor.SetValueFloat("LowerLimit", (float)ang);
                motor.SetValueFloat("UpperLimit", (float)highLimit);
                ang %= mod;
                motorCurrentAngle %= mod;
                angleHoriz %= mod;
            }
            debugLCD.WriteText("\nlowlim: " + (float)motor.LowerLimitDeg, true);
            debugLCD.WriteText("\nhighlim: " + (float)motor.UpperLimitDeg, true);
            debugLCD.WriteText("\nang2: " + (float)ang, true);
            debugLCD.WriteText("\nmotorCurrentAngle2: " + (float)motorCurrentAngle, true);
            motor.SetValueFloat("Velocity", Math.Min((float)(ang - motorCurrentAngle) * 6f, 60));
            debugLCD.WriteText("\nVelocity: " + motor.TargetVelocityRPM, true);
        }
        public IMyTerminalBlock GetBlock(string name)
        {
            IMyTerminalBlock r = GridTerminalSystem.GetBlockWithName(name);
            if (r == null) { errors = true; }
            return r;
        }
    }
}
