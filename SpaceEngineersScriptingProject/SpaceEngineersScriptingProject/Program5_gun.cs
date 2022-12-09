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
    partial class Program5_gun : MyGridProgram
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
        IMySmallMissileLauncherReload Gun;
        int timer;
        Vector3D x, y, z, //gun forward, right up, left
            xp, yp, zp, //gun previous vectors
            tpx, tpy, tpz, //initialised to Control(vehicle).forward
            nulla = new Vector3D(0.0, 0.0, 0.0); //nullvector
        double dY, dP; // difference in Yaw and Pitch
        bool trigger = false;
        public void Main(string args)
        {
            if (setup)
            {
                xp = (x != null) ? x : Gun.WorldMatrix.Forward;
                yp = (y != null) ? y : Gun.WorldMatrix.Right;
                zp = (z != null) ? z : Gun.WorldMatrix.Up;
                x = Gun.WorldMatrix.Forward;
                y = Gun.WorldMatrix.Right;
                z = Gun.WorldMatrix.Up;

                //xp = new Vector3D(tpx.X, x.Y, x.Z);
                //yp = new Vector3D(y.X, tpx.Y, y.Z);
                //zp = new Vector3D(z.X, z.Y, tpx.Z);

                double dx = GetADiff(x, xp);
                double dy = GetADiff(y, yp);
                double dz = GetADiff(z, zp);

                dY = Abs(dx + dy - 2*dz) * (GetADiff(x, yp) < GetADiff(xp, yp) ? 1 : -1);
                dP = Abs(dx + dz - 2*dy) * (GetADiff(x, zp) < GetADiff(xp, zp) ? 1 : -1);

                double rHorizInput = Control.RotationIndicator.Y * sensitivity;
                double rVertInput = Control.RotationIndicator.X * sensitivity;
                //targetPoint += new Vector3D(rVertInput, rHorizInput, 0.0);
                angleHoriz += rHorizInput;
                angleVert -= rVertInput;

                Angle(Vert, (dP + angleVert), -12, 40);
                Angle2(Horiz, (dY + angleHoriz), -360, 360);

                debugLCD.WriteText("", false);
                debugLCD.WriteText("\n", true);
                debugLCD.WriteText("\n Vert.Angle: " + ToDeg(Vert.Angle), true);
                debugLCD.WriteText("\n Horiz.Angle: " + ToDeg(Horiz.Angle), true);
                debugLCD.WriteText("\n", true);
                debugLCD.WriteText("\n dx: " + dx, true);
                debugLCD.WriteText("\n dy: " + dy, true);
                debugLCD.WriteText("\n dz: " + dz, true);
                debugLCD.WriteText("\n", true);
                debugLCD.WriteText("\n rHorizInput: " + rHorizInput, true);
                debugLCD.WriteText("\n rVertInput: " + rVertInput, true);
                debugLCD.WriteText("\n", true);
                debugLCD.WriteText("\n gunf x: " + x.X, true);
                debugLCD.WriteText("\n gunf y: " + x.Y, true);
                debugLCD.WriteText("\n gunf z: " + x.Z, true);
                debugLCD.WriteText("\n", true);
                debugLCD.WriteText("\n tP.X: " + tpx.X, true);
                debugLCD.WriteText("\n tP.Y: " + tpx.Y, true);
                debugLCD.WriteText("\n tP.Z: " + tpx.Z, true);

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
                Control = (IMyShipController)GetBlock("aDriver");
                Horiz = (IMyMotorStator)GetBlock("Rotor Horiz");
                Vert = (IMyMotorStator)GetBlock("Rotor Vert");
                debugLCD = (IMyTextPanel)GetBlock("LCD");
                Gun = (IMySmallMissileLauncherReload)GetBlock("Assault Cannon free");
                if (Control == null) { Echo("ShipController with the name `aDriver` is missing."); }
                if (Vert == null) { Echo("Rotor with the name `Elevation` is missing."); }
                if (Horiz == null) { Echo("Rotor with the name `Azimuth` is missing."); }
                if (Gun == null) { Echo("Gun with the name `Elite Gatling Gun` is missing."); }
                if (!errors) 
                { 
                    setup = true;
                    tpx = Control.WorldMatrix.Forward;
                    tpy = Control.WorldMatrix.Right;
                    tpz = Control.WorldMatrix.Up;
                }
                timer = 0;
            }
        }

        private double Abs(double a) { return (a < 0) ? -a : a; }
        double Asin(double d) { return ToDeg(Math.Asin(d)); }
        double Acos(double d) { return ToDeg(Math.Acos(d)); }
        double Cos(double d) { return Math.Cos(ToRad(d)); }
        double ToRad(double angle) { return angle * (Math.PI / 180.0); }
        double ToDeg(double angle) { return angle * (180.0 / Math.PI); }
        /// <summary> Gets angle difference </summary>
        double GetADiff(Vector3D a, Vector3D b)
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
            }
            else
            {
                motor.SetValueFloat("LowerLimit", (float)ang);
                motor.SetValueFloat("UpperLimit", (float)highLimit);
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
