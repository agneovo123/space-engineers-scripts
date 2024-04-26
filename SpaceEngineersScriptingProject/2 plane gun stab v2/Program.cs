using Malware.MDKUtilities;
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
        // [+] 2 gun desired vector
        // [ ] 3 gun turn to desired vector
        // [+] 4 turn desired vector

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
        double userVert, userHoriz;
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
                ResetGun();
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
                currentVector = Gun.WorldMatrix.Forward;
                // sets user input
                userHoriz = Cockpit.RotationIndicator.Y * sensitivity;
                userVert = Cockpit.RotationIndicator.X * sensitivity;

                debugLCD.WriteText("", false);
                debugLCD.WriteText(" Vert.Angle: " + ToDeg(Vert.Angle), true);
                debugLCD.WriteText("\ntarget angle V: " + ToDeg(AngleBetween(Vert.WorldMatrix.Left, desiredVector)), true);

                //debugLCD.WriteText("\n Horiz.Angle: " + ToDeg(Horiz.Angle), true);
                debugLCD.WriteText("\nVert.Velocity: " + Vert.TargetVelocityRPM, true);
                //debugLCD.WriteText("\n Horiz.Velocity: " + Horiz.TargetVelocityRPM, true);
                debugLCD.WriteText("\n", true);
                //debugLCD.WriteText("\n desiredVector: " + desiredVector, true);
                //debugLCD.WriteText("\n currentVector: " + currentVector, true);
                debugLCD.WriteText("\nAngleBetween: " + ToDeg(AngleBetween(desiredVector, currentVector)), true);
                debugLCD.WriteText("\n", true);
                //debugLCD.WriteText("\n userHoriz: " + userHoriz, true);
                //debugLCD.WriteText("\n userVert: " + userVert, true);
                //debugLCD.WriteText("\n c.rot: " + Cockpit.RotationIndicator, true);
                // dot = 1 when close
                // cross = 0,0,0 when close
                //debugLCD.WriteText("\n dot: " + Vector3D.Dot(vOriginal, x), true);
                //debugLCD.WriteText("\n cross: " + Vector3D.Cross(vOriginal, x), true);
                //debugLCD.WriteText("\n", true);
                if (Math.Abs(Cockpit.RotationIndicator.Y) > 0)
                {
                    desiredVector = RotateAbout(desiredVector, Horiz.WorldMatrix.Up, ToRad(-userHoriz));
                    //debugLCD.WriteText("\n desiredVector H: " + desiredVector, true);
                }
                if (Math.Abs(Cockpit.RotationIndicator.X) > 0)
                {
                    desiredVector = RotateAbout(desiredVector, Vert.WorldMatrix.Up, ToRad(userVert));
                    //debugLCD.WriteText("\n desiredVector V: " + desiredVector, true);
                }
                //Angle(Vert, (-aVertDifference + angleVert), limitDown, limitUp);
                //AngleVert(Vert, (-aVertDifference + angleVert), limitDown, limitUp);
                //AngleHoriz(Horiz, (-aHorizDifference + angleHoriz), limitLeft, limitRight);
                Angle(Vert, limitDown, limitUp, true);
                Angle(Horiz, limitLeft, limitRight, false);




                //Angle(Horiz, (-aHorizDifference + angleHoriz), limitLeft, limitRight);

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
                    userVert = userHoriz = 0;
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

        public Vector3D RotateAbout(Vector3D v, Vector3D k, double theta)
        {
            // formula wikipedia: https://en.wikipedia.org/wiki/Rodrigues'_rotation_formula#Statement
            return v * Math.Cos(theta) +
                Vector3D.Cross(k, v) * Math.Sin(theta) +
                k * Vector3D.Dot(k, v) * (1 - Math.Cos(theta));
        }

        public void ResetGun()
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
        double VanglePrev2 = 0;
        double VanglePrev3 = 0;
        double HanglePrev0 = 0;
        double HanglePrev1 = 0;
        double HanglePrev2 = 0;
        double HanglePrev3 = 0;
        void Angle(IMyMotorStator rotor, double minAngle, double maxAngle, bool v)
        {
            //double dot = Vector3D.Dot(desiredVector, currentVector);

            //Angle(Vert, limitDown, limitUp);
            //Angle(Horiz, limitLeft, limitRight);

            Vector3D desiredDirectionFlat = Rejection(desiredVector, rotor.WorldMatrix.Up);
            Vector3D currentDirectionFlat = Rejection(currentVector, rotor.WorldMatrix.Up);
            double angle = AngleBetween(desiredDirectionFlat, currentDirectionFlat);
            Vector3D axis = Vector3D.Cross(desiredVector, currentVector);
            angle *= Math.Sign(Vector3D.Dot(axis, rotor.WorldMatrix.Up));
            //angle = GetAllowedRotationAngle(angle, rotor);
            if (v)
            {
                double speed0 = VanglePrev1 - VanglePrev0;
                double speed1 = VanglePrev2 - VanglePrev1;
                double speed2 = VanglePrev3 - VanglePrev2;
                double accel0 = speed0 - speed1;
                double accel1 = speed1 - speed2;
                double accelD = accel1 - accel0;
                double speedNext = speed0 + (accel0 + accelD);
                int angleInt = Convert.ToInt32(Math.Abs(speedNext));
                double pointNext = ToDeg(angle) - speedNext;
                for (int i = 0; i < angleInt - 1 && i < 5; i++)
                {
                    speedNext += accel0;
                    pointNext -= speedNext;
                }
                VanglePrev3 = VanglePrev2;
                VanglePrev2 = VanglePrev1;
                VanglePrev1 = VanglePrev0;
                if (Math.Abs(angle) < 0.005) { angle = 0; }
                VanglePrev0 = ToDeg(angle);

                //if (Math.Abs(pointNext) > Math.Abs(ToDeg(angle))){angle /= 2;}else{}
                int signP = 1;
                if (Math.Sign(pointNext) < 0){signP = -1;}
                int signP3 = 1;
                if (Math.Sign(VanglePrev3) < 0){signP3 = -1;}
                int signA = 1;
                if (Math.Sign(angle) < 0){signA = -1;}

                if (signA != signP)
                {
                    angle = -angle;
                }
                if (signA != signP3)
                {
                    angle = angle * 2;
                }
            }
            else
            {
                double speed0 = HanglePrev1 - HanglePrev0;
                double speed1 = HanglePrev2 - HanglePrev1;
                double speed2 = HanglePrev3 - HanglePrev2;
                double accel0 = speed0 - speed1;
                double accel1 = speed1 - speed2;
                double accelD = accel1 - accel0;
                double speedNext = speed0 + (accel0 + accelD);
                int angleInt = Convert.ToInt32(Math.Abs(speedNext));
                double pointNext = ToDeg(angle) - speedNext;
                for (int i = 0; i < angleInt - 1 && i < 5; i++)
                {
                    speedNext += accel0;
                    pointNext -= speedNext;
                }
                HanglePrev3 = HanglePrev2;
                HanglePrev2 = HanglePrev1;
                HanglePrev1 = HanglePrev0;
                if (Math.Abs(angle) < 0.005) { angle = 0; }
                HanglePrev0 = ToDeg(angle);

                //if (Math.Abs(pointNext) > Math.Abs(ToDeg(angle))){angle /= 2;}else{}
                int signP = 1;
                if (Math.Sign(pointNext) < 0) { signP = -1; }
                int signP3 = 1;
                if (Math.Sign(HanglePrev3) < 0) { signP3 = -1; }
                int signA = 1;
                if (Math.Sign(angle) < 0) { signA = -1; }

                if (signA != signP)
                {
                    angle = -angle;
                }
                if (signA != signP3)
                {
                    angle = angle * 2;
                }
            }


            //if (Math.Abs(angle) < 0.005) { angle = 0; }
            //rotor.TargetVelocityRad = Clamp((float)-horizontalSpeedLimit, (float)horizontalSpeedLimit, (float)angle * MathHelper.RadiansPerSecondToRPM);
            //rotor.TargetVelocityRad = (float)angle * MathHelper.RadiansPerSecondToRPM;

            //rotor.TargetVelocityRad = (float)angle / 10;
            //rotor.TargetVelocityRad = ToRad((float)angle * 60);
            //rotor.TargetVelocityRad = ((float)angle * 60);

            rotor.TargetVelocityRPM = ((float)angle * 60);


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
        double AngleBetween(Vector3D a, Vector3D b)
        {
            if (Vector3D.IsZero(a) || Vector3D.IsZero(b) || a.Equals(b)) { return 0; }
            else
            {
                double math = Math.Acos(MathHelper.Clamp(a.Dot(b) / Math.Sqrt(a.LengthSquared() * b.LengthSquared()), -1, 1));
                //if (Math.Abs(math) < 0.001) { return 0; }
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