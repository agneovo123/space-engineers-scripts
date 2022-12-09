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
    partial class Program5_gun_copied_from_game : MyGridProgram
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
        IMySmallMissileLauncherReload Gun;
        int timer;
        Vector3D x, y, z, w, //forward, right up, left
            xg, yg, zg, wg, //gun vectors
            xgp, ygp, zgp, wgp, //gun vectors
            xp, yp, zp, //previous vectors
            ax, ay, az, //absolute (starting) vectors
            nulla = new Vector3D(0.0, 0.0, 0.0); //nullvector
        double dxysign = 0, dxysingp = 0;
        bool trigger = false;
        public void Main(string args)
        {
            if (setup)
            {
                if (ax == nulla || ay == nulla || az == nulla)
                {
                    ax = Control.WorldMatrix.Forward;
                    ay = Control.WorldMatrix.Right;
                    az = Control.WorldMatrix.Up;
                }
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

                x = Control.WorldMatrix.Forward;
                y = Control.WorldMatrix.Right;
                z = Control.WorldMatrix.Up;
                w = Control.WorldMatrix.Left;

                xgp = xg;
                ygp = yg;
                zgp = zg;
                wgp = wg;
                xg = Gun.WorldMatrix.Forward;
                yg = Gun.WorldMatrix.Right;
                zg = Gun.WorldMatrix.Up;
                wg = Gun.WorldMatrix.Left;

                double dxg = GetAngleDifference(xg, xgp);
                double dyg = GetAngleDifference(yg, ygp);
                double dzg = GetAngleDifference(zg, zgp);
                double dwg = GetAngleDifference(wg, wgp);

                if (GetAngleDifference(xg, ygp) > GetAngleDifference(xgp, ygp))
                {
                    dxg = -dxg;
                    dyg = -dyg;
                }



                double dx = GetAngleDifference(ax, x);
                double dy = GetAngleDifference(ay, y);
                double dz = GetAngleDifference(az, z);

                double dxs = GetAngleDifference(x, xp);
                double dys = GetAngleDifference(y, yp);
                double dzs = GetAngleDifference(z, zp);
                if (GetAngleDifference(x, yp) > GetAngleDifference(xp, yp))
                {
                    dxs = -dxs;
                    dys = -dys;
                }

                //right +
                //left -
                dxysingp = dxysign;
                if (GetAngleDifference(ax, y) > GetAngleDifference(ax, w))
                {
                    dxysign = 1;
                }
                else
                {
                    dxysign = -1;
                }
                //get all 3 directions' (forward (x), right(y), up(z)) vector
                //get differences (compared to prevoius frames (GetAngleDifference()))
                // horizontal movement = dx + dy - dz;
                // vertical movement = cos(motor.angle) * (dx + dz - dy) + sin(motor.angle) * (dy + dz - dx)

                //aVertDifference = (Math.Cos(Horiz.Angle) * (dx + dz - dy) + Math.Sin(Horiz.Angle) * (dy + dz - dx)) / 2;

                //FIX THIS:
                //when looking back it switches from 180 to -180
                //aHorizDifference = (dx + dy - dz) / 2 * dxysign;
                //aHorizDifference = (aHorizDifference + (dxs + dys - dzs) / 2) % 360;


                aVertDifference = (Math.Cos(Horiz.Angle) * (dx + dz - dy) + Math.Sin(Horiz.Angle) * (dy + dz - dx)) / 2;
                aHorizDifference = (dx + dy - dz) / 2 * dxysign;

                double rHoriz = Control.RotationIndicator.Y * sensitivity;
                double rVert = Control.RotationIndicator.X * sensitivity;
                angleHoriz += rHoriz;
                angleVert -= rVert;
                Angle(Vert, (aVertDifference + angleVert), -10, 40);
                Angle2(Horiz, (-aHorizDifference + angleHoriz), -360, 360);

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
                Gun = (IMySmallMissileLauncherReload)GetBlock("Assault Cannon free");
                if (Control == null) { Echo("ShipController with the name `aDriver` is missing."); }
                if (Vert == null) { Echo("Rotor with the name `Elevation` is missing."); }
                if (Horiz == null) { Echo("Rotor with the name `Azimuth` is missing."); }
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
            double motorCurrentAngle = ToDeg(motor.Angle);
            double mod = 360;
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