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
    partial class Program2 : MyGridProgram
    {
        public Program()
        {
            Runtime.UpdateFrequency = UpdateFrequency.Update1;
        }
        double sensitivity = 0.05; // lower value = lower sensitivity
        bool setup, errors;
        double angleVert, angleHoriz, aVertDifference, aHorizDifference;
        IMyShipController Control;
        IMyMotorStator Horiz, Vert;
        IMyTextPanel debugLCD;
        int timer;
        Vector3D forward, forwardPrev;
        public void Main(string args)
        {
            if (setup)
            {
                if (forward == null)
                    forwardPrev = Control.WorldMatrix.Forward;
                else
                    forwardPrev = forward;
                forward = Control.WorldMatrix.Forward;
                aVertDifference = GetAngleDifference(Control.GetNaturalGravity(), forward) - 90;
                aHorizDifference = GetAngleDifference2(Control.GetNaturalGravity(), forwardPrev, forward);
                double rHoriz = Control.RotationIndicator.Y * sensitivity;
                double rVert = Control.RotationIndicator.X * sensitivity;
                angleHoriz += rHoriz;
                angleVert -= rVert;
                debugLCD.WriteText("", false);
                debugLCD.WriteText("\n grav X: " + Control.GetNaturalGravity().X, true);
                debugLCD.WriteText("\n grav Y: " + Control.GetNaturalGravity().Y, true);
                debugLCD.WriteText("\n grav Z: " + Control.GetNaturalGravity().Z, true);
                debugLCD.WriteText("\n", true);
                debugLCD.WriteText("\naHorizDifference: " + aHorizDifference, true);
                debugLCD.WriteText("\npassed to angle2: " + (-aHorizDifference + angleHoriz), true);
                debugLCD.WriteText("\ntarget Y angle: " + Control.RotationIndicator.Y, true);
                debugLCD.WriteText("\nrHoriz: " + rHoriz, true);
                debugLCD.WriteText("\nangleHoriz: " + angleHoriz, true);
                Angle(Vert, (-aVertDifference + angleVert), -21, 60);
                Angle2(Horiz, angleHoriz, -360, 360);

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
                if (Control == null) { Echo("ShipController with the name `Driver` is missing."); }
                if (Vert == null) { Echo("Rotor with the name `Elevation` is missing."); }
                if (Horiz == null) { Echo("Rotor with the name `Azimuth` is missing."); }
                if (debugLCD == null) { Echo("LCD with the name `LCD` is missing."); }
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
        double GetAngleDifference2(Vector3D g, Vector3D fp, Vector3D f)
        {
            double fak1 = (g.Y * fp.Y) + (g.Z * fp.Z);
            double aMagnitude = Math.Sqrt(g.Y * g.Y + g.Z * g.Z);
            double bMagnitude = Math.Sqrt(fp.Y * fp.Y + fp.Z * fp.Z);
            return Acos(fak1 / (aMagnitude * bMagnitude));
        }

        public void Angle(IMyMotorStator motor, double ang, double lowLimit, double highLimit)
        {
            double motorCurrentAngle = ToDeg(motor.Angle);
            if (ang > highLimit) { ang = angleVert = highLimit; } 
            else if (ang < lowLimit) { ang = angleVert = lowLimit; }
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
