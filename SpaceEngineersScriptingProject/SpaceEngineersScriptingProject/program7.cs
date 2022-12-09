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
        //////////////////////////////////////////////////
        // Code based on <name here>'s gravity based gun stabilizer
        //////////////////////////////////////////////////
        public Program()
        {
            Runtime.UpdateFrequency = UpdateFrequency.Update1;
        }
        double sensitivity = 0.05; // lower value = lower sensitivity
        bool setup, errors, firstSetup = true;
        double angleVert, angleHoriz;
        double aVertDifference = 0;
        double aHorizDifference = 0;
        IMyShipController Control;
        IMyMotorStator Horiz, Vert;
        IMyTextPanel debugLCD;
        IMySmallGatlingGun Gun;
        int timer;
        Vector3D x, y, z, w, //forward, right up, left
            xp, yp, zp, //previous vectors
            ax, ay, az; //absolute (starting) vectors
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
                    if (firstSetup)
                    {
                        firstSetup = false;
                        Horiz.ApplyAction("OnOff_On");
                        Vert.ApplyAction("OnOff_On");
                    }
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
                Gun = (IMySmallGatlingGun)GetBlock("Elite Gatling Gun");
                debugLCD = (IMyTextPanel)GetBlock("LCD");
                if (Control == null) { Echo("ShipController with the name `aDriver` is missing."); }
                if (Vert == null) { Echo("Rotor with the name `Elevation` is missing."); }
                if (Horiz == null) { Echo("Rotor with the name `Azimuth` is missing."); }
                if (debugLCD == null) { Echo("LCD with the name `LCD` is missing."); }
                if (Gun == null) { Echo("Gun with the name `Elite Gatling Gun` is missing."); }
                if (!errors) { 
                    setup = true;
                    // alap/start
                    ax = Control.WorldMatrix.Forward;
                    ay = Control.WorldMatrix.Right;
                    az = Control.WorldMatrix.Up;
                    angleVert = 0;
                    angleHoriz = 0;
                    aVertDifference = 0;
                    aHorizDifference = 0;
                    Horiz.ApplyAction("OnOff_Off");
                    Vert.ApplyAction("OnOff_Off");
                }
                timer = 0;
            }
        }
        double Asin(double d) { return ToDeg(Math.Asin(d)); }
        double Acos(double d) { return ToDeg(Math.Acos(d)); }
        double Cos(double d) { return Math.Cos(ToRad(d)); }
        double ToRad(double angle) { return angle * (Math.PI / 180.0); }
        double ToDeg(double angle) { return angle * (180.0 / Math.PI); }
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
        bool righttriggered = false;
        public void Angle2(IMyMotorStator motor, double ang, double lowLimit, double highLimit)
        {
            double motorCurrentAngle = ToDeg(motor.Angle);
            double mod = 360;
            debugLCD.WriteText("\n", true);
            debugLCD.WriteText("\n ang: " + ang, true);
            if (ang > 360 || righttriggered)
            {
                motor.SetValueFloat("Velocity", Math.Min((float)(mod - motorCurrentAngle + (ang % mod)) * 6f, 60));
                //motor.SetValueFloat("LowerLimit", (float)(ang % mod));
                //motor.SetValueFloat("UpperLimit", (float)(ang % mod));
                aHorizDifference += mod;
                righttriggered = true;
                if (ang < 360)
                {
                    righttriggered = false;
                }
                return;
            }
            if (ang < -360)
            {
                motor.SetValueFloat("Velocity", Math.Min((float)(mod + motorCurrentAngle - (Math.Abs(ang) % mod)) * 6f, 60));
                //motor.SetValueFloat("LowerLimit", (float)(ang % mod));
                //motor.SetValueFloat("UpperLimit", (float)(ang % mod));
                aHorizDifference -= mod;
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
            motor.SetValueFloat("Velocity", Math.Min((float)(ang - motorCurrentAngle) * 6f, 60));
        }
        public IMyTerminalBlock GetBlock(string name)
        {
            IMyTerminalBlock r = GridTerminalSystem.GetBlockWithName(name);
            if (r == null) { errors = true; }
            return r;
        }
    }
}
