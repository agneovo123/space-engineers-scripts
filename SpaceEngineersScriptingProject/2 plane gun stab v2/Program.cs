﻿using Malware.MDKUtilities;
using Sandbox.Game.EntityComponents;
using Sandbox.ModAPI.Ingame;
using Sandbox.ModAPI.Interfaces;
using SpaceEngineers.Game.ModAPI.Ingame;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Security.Cryptography;
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
//using static VRage.Game.MyObjectBuilder_SessionComponentMission;

namespace IngameScript
{
    partial class Program : MyGridProgram
    {
        //TODO:
        // - argument handling
        // - arguments for reset & sensitivity

        // [+] 1 gun reset to 0-0
        // [ ] 2 gun desired vector
        // [ ] 3 gun turn to desired vector
        // [ ] 4 turn desired vector
        // https://stackoverflow.com/questions/14607640/rotating-a-vector-in-3d-space
        // rotate around the rotors' 'UP' vector

        // where is hinge 'UP' vector ? 









        // if you want to put this script into the game, start copying from this line ...
        #region mdk preserve
        ///////////////////////////////////////////////////////
        //            CUSTOMIZABLE VARIABLES:                //
        ///////////////////////////////////////////////////////

        // This script works with rotor, advanced rotor, and hinge
        // You can find the not-minified version on my github:
        // https://github.com/agneovo123/space-engineers-scripts/blob/main/SpaceEngineersScriptingProject/2-plane-gun-stabilization/Program.cs

        // sets your sensitivity
        const double sensitivity = 0.1;
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
        // 
        const float horizontalResetAngle = 0;
        // 
        const float verticalResetAngle = 0;

        // the name of your cockpit <used for input>
        const string CockpitName = "aTankDriver";
        // the name of your horizontal (left-right) rotor <used for turning the turret left-right>
        const string HorizName = "Rotor Horizontal";
        // the name of your vertical (up-down) rotor <used for turning the turret up-down>
        const string VertName = "Rotor Vertical";
        // the name of your gun <used for stabilisation, it can be any forward facing block>
        const string GunName = "Assault Cannon";
        ////////////////////////////////////////////////////////////////////////////////
        //                 DO NOT EDIT ANYTHING BELOW THIS LINE                       //
        ////////////////////////////////////////////////////////////////////////////////
        public Program() { Runtime.UpdateFrequency = UpdateFrequency.Update1; }
        #endregion

