using System;
using System.Collections.Generic;

// Space Engineers DLLs
using Sandbox.ModAPI.Ingame;
using Sandbox.ModAPI.Interfaces;
using Sandbox.Game.EntityComponents;
using VRageMath;
using VRage.Game;
using VRage.Collections;
using VRage.Game.Components;
using VRage.Game.ObjectBuilders.Definitions;
using VRage.Game.ModAPI.Ingame;
using SpaceEngineers.Game.ModAPI.Ingame;

/*
 * Must be unique per each script project.
 * Prevents collisions of multiple `class Program` declarations.
 * Will be used to detect the ingame script region, whose name is the same.
 */
namespace DrillController
{

    /*
     * Do not change this declaration because this is the game requirement.
     */
    public sealed class Program : MyGridProgram
    {

        /*
         * Must be same as the namespace. Will be used for automatic script export.
         * The code inside this region is the ingame script.
         */
        #region DrillController

        IMyProgrammableBlock commonFunctions;
        IMyTimerBlock startDrillingCaller;
        IMyTimerBlock stopDrillingCaller;
        public Program()
        {
            commonFunctions = GridTerminalSystem.GetBlockWithName("[PG]CommonFunctions") as IMyProgrammableBlock;
            startDrillingCaller = GridTerminalSystem.GetBlockWithName("Start_Drilling_Caller") as IMyTimerBlock;
            stopDrillingCaller = GridTerminalSystem.GetBlockWithName("Stop_Drilling_Caller") as IMyTimerBlock;
        }

        public void Save() { }

        public void Main(string arg)
        {

            switch (arg)
            {
                case "getReady":
                    getReady();
                    break;
                case "getTransport":
                    getTransport();
                    break;
                case "start":
                    start();
                    break;
                case "stop":
                    stop(false);
                    break;
                case "stop:force":
                    stop(true);
                    break;
                case "status":
                    status();
                    break;
                default:
                    commonFunctions.TryRun("DrillController;log;ERROR;Invalid arg: " + arg);
                    return;
            }
        }

        // Puts the drill arm into the ready position.
        // In this position the drill is ready to start drilling at any moment, but the platform can still be moved.
        private void getReady()
        {
            setPistonGroupVelocity("GetReady", 0.05f);
            setPistonGroupLimits("GetReady", 4, 4);
        }

        // Returns the drill into its transport position.
        private void getTransport()
        {
            setPistonGroupVelocity("GetTransport", -0.05f);
            setPistonGroupLimits("GetTransport", 0, 0);

            // Make sure the drill head is in the "ready" positon
            setRotorVelocity("GetTransport", 0);
            setRotorLimits("GetTransport", 0, 1);

            // Ensure no other actions are running
            startDrillingCaller.StopCountdown();
            stopDrillingCaller.StopCountdown();
        }

        // Starts one drilling. Triggers a caller for returning to the ready position once done with this hole.
        // Needs to be in the "ready" position.
        private void start()
        {
            double totalPistonDistance = getPistonDistance();
            if (totalPistonDistance != 40.0)
            {
                string msg = "Pistons are in wrong positon. They need to be exactly at 100m. Where at " + totalPistonDistance;
                Echo(msg);
                commonFunctions.TryRun("StopDrilling;Log;INFO;" + msg);
                return;
            }

            setPistonGroupVelocity("Start", 0.01f);
            setPistonGroupLimits("Start", 4, 10);

            setRotorVelocity("Start", 4);
            setRotorLimits("Start", -361, 361);

            setDrillStatus(true);

            startDrillingCaller.StopCountdown();
            stopDrillingCaller.StartCountdown();
        }

        // Stops the drilling. If not forced (aborting the drill hole), it will not do anything until max depth is reached
        private void stop(Boolean force)
        {
            // TODO: Make depth configurable via CustomData of something
            var commonFunctions = GridTerminalSystem.GetBlockWithName("[PG]CommonFunctions") as IMyProgrammableBlock;

            if (!force)
            {
                double totalPistonDistance = getPistonDistance();
                if (totalPistonDistance != 100.0)
                {
                    string msg = "Pistons are in wrong positon. They need to be exactly at 100m. Where at " + totalPistonDistance;
                    Echo(msg);
                    commonFunctions.TryRun("StopDrilling;Log;INFO;" + msg);
                    return;
                }
            }

            setPistonGroupVelocity("Stop", -0.05f);
            setPistonGroupLimits("Stop", 4, 4);

            setRotorVelocity("Stop", 0);
            setRotorLimits("Stop", 0, 1);

            setDrillStatus(false);

            // Make sure we are no longer called
            stopDrillingCaller.StopCountdown();
        }

        private void status()
        {
            //TODO
        }

        private void setPistonGroupVelocity(string actionName, float velocity)
        {
            commonFunctions.TryRun(String.Format("DrillController->{0};setPistonGroupVelocity;DT Pistons;{1}", actionName, velocity));
        }

        private void setPistonGroupLimits(string actionName, float lowerLimit, float upperLimit)
        {
            commonFunctions.TryRun(String.Format("DrillController->{0};setPistonGroupLimits;DT Pistons;{1};{2}", actionName, lowerLimit, upperLimit));
        }

        private void setRotorVelocity(string actionName, float velocity)
        {
            commonFunctions.TryRun(String.Format("DrillController->{0};SetBlockVelocity;DT Advanced Rotor;{1}", actionName, velocity));
        }

        private void setRotorLimits(string actionName, float lowerLimit, float upperLimit)
        {
            commonFunctions.TryRun(String.Format("DrillController->{0};SetRotorLimit;DT Advanced Rotor;{1};{2}", actionName, lowerLimit, upperLimit));
        }

        private double getPistonDistance()
        {
            // Check that the pistons are extended 10m each
            IMyBlockGroup group = GridTerminalSystem.GetBlockGroupWithName("DT Pistons");
            List<IMyPistonBase> pistons = new List<IMyPistonBase>();
            group.GetBlocksOfType(pistons, piston => piston.Enabled);

            float completeDistance = 0f;
            foreach (var piston in pistons)
            {
                var pistonInf = piston.DetailedInfo;

                //splits the string into an array by separating by the ':' character
                string[] pistInfArr = (pistonInf.Split(':'));

                // splits the resulting 0.0m into an array with single index of "0.0" by again splitting by character "m"
                string[] pistonDist = (pistInfArr[1].Split('m'));

                //uses double.Parse method to parse the "0.0" into a usable double of value 0.0
                float pistonDistD = float.Parse(pistonDist[0]);

                completeDistance = completeDistance + pistonDistD;
            }
            return completeDistance;
        }

        private void setDrillStatus(bool on)
        {
            var group = GridTerminalSystem.GetBlockGroupWithName("DT Drills");
            List<IMyShipDrill> drills = new List<IMyShipDrill>();
            group.GetBlocksOfType(drills);
            foreach (var drill in drills)
            {
                drill.Enabled = on;
            }
        }

        #endregion
    }
}