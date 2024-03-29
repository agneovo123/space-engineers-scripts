﻿using Sandbox.Game.EntityComponents;
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
        //TODO:
        // - argument handling
        // - arguments for reset & sensitivity
        // if you want to put this script into the game, start copying from this line ...
        #region mdk preserve
        ///////////////////////////////////////////////////////
        //  Based on Xionphs stabilized mouse-turret script  //
        ///////////////////////////////////////////////////////
        //            CUSTOMIZABLE VARIABLES:                //
        ///////////////////////////////////////////////////////

        // This script works with rotor, advanced rotor, and hinge
        // You can find the not-minified version on my github:
        // https://github.com/agneovo123/space-engineers-scripts/blob/main/SpaceEngineersScriptingProject/2-plane-gun-stabilization/Program.cs

        // sets your sensitivity
        const double sensitivity = 0.05;
        // how fast the horizontal rotor can go value: 0 to 60
        const double horizontalSpeedLimit = 30;
        // how fast the vertical rotor can go value: 0 to 60
        const double verticalSpeedLimit = 30;
        // the next 4 variables range from -360 to 360
        // how much the turret can turn left (set to -360 to make it unlimited)
        const double limitLeft = -360;
        // how much the turret can turn right (set to 360 to make it unlimited)
        const double limitRight = 360;
        // how much the turret can turn up
        const double limitUp = 40;
        // how much the turret can turn down
        const double limitDown = -12;

        // the name of your cockpit
        const string CockpitName = "aTankDriver";
        // the name of your horizontal (left-right) rotor
        const string HorizName = "Rotor Horizontal";
        // the name of your vertical (up-down) rotor
        const string VertName = "Rotor Vertical";
        // If you want to have your cockpit rotate with the turret, you have to specify a block in the hull, so the
        // stabilisation can work
        // If this is empty (""), then the program will assume, the cockpit is in the hull
        const string hullBlockName = "";
        ////////////////////////////////////////////////////////////////////////////////
        //                 DO NOT EDIT ANYTHING BELOW THIS LINE                       //
        ////////////////////////////////////////////////////////////////////////////////
        public Program() { Runtime.UpdateFrequency = UpdateFrequency.Update1; }
        #endregion

        // all of the bools are used to tell something to work or to not work over multiple frames
        bool setup, errors, rotorsOff = true, rightOverTurn = false, leftOverTurn = false, resetCommandLine = true;
        // angles user input, current angle, vehicle body movement
        double userVert, userHoriz, angleVert, angleHoriz, aVertDifference, aHorizDifference;
        // mathematical constatns, 360 degrees in a circle, you multiply a radian value with 'radToDegMultiplier' to get it's value in degrees
        const double mod = 360, radToDegMultiplier = (180.0 / Math.PI);
        // essential blocks for the functioning of the script ... *hehe* cock *hehe* it means penis
        IMyShipController Cockpit;
        IMyCubeBlock hullBlock;
        IMyMotorStator Horiz, Vert;
        // timer for displaying/updating status
        int timer;
        // vehicle body(cockpit) vectors
        Vector3D x, y, z, // forward, right, up
            xp, yp, zp; // vectors of previous frame
        // nullvector idk what to explain about it, it has no size, nor direction...
        Vector3D nullVector = new Vector3D(0, 0, 0);
        // argument interpreter
        MyCommandLine _commandLine = new MyCommandLine();
        public void Main(string args)
        {
            // checks if setup is done (true); runs from 2nd tick
            // If the programmable block's CustomData = "reset", then don't run; effectively resetting the gun to it's forward position
            if (Me.CustomData != null)
            {
                _commandLine.TryParse(Me.CustomData.Replace(";", " "));
            }
            else if (resetCommandLine)
            {
                _commandLine.TryParse("");
            }
            if (setup && !_commandLine.Switch("resetGun"))
            {
                if (hullBlockName == "")
                {
                    // set previous vectors
                    // on the first run (2nd tick) the x,y,z vectors haven't been set yet, so they are nullVectors
                    xp = (x == nullVector) ? Cockpit.WorldMatrix.Forward : x;
                    yp = (y == nullVector) ? Cockpit.WorldMatrix.Right : y;
                    zp = (z == nullVector) ? Cockpit.WorldMatrix.Up : z;
                    // set current vectors
                    // uses the cockpits directions to claculate deltas
                    x = Cockpit.WorldMatrix.Forward;
                    y = Cockpit.WorldMatrix.Right;
                    z = Cockpit.WorldMatrix.Up;
                }
                else
                {
                    // use the hullBlock's orientation
                    xp = (x == nullVector) ? hullBlock.WorldMatrix.Forward : x;
                    yp = (y == nullVector) ? hullBlock.WorldMatrix.Right : y;
                    zp = (z == nullVector) ? hullBlock.WorldMatrix.Up : z;
                    x = hullBlock.WorldMatrix.Forward;
                    y = hullBlock.WorldMatrix.Right;
                    z = hullBlock.WorldMatrix.Up;
                }

                // deltas
                // claculates how much the cockpit(vehicle body) has turned since last frame
                double dx = GetADif(xp, x);
                double dy = GetADif(yp, y);
                double dz = GetADif(zp, z);

                // accumulative difference
                // basically, over time, these variables well, vary in size
                // uses Sin & Cos to enable stabilization even when not looking forward                                      //this bit here checks if the vehicle turned up or down
                aVertDifference += ((Math.Cos(Horiz.Angle) * (dx + dz - dy) + Math.Sin(Horiz.Angle) * (dy + dz - dx)) / 2) * (GetADif(x, zp) < GetADif(xp, zp) ? 1 : -1);
                //                                      //this bit here checks if the vehicle turned right or left
                aHorizDifference += (dx + dy - dz) / 2 * (GetADif(xp, y) > GetADif(xp, yp) ? 1 : -1);

                // sets user input
                userHoriz = Cockpit.RotationIndicator.Y * sensitivity;
                userVert = Cockpit.RotationIndicator.X * sensitivity;
                angleHoriz += userHoriz;
                angleVert -= userVert;

                AngleVert(Vert, (-aVertDifference + angleVert), limitDown, limitUp);
                AngleHoriz(Horiz, (-aHorizDifference + angleHoriz), limitLeft, limitRight);

                timer++;
                if (timer > 80) { timer = 0; }
                if (timer < 20) { Echo("Agneovo's 2 plane gun stabilizer script \nrunning"); }
                else if (timer < 40)
                {
                    Echo("Agneovo's 2 plane gun stabilizer script \nrunning.");
                    // on the first run of this (21st tick from restart/recompile) turn the rotors back on.
                    // This is to prevent a bug(?), where the rotors would freak out on each paste/Recompile
                    // and would only stop, when manually turned off, then back on again.
                    if (rotorsOff) { rotorsOff = false; Horiz.ApplyAction("OnOff_On"); Vert.ApplyAction("OnOff_On"); }
                }
                else if (timer < 60) { Echo("Agneovo's 2 plane gun stabilizer script \nrunning.."); }
                else if (timer < 80) { Echo("Agneovo's 2 plane gun stabilizer script \nrunning..."); }
            }
            //runs on first tick
            else
            {
                // set errors to false (happy state)
                errors = false;
                // try to get necessary blocks
                Cockpit = (IMyShipController)GetBlock(CockpitName);
                Horiz = (IMyMotorStator)GetBlock(HorizName);
                Vert = (IMyMotorStator)GetBlock(VertName);
                // only get hullBlock if it has a name set
                if (hullBlockName != "") { hullBlock = (IMyCubeBlock)GetBlock(hullBlockName); }
                // check if any of the above blocks are missing
                // and tell user (programmable block's right side text area in the control panel)
                if (Cockpit == null) { Echo("Cockpit with the name `" + CockpitName + "` is missing."); }
                if (Vert == null) { Echo("Rotor with the name `" + HorizName + "` is missing."); }
                if (Horiz == null) { Echo("Rotor with the name `" + VertName + "` is missing."); }
                if (hullBlockName != "" && hullBlock == null) { Echo("hull block with the name `" + hullBlockName + "` is missing."); }
                if (limitUp < -360 || limitUp > 360) { Echo("limitUp must be between -360 and 360"); }
                if (limitDown < -360 || limitDown > 360) { Echo("limitDown must be between -360 and 360"); }
                if (limitLeft < -360 || limitLeft > 360) { Echo("limitLeft must be between -360 and 360"); }
                if (limitRight < -360 || limitRight > 360) { Echo("limitRight must be between -360 and 360"); }
                // if there are no errors
                if (!errors)
                {
                    // marks setup as done(true)
                    // after this the program runs (How do I express what this does, it's plain obvious to me, but what if it isn't for someone reading the code?)
                    setup = true;
                    // turn off rotors to prevent the startup bug
                    Horiz.ApplyAction("OnOff_Off");
                    Vert.ApplyAction("OnOff_Off");
                    // rotorsOff is used to turn the rotors back on after 21 ticks
                    rotorsOff = true;
                    // set angle variables to 0;
                    userVert = userHoriz = angleVert = angleHoriz = aVertDifference = aHorizDifference = 0;
                    // empty the Programmable block's CustomData
                    // used to pass arguments while running (in this case "reset")
                    Me.CustomData = "";
                }
                timer = 0;
            }
        }
        double ClampHSpeed(double number)
        {
            //if (userHoriz == 0)
            //{
            //    if (number > 60) { return 60; }
            //    if (number < -60) { return -60; }
            //}
            //else
            //{
            if (number > horizontalSpeedLimit) { return horizontalSpeedLimit; }
            if (number < -horizontalSpeedLimit) { return -horizontalSpeedLimit; }
            //}
            return number;
        }
        double ClampVSpeed(double number)
        {
            //if (userVert == 0)
            //{
            //    if (number > 60) { return 60; }
            //    if (number < -60) { return -60; }
            //}
            //else
            //{
            if (number > verticalSpeedLimit) { return verticalSpeedLimit; }
            if (number < -verticalSpeedLimit) { return -verticalSpeedLimit; }
            //}
            return number;
        }
        /// <summary> Get angle difference </summary>
        double GetADif(Vector3D a, Vector3D b)
        {
            if (a == b) { return 0; }
            double fak1 = (a.X * b.X) + (a.Y * b.Y) + (a.Z * b.Z);
            double aMagnitude = Math.Sqrt(a.X * a.X + a.Y * a.Y + a.Z * a.Z);
            double bMagnitude = Math.Sqrt(b.X * b.X + b.Y * b.Y + b.Z * b.Z);
            return Math.Acos(fak1 / (aMagnitude * bMagnitude)) * radToDegMultiplier;
        }
        public void AngleVert(IMyMotorStator motor, double ang, double lowLimit, double highLimit)
        {
            double motorCurrentAngle = motor.Angle * radToDegMultiplier;
            if (ang > highLimit)
            {
                //angleVert += userVert; ///Bugfix fail
                motor.SetValueFloat("LowerLimit", (float)highLimit);
                motor.SetValueFloat("UpperLimit", (float)highLimit);
            }
            else if (ang < lowLimit)
            {
                //angleVert += userVert; ///Bugfix fail
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
            float velocity = (float)ClampVSpeed((ang - motorCurrentAngle) * 6f);
            //if (velocity < 1 && velocity > -1) { velocity = (float)0; }
            motor.SetValueFloat("Velocity", velocity);
        }
        public void AngleHoriz(IMyMotorStator motor, double ang, double lowLimit, double highLimit)
        {
            double motorCurrentAngle = motor.Angle * radToDegMultiplier;
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
            float velocity = (float)ClampHSpeed((ang - motorCurrentAngle) * 6f);
            //if (velocity < 1 && velocity > -1) { velocity = (float)0; }
            motor.SetValueFloat("Velocity", velocity);
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