        // all of the bools are used to tell something to work or to not work over multiple frames
        bool setup = false, errors, rightOverTurn = false, leftOverTurn = false, resetCommandLine = true;
        // angles user input, current angle, vehicle body movement
        double userVert, userHoriz, angleVert, angleHoriz, aVertDifference, aHorizDifference;
        // mathematical constatns, 360 degrees in a circle, you multiply a radian value with 'radToDegMultiplier' to get it's value in degrees
        const double mod = 360, radToDegMultiplier = (180.0 / Math.PI);
        // essential blocks for the functioning of the script ... *hehe* cock *hehe* it means penis
        IMyShipController Cockpit;
        IMyCubeBlock Gun;
        IMyMotorStator Horiz, Vert;
        IMyTextPanel debugLCD;
        // timer for displaying/updating status
        int timer;
        // vehicle body(cockpit) vectors
        Vector3D x, y, z, // forward, right, up
            xp, yp, zp; // vectors of previous frame
        Vector3D vOriginal, desiredVector, currentVector; //
        // nullvector idk what to explain about it, it has no size, nor direction...
        Vector3D nullVector = new Vector3D(0, 0, 0);
        bool resetting = true, resetDone = false;
        // argument interpreter
        MyCommandLine _commandLine = new MyCommandLine();
        int idk = 0;
        public void Main(string args)
        {
            idk++;
            if (Me.CustomData != null)
            {
                _commandLine.TryParse(Me.CustomData.Replace(";", " "));
            }
            else if (resetCommandLine)
            {
                _commandLine.TryParse("");
            }
            if (setup && !resetDone)
            {
                debugLCD.WriteText("", false);
                debugLCD.WriteText("\n idk a: " + idk, true);
                resetGun();
                resetDone = IsResetDone();
                if (!resetDone)
                {
                    return;
                }
                desiredVector = Gun.WorldMatrix.Forward;
            }
            if (setup && !_commandLine.Switch("resetGun"))
            {
                debugLCD.WriteText("\n idk b: " + idk, true);
                debugLCD.WriteText("\n DONE", true);
                //return;
                // set previous vectors
                // on the first run (2nd tick) the x,y,z vectors haven't been set yet, so they are nullVectors
                //xp = (x == nullVector) ? Gun.WorldMatrix.Forward : x;
                //yp = (y == nullVector) ? Gun.WorldMatrix.Right : y;
                //zp = (z == nullVector) ? Gun.WorldMatrix.Up : z;
                // set current vectors
                // uses the cockpits directions to claculate deltas
                currentVector = Gun.WorldMatrix.Forward;
                //y = Gun.WorldMatrix.Right;
                //z = Gun.WorldMatrix.Up;

                // deltas
                // claculates how much the cockpit(vehicle body) has turned since last frame
                //double dx = GetADif(xp, x);
                //double dy = GetADif(yp, y);
                //double dz = GetADif(zp, z);

                // accumulative difference
                // basically, over time, these variables well, vary in size
                // uses Sin & Cos to enable stabilization even when not looking forward                                      //this bit here checks if the vehicle turned up or down
                //aVertDifference += ((Math.Cos(Horiz.Angle) * (dx + dz - dy) + Math.Sin(Horiz.Angle) * (dy + dz - dx)) / 2) * (GetADif(x, zp) < GetADif(xp, zp) ? 1 : -1);
                //                                      //this bit here checks if the vehicle turned right or left
                //aHorizDifference += (dx + dy - dz) / 2 * (GetADif(xp, y) > GetADif(xp, yp) ? 1 : -1);

                // sets user input
                userHoriz = Cockpit.RotationIndicator.Y * sensitivity;
                userVert = Cockpit.RotationIndicator.X * sensitivity;
                angleHoriz += userHoriz;
                angleVert -= userVert;

                debugLCD.WriteText("", false);
                debugLCD.WriteText(" Vert.Angle: " + ToDeg(Vert.Angle), true);
                debugLCD.WriteText("\n Horiz.Angle: " + ToDeg(Horiz.Angle), true);
                debugLCD.WriteText("\n Vert.Velocity: " + Vert.TargetVelocityRPM, true);
                debugLCD.WriteText("\n Horiz.Velocity: " + Horiz.TargetVelocityRPM, true);
                debugLCD.WriteText("\n", true);
                debugLCD.WriteText("\n desiredVector: " + desiredVector, true);
                debugLCD.WriteText("\n currentVector: " + currentVector, true);
                debugLCD.WriteText("\n AngleBetween: " + ToDeg(AngleBetween(desiredVector, currentVector)), true);
                debugLCD.WriteText("\n", true);
                //debugLCD.WriteText("\n userHoriz: " + userHoriz, true);
                //debugLCD.WriteText("\n userVert: " + userVert, true);
                debugLCD.WriteText("\n c.rot: " + Cockpit.RotationIndicator, true);
                // dot = 1 when close
                // cross = 0,0,0 when close
                //debugLCD.WriteText("\n dot: " + Vector3D.Dot(vOriginal, x), true);
                //debugLCD.WriteText("\n cross: " + Vector3D.Cross(vOriginal, x), true);

                if (Math.Abs(Cockpit.RotationIndicator.Y) > 0)
                {
                    desiredVector = rotateAbout(desiredVector, Horiz.WorldMatrix.Up, ToRad(-userHoriz));
                    debugLCD.WriteText("\n desiredVector H: " + desiredVector, true);
                }
                if (Math.Abs(Cockpit.RotationIndicator.X) > 0)
                {
                    desiredVector = rotateAbout(desiredVector, Vert.WorldMatrix.Up, ToRad(userVert));
                    debugLCD.WriteText("\n desiredVector V: " + desiredVector, true);
                }

                //AngleVert(Vert, (-aVertDifference + angleVert), limitDown, limitUp);
                //AngleHoriz(Horiz, (-aHorizDifference + angleHoriz), limitLeft, limitRight);
                Angle(Vert, limitDown, limitUp, true);
                Angle(Horiz, limitLeft, limitRight, false);




                timer++;
                if (timer > 80) { timer = 0; }
                if (timer < 20) { Echo("Agneovo's 2 plane gun stabilizer script \nrunning"); }
                else if (timer < 40) { Echo("Agneovo's 2 plane gun stabilizer script \nrunning."); }
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
                Gun = (IMyCubeBlock)GetBlock(GunName);
                // check if any of the above blocks are missing
                // and tell user (programmable block's right side text area in the control panel)
                if (Cockpit == null) { Echo("Cockpit with the name `" + CockpitName + "` is missing."); errors = true; }
                if (Vert == null) { Echo("Rotor with the name `" + HorizName + "` is missing."); errors = true; }
                if (Horiz == null) { Echo("Rotor with the name `" + VertName + "` is missing."); errors = true; }
                if (Gun == null) { Echo("hull block with the name `" + GunName + "` is missing."); errors = true; }
                if (limitUp < -360 || limitUp > 360) { Echo("limitUp must be between -360 and 360"); errors = true; }
                if (limitDown < -360 || limitDown > 360) { Echo("limitDown must be between -360 and 360"); errors = true; }
                if (limitLeft < -360 || limitLeft > 360) { Echo("limitLeft must be between -360 and 360"); errors = true; }
                if (limitRight < -360 || limitRight > 360) { Echo("limitRight must be between -360 and 360"); errors = true; }
                // if there are no errors
                if (!errors)
                {
                    // marks setup as done
                    setup = true;
                    // set angle variables to 0;
                    userVert = userHoriz = angleVert = angleHoriz = aVertDifference = aHorizDifference = 0;
                    // empty the Programmable block's CustomData
                    // used to pass arguments while running (in this case "reset")
                    Me.CustomData = "";
                }

                vOriginal = Gun.WorldMatrix.Forward;
                debugLCD = (IMyTextPanel)GetBlock("DEBUGLCD");

                timer = 0;

                debugLCD.WriteText("", false);
            }
        }

