﻿using Sandbox.Game.EntityComponents;
using Sandbox.ModAPI.Ingame;
using Sandbox.ModAPI.Interfaces;
using SpaceEngineers.Game.ModAPI.Ingame;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics.Eventing.Reader;
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
//using static SpaceEngineers.Game.VoiceChat.OpusDevice;
using static VRage.Game.ModAPI.Ingame.Utilities.MyCommandLine;

namespace IngameScript
{
    partial class Program : MyGridProgram
    {
        ////////////////////////////////////////////////////////////////
        //         inspired by Rdav's Guided Missile Script           //
        ////////////////////////////////////////////////////////////////
        string MissileTag = "[SRM]";
        bool UseAdvancedTargetting = true;
        // custom turret controller
        IMyTurretControlBlock Turret = null;
        IMyShipController RC;
        IMyCameraBlock TOWCamera;
        MyDetectedEntityInfo LastSeenTarget;

        List<MISSILE> MISSILES = new List<MISSILE>();
        bool missileOut = false; // is there a missile in the air?
        int missileTimer = 0; // ticks since launch
        int guidanceDelay = 120; // wait untill this much time has passed before activating the guidance (60 = 1 second)
        //Consts
        double Global_Timestep = 0.016;
        const double _45_deg = Math.PI / 4; // 45 degrees in radians;; The max allowed 'tilt' of the missile (per axis)

        class MISSILE
        {
            //Terminal Blocks On Each Missile
            public IMyGyro GYRO;
            public IMyTurretControlBlock TURRET;
            public IMyTerminalBlock MERGE;
            public IMyThrust THRUSTER;
            //public IMyTerminalBlock POWER;
            public List<IMyWarhead> WARHEADS = new List<IMyWarhead>(); //Multiple

            //Permanent Missile Details
            public double FuseDistance = 7;

            //Runtime Assignables
            public bool GUIDANCE_ON = false;
            public bool LAUNCHED = false;
            public Vector3D target_pos_prev = new Vector3D();
            public double target_vel_prev;
            public Vector3D msl_pos_prev = new Vector3D();
            public double msl_vel_prev;

            public double PREV_Roll = 0;
            public double PREV_Pitch = 0;

        }
        public Program()
        {
            Runtime.UpdateFrequency = UpdateFrequency.Update1;
            //Get Custom turret controller
            List<IMyTerminalBlock> TempCollection = new List<IMyTerminalBlock>();
            GridTerminalSystem.GetBlocksOfType<IMyTurretControlBlock>(TempCollection, a => a.CustomName.Contains(MissileTag) && a.DetailedInfo != "NoUse");
            if (TempCollection.Count > 0)
            { Turret = TempCollection[0] as IMyTurretControlBlock; }
            // get cockpit
            GridTerminalSystem.GetBlocksOfType<IMyShipController>(TempCollection, a => a.CustomName.Contains(MissileTag) && a.DetailedInfo != "NoUse");
            if (TempCollection.Count > 0)
            { RC = TempCollection[0] as IMyShipController; }
            // get Camera
            GridTerminalSystem.GetBlocksOfType<IMyCameraBlock>(TempCollection, a => a.CustomName.Contains(MissileTag) && a.DetailedInfo != "NoUse");
            if (TempCollection.Count > 0)
            { TOWCamera = TempCollection[0] as IMyCameraBlock; TOWCamera.EnableRaycast = true; }
        }
        IMyTerminalBlock ME = null;

