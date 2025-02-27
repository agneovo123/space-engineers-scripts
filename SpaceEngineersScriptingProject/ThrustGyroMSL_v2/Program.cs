﻿using Sandbox.Game.Entities;
using Sandbox.Game.EntityComponents;
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
//using static VRage.Game.ModAPI.Ingame.Utilities.MyCommandLine;

namespace IngameScript
{
    partial class Program : MyGridProgram
    {
        #region mdk preserve
        ////////////////////////////////////////////////////////////////
        //         inspired by Rdav's Guided Missile Script           //
        ////////////////////////////////////////////////////////////////
        const string MissileTag = "[SRM]";

        const int guidanceDelay = 30; // how many ticks before activating the guidance (60 = 1 second)
        const int thrusterDelay = 0; // how many ticks before activating the thruster (60 = 1 second)
        const double FuseDistance = 7; // proximity fuse (smart)
        const double ArmDistance = 50; // arming distance
        const double MissileMaxAngle = Math.PI / 4; // The max allowed 'tilt' of the missile (per axis); Math.PI / 4 = 45 degrees in radians

        const double P = 1.0; // gain (between 0 and 1)
        const double I = 0; // comulative error (i don't know how to use this)
        const double D = 0.3; // damping gain

        PID PIDroll = new PID(P, I, D, 0);
        PID PIDpitch = new PID(P, I, D, 0);

        // PID DEBUG
        // const double GAIN = 1.3;
        // const double DAMPINGGAIN = 0.25;


        ////////////////////////////////////////////////////////////////
        //                DON'T EDIT BELOW THIS LINE                  //
        ////////////////////////////////////////////////////////////////
        class endregion { } // this is needed so MDK doesn't eat the last comment lol
        #endregion
        // custom turret controller
        IMyTurretControlBlock Turret = null;
        IMyShipController RC;
        MyDetectedEntityInfo LastSeenTarget;

        List<MISSILE> MISSILES = new List<MISSILE>();
        bool missileOut = false; // is there a missile in the air?
        int missileTimer = 0; // ticks since launch
        double MinDistanceToAimpoint = 99999;

        IMyTextPanel debugLCD = null;

        class MISSILE
        {
            // blocks of the missile
            public IMyGyro GYRO;
            public IMyTurretControlBlock TURRET;
            public IMyTerminalBlock MERGE;
            public IMyThrust THRUSTER;
            public List<IMyBatteryBlock> BATTERIES = new List<IMyBatteryBlock>();
            public List<IMyWarhead> WARHEADS = new List<IMyWarhead>();

            // msl variables
            public bool GUIDANCE_ON = false;
            public bool THRUSTER_ON = false;
            public bool LAUNCHED = false;
            public int msl_time = 0;
            public Vector3D target_pos_prev = new Vector3D();
            public double target_vel_prev;
            public Vector3D msl_pos_prev = new Vector3D();
            public double msl_vel_prev;

            public double PREV_Roll = 0;
            public double PREV_Pitch = 0;

        }

        const double Timestep = 0.016; // how many seconds in a timestep
        const double InverseTimestep = 60;   // how many timesteps in a second
        class PID
        {
            double error;
            double integral; // idk
            double derivative;
            double errorPrev = 0;

            double KP;
            double KI;
            double KD;
            public double setPoint { get; set; } = 0;

            public PID(double P, double I, double D, double setPoint_in)
            {
                KP = P;
                KI = I;
                KD = D;
                setPoint = setPoint_in;
            }
            public double getDriveCommandFromError(double Error)
            {
                // update error
                error = Error;
                // update integral
                integral += error * Timestep;
                // update derivative
                //derivative = CurrentValue + (KD * InverseTimestep) * (errorPrev - error); // why did I add to CV, and *KD ??? what
                derivative = InverseTimestep * (errorPrev - error);
                // update errorprev
                errorPrev = error;

                // create driveCommand
                //double driveCommand = -(KP * error + KI * integral + KD * derivative);
                double driveCommand = -(KP * error + KD * derivative);
                //if (Math.Abs(error) < 0.01 && Math.Abs(driveCommand) < 0.01) { return 0; }
                return driveCommand;
            }
            public double getDriveCommand(double CurrentValue)
            {
                // update errorprev
                errorPrev = error;
                // update error
                error = setPoint - CurrentValue;
                // update integral
                integral += error * Timestep;
                // update derivative
                //derivative = CurrentValue + (KD * InverseTimestep) * (errorPrev - error); // why did I add to CV, and *KD ??? what
                derivative = InverseTimestep * (errorPrev - error);

                // create driveCommand
                double driveCommand = -(KP * error + KI * integral + KD * derivative);
                //if (Math.Abs(error) < 0.01 && Math.Abs(driveCommand) < 0.01) { return 0; }
                return driveCommand;
            }
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
        }