        public Vector3D rotateAbout(Vector3D a, Vector3D b, double theta)
        {

            //debugLCD.WriteText("\n IsUnit a: " + Vector3D.IsUnit(ref a), true);
            //debugLCD.WriteText("\n IsUnit b: " + Vector3D.IsUnit(ref b), true);
            //Vector3D parellel = xParV(a, b);
            //Vector3D perpendicular = a - parellel;
            //Vector3D w = Vector3D.Cross(b, perpendicular);
            //return Vector3D.Normalize(parellel
            //    + perpendicular * Math.Cos(theta)
            //    + Vector3D.Normalize(perpendicular) * Vector3D.Normalize(w) * Math.Sin(theta));


            // return (proj['par'] +
            //         proj['perp'] * np.cos(theta) +
            //         linalg.norm(proj['perp']) * makeUnit(w) * np.sin(theta))

            // wikipedia:
            // V_rot= V*cos(theta) + (K cross V)*sin(theta) + K*(K dot V)*(1-cos(theta))
            // translated:
            // return A*cos(theta) + (B cross A)*sin(theta) + B*(B dot A)*(1-cos(theta))

            return a * Math.Cos(theta) +
                Vector3D.Cross(b, a) * Math.Sin(theta) +
                b * Vector3D.Dot(b, a) * (1 - Math.Cos(theta));
        }