        IMyTextPanel debugLCD = null;
        public void Main(string argument, UpdateType updateSource)
        {
            //if (ME == null) { ME = GetBlock("asd"); }
            if (debugLCD == null) { debugLCD = (IMyTextPanel)GetBlock("LCD"); }
            Echo("MissileTimer " + missileTimer);
            Echo("missileOut " + missileOut);
            Echo("missiles found: " + MISSILES.Count);
            debugLCD.WriteText("", false);
            debugLCD.WriteText("\nMissileTimer " + missileTimer, true);
            debugLCD.WriteText("\nmissileOut " + missileOut, true);
            debugLCD.WriteText("\nmissiles found: " + MISSILES.Count, true);
            // add new missiles to list
            // update missiles (pos && steering)
            //Turret.Camera = TOWCamera;
            GetNewMissiles();
            if (!Turret.GetTargetedEntity().IsEmpty())
            {
                Echo("Target FOUND");
                Echo("IsAimed: " + Turret.IsAimed);
                debugLCD.WriteText("\nTarget FOUND", true);
                debugLCD.WriteText("\nIsAimed: " + Turret.IsAimed, true);
                if (MISSILES.Count > 0 && Turret.IsAimed && !missileOut)
                {
                    LaunchMissile();
                }
            }
            else
            {
                Echo("No target");
                debugLCD.WriteText("\nNo target", true);
            }

            if (missileOut)
            {
                missileTimer++;
                //Runs Guidance Block (foreach missile)
                //---------------------------------------
                for (int i = 0; i < MISSILES.Count; i++)
                {
                    var ThisMissile = MISSILES[i];

                    //Runs Standard System Guidance
                    if (ThisMissile.GUIDANCE_ON == true)
                    { STD_GUIDANCE(ThisMissile); }

                    //Fires Straight (NO OVERRIDES)
                    else if (ThisMissile.GUIDANCE_ON == false)
                    {
                        if (missileTimer > guidanceDelay && ThisMissile.LAUNCHED)
                        { ThisMissile.GUIDANCE_ON = true; }
                    }

                    // Remove missile if out of range or damaged
                    bool Isgyroout = ThisMissile.GYRO.CubeGrid.GetCubeBlock(ThisMissile.GYRO.Position) == null;
                    bool Isthrusterout = ThisMissile.THRUSTER.CubeGrid.GetCubeBlock(ThisMissile.THRUSTER.Position) == null;
                    bool Isouttarange = (ThisMissile.GYRO.GetPosition() - Me.GetPosition()).LengthSquared() > 9000 * 9000;
                    if (Isgyroout || Isthrusterout || Isouttarange)
                    {
                        MISSILES.Remove(ThisMissile);
                        missileOut = false;
                    }

                }
            }
            //if (GYRO == null) { Echo("GYRO is null"); GYRO = GetBlock("Gyroscope 2") as IMyGyro; }
            //else { Echo("GYRO OK: " + val); }
            //val++;
            //if (val > 60) { val = -60; }
            //GYRO.GyroOverride = true;
            //GYRO.Roll = val * MathHelper.RPMToRadiansPerSecond;
        }
        /// <summary>
        /// Launches the missile
        /// </summary>
        void LaunchMissile()
        {
            //MISSILES[0].THRUSTER.ThrustOverride = 35000;
            MISSILES[0].THRUSTER.ApplyAction("OnOff_On");
            MISSILES[0].MERGE.ApplyAction("OnOff_Off");
            MISSILES[0].GYRO.GyroOverride = true;
            MISSILES[0].LAUNCHED = true;
            missileOut = true;
            missileTimer = 0;
        }
        void STD_GUIDANCE(MISSILE This_Missile)
        {
            //Finds Current Target
            MyDetectedEntityInfo Target = Turret.GetTargetedEntity();
            if (Target.IsEmpty()) { Target = LastSeenTarget; }
            else { LastSeenTarget = Target; }
            Vector3D target_pos = Target.Position;
            Vector3D target_pos_prev = This_Missile.target_pos_prev;
            Vector3D target_dir = target_pos - target_pos_prev;
            Vector3D target_travel = Target.Velocity;
            double target_vel = target_travel.Length() / Global_Timestep;
            double target_vel_prev = This_Missile.target_vel_prev;
            double target_accel = target_vel - target_vel_prev;

            //Sorts CurrentVelocities
            Vector3D msl_pos = This_Missile.GYRO.CubeGrid.WorldVolume.Center;
            Vector3D msl_pos_prev = This_Missile.msl_pos_prev;
            Vector3D msl_travel = msl_pos - msl_pos_prev;
            double msl_vel = msl_travel.Length() / Global_Timestep;
            double msl_vel_prev = This_Missile.msl_vel_prev;
            double msl_accel = msl_vel - msl_vel_prev;

            ///DEBUG
            Vector3D aimpoint = target_pos;

            // get target distance
            double target_distance = (msl_pos - target_pos).Length();

            if (UseAdvancedTargetting)
            {
                double t = AdvancedPreditction_T(target_pos, target_dir, target_accel, target_vel, msl_pos, msl_accel, msl_vel);
            }
            else
            {
                //// 1st iterations of look-ahead calculations
                //double msl_time = TimeToReach(msl_vel, msl_accel, target_distance);
                //double target_travel_distance = TraveledDistance(target_vel, target_accel, msl_time);
                //Vector3D aimpoint = target_pos + (target_dir * target_travel_distance);
                //// 2nd iteration
                //target_distance = (msl_pos - aimpoint).Length();
                //// 2 cycles of look-ahead calculations
                //msl_time = TimeToReach(msl_vel, msl_accel, target_distance);
                //target_travel_distance = TraveledDistance(target_vel, target_accel, msl_time);
                //aimpoint = target_pos + (target_dir * target_travel_distance);
            }

            // set rotation
            // a.k.a: we have the aimpoint, from here, it's the vector angly bit that needs coding
            //Vector3D msl_forwards = This_Missile.THRUSTER.WorldMatrix.Backward;
            Vector3D msl_forwards = WorldToBody(This_Missile.THRUSTER.WorldMatrix.Backward, This_Missile.THRUSTER);
            msl_travel = WorldToBody(msl_travel, This_Missile.THRUSTER);

            Vector3D aim_dir = Vector3D.Normalize(aimpoint - msl_pos);
            VEcho("AD_w: ", aim_dir);
            aim_dir = WorldToBody(aim_dir, This_Missile.THRUSTER);
            VEcho("AD_b: ", aim_dir);


            //VEcho("msl_pos", msl_pos);
            //VEcho("aim", aimpoint);

            /// TODO:
            /// Compensate gravity

            //Vector3D thruster_right = This_Missile.THRUSTER.WorldMatrix.Right;
            Vector3D thruster_right = WorldToBody(This_Missile.THRUSTER.WorldMatrix.Right, This_Missile.THRUSTER);
            Vector3D thruster_up = WorldToBody(This_Missile.THRUSTER.WorldMatrix.Up, This_Missile.THRUSTER);

            double roll; double pitch = 0;
            float g_roll = AngleGyro(msl_forwards, thruster_right, msl_travel, aim_dir, This_Missile.PREV_Roll, out roll);
            float g_pitch = AngleGyro(msl_forwards, thruster_up, msl_travel, aim_dir, This_Missile.PREV_Pitch, out pitch);
            //Echo(String.Format("Roll: {0:N2}", roll));
            //Echo(String.Format("g_roll: {0:N2}", g_roll));
            //Echo(String.Format("pitch: {0:N2}", pitch));
            //Echo(String.Format("g_pitch: {0:N2}", g_pitch));
            //debugLCD.WriteText("\n" + String.Format("Roll: {0:N2}", roll), true);
            //debugLCD.WriteText("\n" + String.Format("g_roll: {0:N2}", g_roll), true);
            //debugLCD.WriteText("\n" + String.Format("pitch: {0:N2}", pitch), true);
            //debugLCD.WriteText("\n" + String.Format("g_pitch: {0:N2}", g_pitch), true);

            This_Missile.GYRO.Roll = g_roll;
            This_Missile.GYRO.Pitch = g_pitch;

            //Updates For Next Tick Round
            This_Missile.target_pos_prev = target_pos;
            This_Missile.target_vel_prev = target_vel;
            This_Missile.msl_pos_prev = msl_pos;
            This_Missile.PREV_Roll = roll;
            This_Missile.PREV_Pitch = pitch;

            //Detonates warheads in close proximity
            if (target_distance * target_distance < 20 * 20 && This_Missile.WARHEADS.Count > 0) //Arms
            { foreach (var item in This_Missile.WARHEADS) { (item as IMyWarhead).IsArmed = true; } }
            if (target_distance * target_distance < This_Missile.FuseDistance * This_Missile.FuseDistance && This_Missile.WARHEADS.Count > 0) //A mighty earth shattering kaboom
            { (This_Missile.WARHEADS[0] as IMyWarhead).Detonate(); }

        }
        void VEcho(string str, Vector3D v)
        {
            //Echo(String.Format("{0}: {1:N3};{2:N3};{3:N3}", str, v.X, v.Y, v.Z));
            //debugLCD.WriteText("\n" + String.Format("{0}: {1:N3};{2:N3};{3:N3}", str, v.X, v.Y, v.Z), true);
        }
        float AngleGyro(Vector3D msl_forwards, Vector3D thruster_vector, Vector3D msl_travel, Vector3D aim_dir, double oldAngle, out double newAngle)
        {
            Vector3D MFT_normal = msl_forwards.Cross(thruster_vector);
            // msl travel direction (flattened to the plane defined by msl_forwards and thruster_vector)
            Vector3D rejected_MT = Rejection(msl_travel, MFT_normal);
            // aimpoint direction (flattened to the plane defined by msl_forwards and thruster_vector)
            Vector3D rejected_AD = Rejection(aim_dir, MFT_normal);

            Vector3D thruster_vector_back = -thruster_vector;

            double alpha = Vector3D.Angle(rejected_MT, rejected_AD);
            double beta = Vector3D.Angle(msl_forwards, rejected_AD);
            double gamma = Vector3D.Angle(msl_forwards, rejected_MT);
            double beta_target = 2 * alpha;
            double beta_distance;
            if (beta_target > _45_deg) { beta_target = _45_deg; }

            // MF is on the wrong 'side' of AD
            if (gamma < alpha + beta)
            {
                beta_distance = Math.Abs(beta + beta_target);
            }
            else
            {
                beta_distance = Math.Abs(beta_target - beta);
            }


            ///  //Post Setting Factors
            ///  NewYaw = ShipForwardAzimuth;
            ///  NewPitch = ShipForwardElevation;
            ///  
            ///  //Applies Some PID Damping
            ///  ShipForwardAzimuth = ShipForwardAzimuth + DAMPINGGAIN * ((ShipForwardAzimuth - YawPrev) / Global_Timestep);
            ///  ShipForwardElevation = ShipForwardElevation + DAMPINGGAIN * ((ShipForwardElevation - PitchPrev) / Global_Timestep);

            double GAIN = 18;
            double DAMPINGGAIN = 0.3;

            newAngle = beta_distance;

            //beta_distance = beta_distance + DAMPINGGAIN * ((beta_distance - oldAngle) / Global_Timestep);

            //double g_speed = beta_distance * MathHelper.RadiansPerSecondToRPM;
            double g_speed = beta_distance;

            //Echo(String.Format("alpha: {0:N2}", ToDeg(alpha)));
            //Echo(String.Format("beta: {0:N2}", ToDeg(beta)));
            //Echo(String.Format("gamma: {0:N2}", ToDeg(beta)));
            //Echo(String.Format("beta_target: {0:N2}", ToDeg(beta_target)));
            //Echo(String.Format("beta_distance: {0:N2}", ToDeg(beta_distance)));
            //Echo(String.Format("g_speed: {0:N2}\n", g_speed));
            //debugLCD.WriteText("\n" + String.Format("alpha: {0:N2}", ToDeg(alpha)), true);
            //debugLCD.WriteText("\n" + String.Format("beta: {0:N2}", ToDeg(beta)), true);
            //debugLCD.WriteText("\n" + String.Format("gamma: {0:N2}", ToDeg(beta)), true);
            //debugLCD.WriteText("\n" + String.Format("beta_target: {0:N2}", ToDeg(beta_target)), true);
            //debugLCD.WriteText("\n" + String.Format("beta_distance: {0:N2}", ToDeg(beta_distance)), true);
            //debugLCD.WriteText("\n" + String.Format("g_speed: {0:N2}\n", g_speed), true);

            //VEcho("MF: ", msl_forwards);
            //VEcho("MT: ", msl_travel);
            //VEcho("r-MT: ", rejected_MT);
            VEcho("AD: ", aim_dir);
            VEcho("MFT_normal: ", MFT_normal);
            VEcho("r-AD: ", rejected_AD);
            VEcho("TR: ", thruster_vector);
            VEcho("TR': ", thruster_vector_back);
            //Echo(String.Format("AD-TR: {0:N2}", Vector3D.Angle(rejected_AD, thruster_vector)));
            //Echo(String.Format("AD-TR': {0:N2}", Vector3D.Angle(rejected_AD, thruster_vector_back)));
            //Echo("AD-TR' < AD-TR: " + (Vector3D.Angle(rejected_AD, thruster_vector_back) < Vector3D.Angle(rejected_AD, thruster_vector)));
            //debugLCD.WriteText("\n" + String.Format("AD-TR: {0:N2}", Vector3D.Angle(rejected_AD, thruster_vector)), true);
            //debugLCD.WriteText("\n" + String.Format("AD-TR': {0:N2}", Vector3D.Angle(rejected_AD, thruster_vector_back)), true);
            //debugLCD.WriteText("\n" + "AD-TR' < AD-TR: " + (Vector3D.Angle(rejected_AD, thruster_vector_back) < Vector3D.Angle(rejected_AD, thruster_vector)), true);

            // S
            if (Vector3D.Angle(rejected_AD, thruster_vector_back) < Vector3D.Angle(rejected_AD, thruster_vector))
            { g_speed *= -1; }

            //Echo(String.Format("g_speed 2: {0:N2}", ToDeg(g_speed)));
            //debugLCD.WriteText("\n" + String.Format("g_speed 2: {0:N2}", ToDeg(g_speed)), true);

            //GYRO.Roll = (float)(g_speed * g_dir);
            //return (float)MathHelper.Clamp(g_speed, -1000, 1000);
            return (float)g_speed;
        }
        public Vector3D Rejection(Vector3D a, Vector3D b)
        {
            // https://en.wikipedia.org/wiki/Vector_projection - rejection section
            if (Vector3D.IsZero(a) || Vector3D.IsZero(b))
                return Vector3D.Zero;

            return a - a.Dot(b) / b.LengthSquared() * b;
        }
        public Vector3D RotateAbout(Vector3D v, Vector3D k, double theta)
        {
            // formula wikipedia: https://en.wikipedia.org/wiki/Rodrigues'_rotation_formula#Statement
            return v * Math.Cos(theta) +
                Vector3D.Cross(k, v) * Math.Sin(theta) +
                k * Vector3D.Dot(k, v) * (1 - Math.Cos(theta));
        }
        /// <summary>
        /// converts World direction to Body Direction (&& normalizes it)
        /// </summary>
        /// <param name="V">World direction</param>
        /// <param name="block">reference block</param>
        /// <returns></returns>
        Vector3D WorldToBody(Vector3D V, IMyTerminalBlock block)
        {
            // Convert worldDirection into a local direction
            V = Vector3D.TransformNormal(V, MatrixD.Transpose(block.WorldMatrix.GetOrientation()));
            return Vector3D.Normalize(V);
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="P0">Enemy Pos</param>
        /// <param name="V0">Enemy Heading</param>
        /// <param name="A0">Enemy Acceleration</param>
        /// <param name="U0">Enemy Speed</param>
        /// <param name="P1">Missile Pos</param>
        /// <param name="A1">Missile Acceleration</param>
        /// <param name="U1">Missile Speed</param>
        /// <returns>time untill impact</returns>
        double AdvancedPreditction_T(Vector3D P0, Vector3D V0, double A0, double U0, Vector3D P1, double A1, double U1)
        {
            // move target and msl vectors such that msl is at {1,1,1}
            double P1dx = 1 - P1.X;
            double P1dy = 1 - P1.Y;
            double P1dz = 1 - P1.Z;
            P1.X += P1dx;
            P1.Y += P1dy;
            P1.Z += P1dz;

            P0.X += P1dx;
            P0.Y += P1dy;
            P0.Z += P1dz;

            P0 /= 100;
            V0 /= 100;
            A0 /= 100;
            U0 /= 100;
            P1 /= 100;
            A1 /= 100;
            U1 /= 100;

            debugLCD.WriteText("\n" + String.Format("V0: {0}", V0), true);
            /*
            The expanded formula for target prediction in 3D with linear acceleration
            2P0.X ^ 2 + 4P0.X* V0.X* U0*T + 2P0.X* A0*V0.X * T ^ 2 + 2U0 * V0.X * T + A0 * V0.X * T ^ 2 - 4 * P1.X * P0.X + 4 * P1.X * U0 * V0.X * T + 2 * P1.X * A0 * V0.X * T ^ 2 +
            2P0.Y ^ 2 + 4P0.Y* V0.Y* U0*T + 2P0.Y* A0*V0.Y * T ^ 2 + 2U0 * V0.Y * T + A0 * V0.Y * T ^ 2 - 4 * P1.Y * P0.Y + 4 * P1.Y * U0 * V0.Y * T + 2 * P1.Y * A0 * V0.Y * T ^ 2 +
            2P0.Z ^ 2 + 4P0.Z* V0.Z* U0*T + 2P0.Z* A0*V0.Z * T ^ 2 + 2U0 * V0.Z * T + A0 * V0.Z * T ^ 2 - 4 * P1.Z * P0.Z + 4 * P1.Z * U0 * V0.Z * T + 2 * P1.Z * A0 * V0.Z * T ^ 2
            - 2U1 * T ^ 2 - 2U1 * A1 * T ^ 3 - (A1 ^ 2 * T ^ 4) / 2 = 0
            */
            // quartic formula https://en.wikipedia.org/wiki/Quartic_function

            double a = -(A1 * A1) / 2;
            double b = -2 * U1 * A1;
            double c = -2 * U1
            + 2 * P0.X * A0 * V0.X
            + 2 * P0.Y * A0 * V0.Y
            + 2 * P0.Z * A0 * V0.Z
            + A0 * V0.X
            + A0 * V0.Y
            + A0 * V0.Z
            + 2 * P1.X * A0 * V0.X
            + 2 * P1.Y * A0 * V0.Y
            + 2 * P1.Z * A0 * V0.Z;
            double d = +4 * P0.X * V0.X * U0
            + 4 * P0.Y * V0.Y * U0
            + 4 * P0.Z * V0.Z * U0
            + 2 * U0 * V0.X
            + 2 * U0 * V0.Y
            + 2 * U0 * V0.Z
            + 4 * P1.X * U0 * V0.X
            + 4 * P1.Y * U0 * V0.Y
            + 4 * P1.Z * U0 * V0.Z;
            double e = +2 * P0.X * P0.X
            + 2 * P0.Y * P0.Y
            + 2 * P0.Z * P0.Z
            - 4 * P1.X * P0.X
            - 4 * P1.Y * P0.Y
            - 4 * P1.Z * P0.Z;


            double p = (8 * a * c - 3 * b * b) / (8 * a * a);
            double q = (b * b * b - 4 * a * b * c + 8 * a * a * d) / (8 * a * a * a);

            double a2 = a * a;
            double b2 = b * b;
            double c2 = c * c;
            double d2 = d * d;
            double e2 = e * e;

            double a3 = a * a * a;
            double b3 = b * b * b;
            double c3 = c * c * c;
            double d3 = d * d * d;
            double e3 = e * e * e;

            double a4 = a2 * a2;
            double b4 = b2 * a2;
            double c4 = c2 * a2;
            double d4 = d2 * a2;
            double e4 = e2 * a2;





            // https://wikimedia.org/api/rest_v1/media/math/render/svg/654adf3cf7f9d0750b278d62a52014c015f6e22a
            double delta = 256 * a3 * e3 - 192 * a2 * b * d * e2 - 128 * a2 * c2 * e2 + 144 * a2 * c * d2 * e - 27 * a2 * d4
                + 144 * a * b2 * c * e2 - 6 * a * b2 * d2 * e - 80 * a * b * c2 * d * e + 18 * a * b * c * d3 + 16 * a * c4 * e
                - 4 * a * c3 * d2 - 27 * b4 * e2 + 18 * b3 * c * d * e - 4 * b3 * d3 - 4 * b2 * c3 * e + b2 * c2 * d2;
            double delta0 = c * c - 3 * b * d + 12 * a * e;
            double delta1 = 2 * c * c * c - 9 * b * c * d + 27 * b * b * e + 27 * a * d * d - 72 * a * c * e;

            //Q = Math.Pow((delta1 + Math.Sqrt(delta2_1 - 4 * delta3_0)) / 2, 1 / 3);
            double Q = Math.Pow((delta1 + Math.Sqrt(-27 * delta)) / 2, 1 / 3);

            double S = (1 / 2) * Math.Sqrt((-2 / 3) * p + (1 / (3 * a)) * (Q + delta0 / Q));

            double[] x = new double[4];
            x[0] = -b / (4 * a) - S + (1 / 2) * Math.Sqrt(-4 * S * S - 2 * p + q / S);
            x[1] = -b / (4 * a) - S - (1 / 2) * Math.Sqrt(-4 * S * S - 2 * p + q / S);
            x[2] = -b / (4 * a) + S + (1 / 2) * Math.Sqrt(-4 * S * S - 2 * p - q / S);
            x[3] = -b / (4 * a) + S - (1 / 2) * Math.Sqrt(-4 * S * S - 2 * p - q / S);

            debugLCD.WriteText("\n" + String.Format("a: {0:N2}", a), true);
            debugLCD.WriteText("\n" + String.Format("b: {0:N2}", b), true);
            debugLCD.WriteText("\n" + String.Format("c: {0:N2}", c), true);
            debugLCD.WriteText("\n" + String.Format("d: {0:N2}", d), true);
            debugLCD.WriteText("\n" + String.Format("e: {0:N2}", e), true);

            debugLCD.WriteText("\n" + String.Format("p: {0:N2}", p), true);
            debugLCD.WriteText("\n" + String.Format("q: {0:N2}", q), true);
            debugLCD.WriteText("\n" + String.Format("delta: {0:N2}", delta), true);
            debugLCD.WriteText("\n" + String.Format("delta0: {0:N2}", delta0), true);
            debugLCD.WriteText("\n" + String.Format("delta1: {0:N2}", delta1), true);
            debugLCD.WriteText("\n" + String.Format("Q: {0:N2}", Q), true);
            debugLCD.WriteText("\n" + String.Format("S: {0:N2}", S), true);
            debugLCD.WriteText("\n" + String.Format("x[0]: {0:N2}", x[0]), true);
            debugLCD.WriteText("\n" + String.Format("x[1]: {0:N2}", x[1]), true);
            debugLCD.WriteText("\n" + String.Format("x[2]: {0:N2}", x[2]), true);
            debugLCD.WriteText("\n" + String.Format("x[3]: {0:N2}", x[3]), true);


            Array.Sort(x);
            Echo("x[0]: " + x[0]);
            Echo("x[1]: " + x[1]);
            Echo("x[2]: " + x[2]);
            Echo("x[3]: " + x[3]);

            return 0;
        }
        /// <summary>
        /// Time it takes to go a set distance with linear acceleration
        /// </summary>
        /// <param name="U">Speed or Velocity (m/s)</param>
        /// <param name="A">Acceleration (m/s^2)</param>
        /// <param name="S">Distance (m)</param>
        /// <returns></returns>
        double TimeToReach(double U, double A, double S)
        {
            double T1 = (-U + Math.Sqrt(U * U + 2 * A * S)) / A;
            double T2 = (-U - Math.Sqrt(U * U + 2 * A * S)) / A;
            if (T1 > 0) { return T1; }
            else { return T2; }
        }
        /// <summary>
        /// Distance travelled in a set amount of time with linear acceleration
        /// </summary>
        /// <param name="U">Speed or Velocity (m/s)</param>
        /// <param name="A">Acceleration (m/s^2)</param>
        /// <param name="T">Time (s)</param>
        /// <returns></returns>
        double TraveledDistance(double U, double A, double T)
        {
            double S = (U * T) + (A * T * T) / 2;
            return S;
        }
        /// <summary>
        /// Adds all new missiles to 'MISSILES' list
        /// </summary>
        void GetNewMissiles()
        {
            IMyGyro Gyro = null;
            List<IMyTerminalBlock> TempCollection = new List<IMyTerminalBlock>();
            GridTerminalSystem.GetBlocksOfType<IMyGyro>(TempCollection, a => a.CustomName.Contains(MissileTag));
            if (TempCollection.Count > 0)
            { Gyro = TempCollection[0] as IMyGyro; }
            else { return; }
            bool exists = false;
            foreach (MISSILE msl in MISSILES)
            {
                if (msl.GYRO.Equals(Gyro))
                {
                    exists = true;
                    break;
                }
            }
            if (!exists)
            {
                MISSILE tmp = new MISSILE();
                // get new gyro
                GridTerminalSystem.GetBlocksOfType<IMyGyro>(TempCollection, a => a.CustomName.Contains(MissileTag));
                if (TempCollection.Count > 0)
                { tmp.GYRO = TempCollection[0] as IMyGyro; }
                else { return; }
                // get new thruster
                GridTerminalSystem.GetBlocksOfType<IMyThrust>(TempCollection, a => a.CustomName.Contains(MissileTag));
                if (TempCollection.Count > 0)
                { tmp.THRUSTER = TempCollection[0] as IMyThrust; }
                else { return; }
                // get new merge
                GridTerminalSystem.GetBlocksOfType<IMyShipMergeBlock>(TempCollection, a => a.CustomName.Contains(MissileTag));
                if (TempCollection.Count > 0)
                { tmp.MERGE = TempCollection[0] as IMyShipMergeBlock; }
                else { return; }
                // get new warheads
                GridTerminalSystem.GetBlocksOfType<IMyWarhead>(TempCollection, a => a.CustomName.Contains(MissileTag));
                if (TempCollection.Count > 0)
                {
                    tmp.WARHEADS = new List<IMyWarhead>();
                    foreach (IMyWarhead item in TempCollection)
                    { tmp.WARHEADS.Add(item); }
                }
                else { return; }
                // turret is always the targetting turret
                tmp.TURRET = Turret;
                MISSILES.Add(tmp);
            }
        }

        float ToDeg(float rad) { return MathHelper.ToDegrees(rad); }
        double ToDeg(double rad) { return MathHelper.ToDegrees(rad); }
        float ToRad(float deg) { return MathHelper.ToRadians(deg); }
        double ToRad(double deg) { return MathHelper.ToRadians(deg); }

        public IMyTerminalBlock GetBlock(string name)
        {
            IMyTerminalBlock block = GridTerminalSystem.GetBlockWithName(name);
            return block;
        }
    }
}