        public void Main(string argument, UpdateType updateSource)
        {
            if (debugLCD == null) { debugLCD = (IMyTextPanel)GetBlock("LCD"); }
            debugLCD.WriteText("", false);
            debugLCD.WriteText("\nMissileTimer " + missileTimer, true);
            debugLCD.WriteText("\nmissileOut " + missileOut, true);
            debugLCD.WriteText("\nmissiles found: " + MISSILES.Count, true);
            debugLCD.WriteText("\ncockpit found: " + !(RC == null), true);
            // add new missiles to list
            // update missiles (pos && steering)
            GetNewMissiles();
            if (!Turret.GetTargetedEntity().IsEmpty())
            {
                debugLCD.WriteText("\nTarget FOUND", true);
                debugLCD.WriteText("\nIsAimed: " + Turret.IsAimed, true);
                if (MISSILES.Count > 0 && Turret.IsAimed && !missileOut)
                {
                    LaunchMissile();
                }
            }
            else
            {
                debugLCD.WriteText("\nNo target", true);
            }

            if (missileOut)
            {
                missileTimer++;
                //---------------------------------------
                for (int i = 0; i < MISSILES.Count; i++)
                {
                    var ThisMissile = MISSILES[i];

                    // 
                    if (ThisMissile.LAUNCHED)
                    {
                        ThisMissile.msl_time++;
                        // turn on thruster after delay
                        if (!ThisMissile.THRUSTER_ON && ThisMissile.msl_time > thrusterDelay)
                        {
                            ThisMissile.THRUSTER_ON = true;
                            ThisMissile.THRUSTER.ApplyAction("OnOff_On");
                        }
                        // turn on guidance
                        if (!ThisMissile.GUIDANCE_ON && ThisMissile.msl_time > guidanceDelay && ThisMissile.LAUNCHED)
                        { ThisMissile.GUIDANCE_ON = true; }
                    }

                    // run guidance
                    if (ThisMissile.GUIDANCE_ON)
                    { STD_GUIDANCE(ThisMissile); }

                    // Remove missile if out of range or damaged
                    bool Isgyroout = ThisMissile.GYRO.CubeGrid.GetCubeBlock(ThisMissile.GYRO.Position) == null;
                    bool Isthrusterout = ThisMissile.THRUSTER.CubeGrid.GetCubeBlock(ThisMissile.THRUSTER.Position) == null;
                    bool Isouttarange = (ThisMissile.GYRO.GetPosition() - Me.GetPosition()).Length() > 9000;
                    if (Isgyroout || Isthrusterout || Isouttarange)
                    {
                        // Server friendly discarding of missiles (turn off thruster, gyro and batteries)
                        ThisMissile.THRUSTER.ApplyAction("OnOff_Off");
                        ThisMissile.GYRO.ApplyAction("OnOff_Off");
                        foreach (IMyTerminalBlock battery in ThisMissile.BATTERIES)
                        { battery.ApplyAction("OnOff_Off"); }
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
            //MISSILES[0].THRUSTER.ThrustOverride = 0;
            //MISSILES[0].THRUSTER.ApplyAction("OnOff_On");

            MISSILES[0].MERGE.ApplyAction("OnOff_Off");
            MISSILES[0].GYRO.GyroOverride = true;
            MISSILES[0].LAUNCHED = true;
            missileOut = true;
            missileTimer = 0;
            // debug
            MinDistanceToAimpoint = 999999;
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
            double target_vel = target_travel.Length();
            double target_vel_prev = This_Missile.target_vel_prev;
            double target_accel = target_vel - target_vel_prev;

            //Sorts CurrentVelocities
            Vector3D msl_pos = This_Missile.GYRO.CubeGrid.WorldVolume.Center;
            Vector3D msl_pos_prev = This_Missile.msl_pos_prev;
            Vector3D msl_travel = msl_pos - msl_pos_prev;
            double msl_travel_len = msl_travel.Length();
            double msl_vel = msl_travel.Length();
            double msl_vel_prev = This_Missile.msl_vel_prev;
            double msl_accel = msl_vel - msl_vel_prev;


            Vector3D gravity = RC.GetNaturalGravity();
            // the effect of grav. almost fully removed with this one:
            //debugLCD.WriteText("\n" + String.Format("msl_travel: {0:N2} {1:N2} {2:N2}", msl_travel.X, msl_travel.Y, msl_travel.Z), true);
            //msl_travel = msl_travel + gravity * Global_Timestep;
            //debugLCD.WriteText("\n" + String.Format("msl_travel: {0:N2} {1:N2} {2:N2}", msl_travel.X, msl_travel.Y, msl_travel.Z), true);


            Vector3D aimpoint = GetInterCeptionPoint(msl_pos, msl_vel, target_pos, target_travel);
            ///DEBUG
            //aimpoint = target_pos;

            // set rotation
            // a.k.a: we have the aimpoint, from here, it's the vector angly bit that needs coding
            Vector3D msl_forwards = WorldToBody(This_Missile.THRUSTER.WorldMatrix.Backward, This_Missile.THRUSTER);
            msl_travel = WorldToBody(msl_travel, This_Missile.THRUSTER);

            // counter gravity
            // aimpoint += gravityAccel(msl_time, msl_travel, This_Missile.GYRO);

            Vector3D aim_dir = Vector3D.Normalize(aimpoint - msl_pos);
            //VEcho("AD_w: ", aim_dir);
            aim_dir = WorldToBody(aim_dir, This_Missile.THRUSTER);
            //VEcho("AD_b: ", aim_dir);

            double DistanceToAimpoint = (aimpoint - msl_pos).Length();
            debugLCD.WriteText("\n" + String.Format("Distance from Aimpoint: {0:N2}", DistanceToAimpoint), true);

            if (DistanceToAimpoint < MinDistanceToAimpoint) { MinDistanceToAimpoint = DistanceToAimpoint; }
            debugLCD.WriteText("\n" + String.Format("MinDistanceToAimpoint: {0:N2}", MinDistanceToAimpoint), true);

            //debugLCD.WriteText("\n" + String.Format("msl_travel: {0:N2}", (msl_travel * msl_travel_len).Length()), true);

            //VEcho("msl_pos", msl_pos);
            //VEcho("aim", aimpoint);

            /// TODO:
            /// Compensate gravity

            //Vector3D thruster_right = This_Missile.THRUSTER.WorldMatrix.Right;
            Vector3D thruster_right = WorldToBody(This_Missile.THRUSTER.WorldMatrix.Right, This_Missile.THRUSTER);
            Vector3D thruster_up = WorldToBody(This_Missile.THRUSTER.WorldMatrix.Up, This_Missile.THRUSTER);

            float g_roll = AngleError(msl_forwards, thruster_right, msl_travel, msl_travel_len, aim_dir, gravity, This_Missile.THRUSTER);
            float g_pitch = AngleError(msl_forwards, thruster_up, msl_travel, msl_travel_len, aim_dir, gravity, This_Missile.THRUSTER);

            //debugLCD.WriteText("\n" + String.Format("Roll: {0:N2}", roll), true);
            //debugLCD.WriteText("\n" + String.Format("g_roll: {0:N2}", g_roll), true);
            //debugLCD.WriteText("\n" + String.Format("pitch: {0:N2}", pitch), true);
            //debugLCD.WriteText("\n" + String.Format("g_pitch: {0:N2}", g_pitch), true);

            float rollCmd = (float)-PIDroll.getDriveCommandFromError(g_roll);
            float pitchCmd = (float)-PIDpitch.getDriveCommandFromError(g_pitch);

            This_Missile.GYRO.Roll = rollCmd;
            This_Missile.GYRO.Pitch = pitchCmd;

            debugLCD.WriteText("\n" + String.Format("rollCmd: {0:N2}", rollCmd * MathHelper.RadiansPerSecondToRPM), true);
            debugLCD.WriteText("\n" + String.Format("pitchCmd: {0:N2}", pitchCmd * MathHelper.RadiansPerSecondToRPM), true);


            // Updates For Next Tick Round
            This_Missile.target_pos_prev = target_pos;
            This_Missile.target_vel_prev = target_vel;
            This_Missile.msl_pos_prev = msl_pos;

            // Arms warheads in close proximity
            if (DistanceToAimpoint < ArmDistance && This_Missile.WARHEADS.Count > 0) // Arms
            { foreach (var item in This_Missile.WARHEADS) { (item as IMyWarhead).IsArmed = true; } }

            // smart proximity detection.
            // allows the missile to get closer to the target, causing more damage
            msl_travel = msl_pos - msl_pos_prev;
            Vector3D target_pos_next = target_pos + target_dir;
            Vector3D msl_pos_next = msl_pos + msl_travel;

            // Detonates warheads in close proximity
            if (DistanceToAimpoint < FuseDistance && This_Missile.WARHEADS.Count > 0)
            {
                if ((target_pos - msl_pos).Length() < (target_pos_next - msl_pos_next).Length())
                {
                    This_Missile.WARHEADS[0].Detonate();
                }
            }
            //debug
            //if ((This_Missile.WARHEADS[0] as IMyWarhead).IsArmed) { debugLCD.WriteText("\n[MISSILE ARMED]", true); }

        }

        public Vector3D GetInterCeptionPoint(Vector3D mslPos, double mslSpeed, Vector3D targetPos, Vector3D targetVel)
        {
            // Extract target and projectile components
            double
                Tx = targetPos.X,
                Ty = targetPos.Y,
                Tz = targetPos.Z;
            double
                Vtx = targetVel.X,
                Vty = targetVel.Y,
                Vtz = targetVel.Z;
            double
                Px = mslPos.X,
                Py = mslPos.Y,
                Pz = mslPos.Z;

            // Relative position
            double Rx0 = targetPos.X - mslPos.X;
            double Ry0 = targetPos.Y - mslPos.Y;
            double Rz0 = targetPos.Z - mslPos.Z;

            // Quadratic coefficients to solve for t
            double A = Vtx * Vtx + Vty * Vty + Vtz * Vtz - mslSpeed * mslSpeed;
            double B = 2 * (Rx0 * Vtx + Ry0 * Vty + Rz0 * Vtz);
            double C = Rx0 * Rx0 + Ry0 * Ry0 + Rz0 * Rz0;

            // Solve the quadratic equation: At^2 + Bt + C = 0
            double discriminant = B * B - 4 * A * C;

            if (discriminant < 0)
            {
                // No solution (projectile cannot catch up to target)
                return GetInterCeptionPointTooSlow(mslSpeed, (mslPos - targetPos).Length(), targetVel.Length(), targetPos, targetVel);
                //return targetPos;
            }

            // Calculate the two possible solutions for t
            double t1 = (-B + Math.Sqrt(discriminant)) / (2 * A);
            double t2 = (-B - Math.Sqrt(discriminant)) / (2 * A);

            // Choose the smallest positive time
            double t = Math.Min(t1, t2);
            if (t < 0) t = Math.Max(t1, t2); // Check if the other root is positive
            if (t < 0)
            {
                // No valid time
                return GetInterCeptionPointTooSlow(mslSpeed, (mslPos - targetPos).Length(), targetVel.Length(), targetPos, targetVel);
                //return targetPos;
            }

            // Interception point
            double Ix = Tx + Vtx * t;
            double Iy = Ty + Vty * t;
            double Iz = Tz + Vtz * t;
            Vector3D interceptPoint = new Vector3D(Ix, Iy, Iz);

            // Direction of the projectile
            //double Dx = (Ix - Px) / Math.Sqrt((Ix - Px) * (Ix - Px) + (Iy - Py) * (Iy - Py) + (Iz - Pz) * (Iz - Pz));
            //double Dy = (Iy - Py) / Math.Sqrt((Ix - Px) * (Ix - Px) + (Iy - Py) * (Iy - Py) + (Iz - Pz) * (Iz - Pz));
            //double Dz = (Iz - Pz) / Math.Sqrt((Ix - Px) * (Ix - Px) + (Iy - Py) * (Iy - Py) + (Iz - Pz) * (Iz - Pz));

            return interceptPoint;
        }
        public Vector3D GetInterCeptionPointTooSlow(double mslSpeed, double targetDist, double targetSpeed, Vector3D targetPos, Vector3D targetVel)
        {
            // 1st iterations of look-ahead calculations
            double msl_time = TimeToReach(mslSpeed, 0, targetDist);
            double target_travel_distance = TraveledDistance(targetSpeed, 0, msl_time);
            Vector3D aimpoint = targetPos + (targetVel * target_travel_distance);
            return aimpoint;
        }
        double Asin(double d)
        {
            if (d < -1)
            {
                d = -1;
            }
            if (d > 1)
            {
                d = 1;
            }
            return Math.Asin(d);
        }
        private float AngleError(Vector3D msl_forwards, Vector3D thruster_vector, Vector3D msl_travel, double msl_travel_len, Vector3D aim_dir, Vector3D gravity, IMyThrust refBlock)
        {
            // msl_forwards and thruster_vector's cross product
            // a.k.a. the normal of the plane intersecting both msl_forwards and thruster_vector
            Vector3D MFT_normal = msl_forwards.Cross(thruster_vector);
            // msl travel direction (flattened to the plane defined by msl_forwards and thruster_vector)
            Vector3D rejected_MT = Rejection(msl_travel, MFT_normal);
            // aimpoint direction (flattened to the plane defined by msl_forwards and thruster_vector)
            Vector3D rejected_AD = Rejection(aim_dir, MFT_normal);
            // gravity direction (flattened)
            Vector3D rejected_Grav;

            Vector3D thruster_vector_back = -thruster_vector;

            double alpha = Vector3D.Angle(rejected_MT, rejected_AD);
            double beta = Vector3D.Angle(msl_forwards, rejected_AD);
            double gamma = Vector3D.Angle(msl_forwards, rejected_MT);
            double beta_target = 2 * alpha;
            double beta_distance;
            if (beta_target > MissileMaxAngle) { beta_target = MissileMaxAngle; }

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

            //beta_distance = beta_distance + DAMPINGGAIN * ((beta_distance - oldAngle) / Global_Timestep);

            //double g_speed = beta_distance * MathHelper.RadiansPerSecondToRPM;
            double g_speed = beta_distance;

            //debugLCD.WriteText("\n" + String.Format("alpha: {0:N2}", ToDeg(alpha)), true);
            //debugLCD.WriteText("\n" + String.Format("beta: {0:N2}", ToDeg(beta)), true);
            //debugLCD.WriteText("\n" + String.Format("gamma: {0:N2}", ToDeg(beta)), true);
            //debugLCD.WriteText("\n" + String.Format("beta_target: {0:N2}", ToDeg(beta_target)), true);
            debugLCD.WriteText("\n" + String.Format("beta_distance: {0:N2}", ToDeg(beta_distance)), true);
            debugLCD.WriteText("\n" + String.Format("g_speed: {0:N2}", g_speed), true);
            debugLCD.WriteText("\n" + String.Format("thruster_vector: {0:N2},{1:N2},{2:N2}", thruster_vector.X, thruster_vector.Y, thruster_vector.Z), true);
            debugLCD.WriteText("\n" + String.Format("msl_forwards: {0:N2},{1:N2},{2:N2}", msl_forwards.X, msl_forwards.Y, msl_forwards.Z), true);
            debugLCD.WriteText("\n" + String.Format("MFT_normal: {0:N2},{1:N2},{2:N2}", MFT_normal.X, MFT_normal.Y, MFT_normal.Z), true);

            //VEcho("MF: ", msl_forwards);
            //VEcho("MT: ", msl_travel);
            //VEcho("r-MT: ", rejected_MT);
            //VEcho("AD: ", aim_dir);
            //VEcho("MFT_normal: ", MFT_normal);
            //VEcho("r-AD: ", rejected_AD);
            //VEcho("TR: ", thruster_vector);
            //VEcho("TR': ", thruster_vector_back);
            //Echo(String.Format("AD-TR: {0:N2}", Vector3D.Angle(rejected_AD, thruster_vector)));
            //Echo(String.Format("AD-TR': {0:N2}", Vector3D.Angle(rejected_AD, thruster_vector_back)));
            //Echo("AD-TR' < AD-TR: " + (Vector3D.Angle(rejected_AD, thruster_vector_back) < Vector3D.Angle(rejected_AD, thruster_vector)));
            //debugLCD.WriteText("\n" + String.Format("AD-TR: {0:N2}", Vector3D.Angle(rejected_AD, thruster_vector)), true);
            //debugLCD.WriteText("\n" + String.Format("AD-TR': {0:N2}", Vector3D.Angle(rejected_AD, thruster_vector_back)), true);
            //debugLCD.WriteText("\n" + "AD-TR' < AD-TR: " + (Vector3D.Angle(rejected_AD, thruster_vector_back) < Vector3D.Angle(rejected_AD, thruster_vector)), true);

            // S (why no description?)
            // ??? reverse turn direction when angle of attack > 90 ???
            if (Vector3D.Angle(rejected_AD, thruster_vector_back) < Vector3D.Angle(rejected_AD, thruster_vector))
            { g_speed *= -1; }

            //debugLCD.WriteText("\n" + String.Format("g_speed 2: {0:N2}", ToDeg(g_speed)), true);

            //GYRO.Roll = (float)(g_speed * g_dir);
            //return (float)MathHelper.Clamp(g_speed, -1000, 1000);
            return (float)g_speed;
        }

        Vector3D gravityAccel(double msl_time, Vector3D msl_travel, IMyTerminalBlock reference)
        {
            // gravity's lenght is it's value in ms2 (9.81 on the surface of Earthlike)
            Vector3D gravity = RC.GetNaturalGravity();
            Vector3D gravityBody = WorldToBody(gravity, reference);
            // get acceleration in direction of gravity -- let's skip this for now
            double distance = TraveledDistance(0, gravity.Length(), msl_time);
            debugLCD.WriteText("\n" + String.Format("gravity: {0:N2} {1:N2} {2:N2}", gravity.X, gravity.Y, gravity.Z), true);
            debugLCD.WriteText("\n" + String.Format("gravityBody: {0:N2} {1:N2} {2:N2}", gravityBody.X, gravityBody.Y, gravityBody.Z), true);
            //debugLCD.WriteText("\n" + String.Format("gravity.Length(): {0:N2}", gravity.Length()), true);
            debugLCD.WriteText("\n" + String.Format("distance: {0:N2}", distance), true);

            return -Vector3D.Normalize(gravity) * distance;
        }
        void VEcho(string str, Vector3D v)
        {
            //Echo(String.Format("{0}: {1:N3};{2:N3};{3:N3}", str, v.X, v.Y, v.Z));
            debugLCD.WriteText("\n" + String.Format("{0}: {1:N3};{2:N3};{3:N3}", str, v.X, v.Y, v.Z), true);
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
                // get new batteries
                GridTerminalSystem.GetBlocksOfType<IMyBatteryBlock>(TempCollection, a => a.CustomName.Contains(MissileTag));
                if (TempCollection.Count > 0)
                {
                    tmp.BATTERIES = new List<IMyBatteryBlock>();
                    foreach (IMyBatteryBlock item in TempCollection)
                    { tmp.BATTERIES.Add(item); }
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