        public void resetGun()
        {
            debugLCD.WriteText("\n resetGun call: " + idk, true);
            float hAngle = ToDeg(Horiz.Angle);
            if (ToDeg(Horiz.Angle) != horizontalResetAngle)
            {
                if (hAngle > 180) { hAngle -= 360; }
                if (hAngle < -180) { hAngle += 360; }
                if (hAngle > horizontalResetAngle)
                {
                    Horiz.TargetVelocityRad = ToRad(Clamp(-10, 0, -hAngle)) * MathHelper.RadiansPerSecondToRPM;
                }
                else
                {
                    Horiz.TargetVelocityRad = ToRad(Clamp(0, 10, -hAngle)) * MathHelper.RadiansPerSecondToRPM;
                }
            }
            float vAngle = ToDeg(Vert.Angle);
            if (vAngle != verticalResetAngle)
            {
                if (vAngle > verticalResetAngle)
                {
                    Vert.TargetVelocityRad = Clamp(-10, 0, -vAngle) * MathHelper.RadiansPerSecondToRPM;
                }
                else
                {
                    Vert.TargetVelocityRad = ToRad(Clamp(0, 10, -vAngle)) * MathHelper.RadiansPerSecondToRPM;
                }
            }
            debugLCD.WriteText("\n", true);
            debugLCD.WriteText("\n H.a: " + ToDeg(Horiz.Angle), true);
            debugLCD.WriteText("\n hAngle: " + hAngle, true);
            debugLCD.WriteText("\n H.l: " + Horiz.LowerLimitDeg, true);
            debugLCD.WriteText("\n H.u: " + Horiz.UpperLimitDeg, true);
            debugLCD.WriteText("\n H.v: " + ToDeg(Horiz.TargetVelocityRad), true);
            debugLCD.WriteText("\n", true);
            debugLCD.WriteText("\n V.a: " + ToDeg(Vert.Angle), true);
            debugLCD.WriteText("\n V.l: " + Vert.LowerLimitDeg, true);
            debugLCD.WriteText("\n V.u: " + Vert.UpperLimitDeg, true);
            debugLCD.WriteText("\n V.v: " + ToDeg(Vert.TargetVelocityRad), true);
            debugLCD.WriteText("\n", true);
        }
        public bool IsResetDone()
        {
            float limit = (float)0.01;
            debugLCD.WriteText("\n IsResetDone call: " + idk, true);
            debugLCD.WriteText("\n H.a: " + ToDeg(Horiz.Angle), true);
            debugLCD.WriteText("\n if: " + (WithinN(ToDeg(Horiz.Angle), horizontalResetAngle, limit)), true);
            debugLCD.WriteText("\n", true);
            debugLCD.WriteText("\n V.a: " + ToDeg(Vert.Angle), true);
            debugLCD.WriteText("\n if: " + (WithinN(ToDeg(Vert.Angle), verticalResetAngle, limit)), true);
            if (WithinN(ToDeg(Horiz.Angle), horizontalResetAngle, limit))
            {
                Horiz.TargetVelocityRad = 0;
                Horiz.LowerLimitDeg = float.MinValue;
                Horiz.UpperLimitDeg = float.MaxValue;
            }
            if (WithinN(ToDeg(Vert.Angle), verticalResetAngle, limit))
            {
                Vert.TargetVelocityRad = 0;
                Vert.LowerLimitDeg = -90;
                Vert.UpperLimitDeg = 90;
            }
            return WithinN(ToDeg(Horiz.Angle), horizontalResetAngle, limit) && WithinN(ToDeg(Vert.Angle), verticalResetAngle, limit);
        }
        public float Clamp(float min, float max, float num)
        {
            if (num > max) { return max; }
            if (num < min) { return min; }
            return num;
        }
        bool WithinN(float a, float b, float limit)
        {
            if (Math.Abs(a - b) < limit) { return true; }
            return false;
        }
        float ToDeg(float rad) { return MathHelper.ToDegrees(rad); }
        double ToDeg(double rad) { return MathHelper.ToDegrees(rad); }
        float ToRad(float deg) { return MathHelper.ToRadians(deg); }
        double ToRad(double deg) { return MathHelper.ToRadians(deg); }
        public static Vector3D Rejection(Vector3D a, Vector3D b)
        {
            if (Vector3D.IsZero(a) || Vector3D.IsZero(b))
                return Vector3D.Zero;

            return a - a.Dot(b) / b.LengthSquared() * b;
        }
        double VanglePrev0 = 0;
        double VanglePrev1 = 0;
        double HanglePrev0 = 0;
        double HanglePrev1 = 0;
        void Angle(IMyMotorStator rotor, double minAngle, double maxAngle, bool v)
        {
            //double dot = Vector3D.Dot(desiredVector, currentVector);

            //Angle(Vert, limitDown, limitUp);
            //Angle(Horiz, limitLeft, limitRight);

            Vector3D desiredDirectionFlat = Rejection(desiredVector, rotor.WorldMatrix.Up);
            Vector3D currentDirectionFlat = Rejection(currentVector, rotor.WorldMatrix.Up);
            double angle = AngleBetween(desiredDirectionFlat, currentDirectionFlat);
            //double angle = ToDeg(AngleBetween(desiredDirectionFlat, currentDirectionFlat));
            //if (Math.Abs(angle) < 0.003) { angle = 0; }
            //debugLCD.WriteText("\n", true);
            //debugLCD.WriteText("\n angle: " + angle, true);
            Vector3D axis = Vector3D.Cross(desiredVector, currentVector);
            angle *= Math.Sign(Vector3D.Dot(axis, rotor.WorldMatrix.Up));
            angle = GetAllowedRotationAngle(angle, rotor);
            if (v)
            {
                double tmp = VanglePrev0;
                debugLCD.WriteText("\n V angle: " + angle, true);
                debugLCD.WriteText("\n VanglePrev0: " + VanglePrev0, true);
                debugLCD.WriteText("\n VanglePrev1: " + VanglePrev1, true);
                double speed0 = VanglePrev0 - angle;
                double speed1 = VanglePrev1 - VanglePrev0;
                debugLCD.WriteText("\n speed0: " + speed0, true);
                debugLCD.WriteText("\n speed1: " + speed1, true);
                debugLCD.WriteText("\n tmp: " + tmp, true);
                VanglePrev1 = VanglePrev0;
                VanglePrev0 = ToDeg(angle);
                //tmp = ((Math.Abs(tmp) < 1) ? Math.Pow(tmp, 2) : Math.Sqrt(Math.Abs(tmp))) * Math.Sign(tmp);
                angle += tmp;
            }
            else
            {
                double tmp = HanglePrev0;
                debugLCD.WriteText("\n H angle: " + angle, true);
                debugLCD.WriteText("\n HanglePrev0: " + HanglePrev0, true);
                debugLCD.WriteText("\n HanglePrev1: " + HanglePrev1, true);
                double speed0 = HanglePrev0 - angle;
                double speed1 = HanglePrev1 - HanglePrev0;
                debugLCD.WriteText("\n speed0: " + speed0, true);
                debugLCD.WriteText("\n speed1: " + speed1, true);
                debugLCD.WriteText("\n tmp: " + tmp, true);
                HanglePrev1 = HanglePrev0;
                HanglePrev0 = ToDeg(angle);
                //tmp = ((Math.Abs(tmp) < 1) ? Math.Pow(tmp, 2) : Math.Sqrt(Math.Abs(tmp))) * Math.Sign(tmp);
                angle += tmp;
            }


            if (Math.Abs(angle) < 0.005) { angle = 0; }
            //rotor.TargetVelocityRad = Clamp((float)-horizontalSpeedLimit, (float)horizontalSpeedLimit, (float)angle * MathHelper.RadiansPerSecondToRPM);
            //rotor.TargetVelocityRad = (float)angle * MathHelper.RadiansPerSecondToRPM;

            //rotor.TargetVelocityRad = (float)angle / 10;
            rotor.TargetVelocityRad = ToRad((float)angle * 60);
            //rotor.TargetVelocityRad = (float)angle;
            //debugLCD.WriteText("\n angle 4: " + angle, true);
            //debugLCD.WriteText("\n TargetVelocityDeg: " + ToDeg(rotor.TargetVelocityRad), true);
        }
        /// <summary>
        /// "desiredDelta" is in RADIANS
        /// </summary>
        double GetAllowedRotationAngle(double desiredDelta, IMyMotorStator rotor)
        {
            double desiredAngle = rotor.Angle + desiredDelta;
            if ((desiredAngle < rotor.LowerLimitRad && desiredAngle + MathHelper.TwoPi < rotor.UpperLimitRad)
                || (desiredAngle > rotor.UpperLimitRad && desiredAngle - MathHelper.TwoPi > rotor.LowerLimitRad))
            {
                return -Math.Sign(desiredDelta) * (MathHelper.TwoPi - Math.Abs(desiredDelta));
            }
            return desiredDelta;
        }
        /// <summary>
        /// the angle between 2 vectors (in radians)
        /// </summary>
        /// <param name="a"></param>
        /// <param name="b"></param>
        /// <returns></returns>
        double AngleBetween(Vector3D a, Vector3D b)
        {

            //debugLCD.WriteText("\na " + a, true);
            //debugLCD.WriteText("\nb " + b, true);
            if (Vector3D.IsZero(a) || Vector3D.IsZero(b) || a.Equals(b))
            {
                //debugLCD.WriteText("\nreturn 0", true);
                return 0;
            }
            else
            {
                double math = Math.Acos(MathHelper.Clamp(a.Dot(b) / Math.Sqrt(a.LengthSquared() * b.LengthSquared()), -1, 1));
                if (Math.Abs(math) < 0.001)
                {
                    //debugLCD.WriteText("\nsmall math: " + math, true);
                    return 0;
                }
                //debugLCD.WriteText("\nreturn math: " + math, true);
                return math;
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
            float velocity = (float)ClampVSpeed((ang - motorCurrentAngle) * 6f);
            //if (velocity < 1 && velocity > -1) { velocity = (float)0; }
            motor.SetValueFloat("Velocity", Min1(velocity));
        }
        public void AngleHoriz(IMyMotorStator motor, double ang, double lowLimit, double highLimit)
        {
            double motorCurrentAngle = motor.Angle * radToDegMultiplier;
            if (ang > 360 || rightOverTurn)
            {
                motorCurrentAngle %= mod;
                motor.SetValueFloat("Velocity", Min1((float)ClampHSpeed((mod - motorCurrentAngle + (ang % mod)) * 6f)));
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
                motor.SetValueFloat("Velocity", Min1((float)ClampHSpeed((motorCurrentAngle - mod - (ang % mod)) * 6f)));
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
            float velocity = (float)ClampHSpeed((ang - motorCurrentAngle) * 6f);
            //if (velocity < 1 && velocity > -1) { velocity = (float)0; }
            motor.SetValueFloat("Velocity", Min1(velocity));
        }
        public float Min1(float v)
        {
            if (Math.Abs(v) < 0.1)
            {
                return 0;
            }
            return v;
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