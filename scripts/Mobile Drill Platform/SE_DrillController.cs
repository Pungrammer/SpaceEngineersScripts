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
        List<IMyExtendedPistonBase> pistons;

        float maxDepth;
        float readyPositon;
        float readyPositionPerPiston;
        float drillSpeed; // 0.1m/s divided by count of pistons
        float positioningSpeed; // 0.5m/s divided by count of pistons
        public Program()
        {
            commonFunctions = GridTerminalSystem.GetBlockWithName("[PG]CommonFunctions") as IMyProgrammableBlock;
            if (commonFunctions == null)
            {
                Echo("Needs a block with common functions called \"[PG]CommonFunctions\"");
                return;
            }
            startDrillingCaller = GridTerminalSystem.GetBlockWithName("Start_Drilling_Caller") as IMyTimerBlock;
            if (startDrillingCaller == null)
            {
                commonFunctions.TryRun("DrillController->Constructor;log;ERROR;Missing 'Start_Drilling_Caller' timer block");
                return;
            }
            stopDrillingCaller = GridTerminalSystem.GetBlockWithName("Stop_Drilling_Caller") as IMyTimerBlock;
            if (stopDrillingCaller == null)
            {
                commonFunctions.TryRun("DrillController->Constructor;log;ERROR;Missing 'Stop_Drilling_Caller' timer block");
                return;
            }

            IMyBlockGroup group = GridTerminalSystem.GetBlockGroupWithName("DT Pistons");
            if (group == null)
            {
                commonFunctions.TryRun("DrillController->Constructor;log;ERROR;Missing 'DT Pistons' piston group");
                return;
            }
            pistons = new List<IMyExtendedPistonBase>();
            group.GetBlocksOfType(pistons, piston => piston.Enabled);

            // Each piston can extend 10m
            maxDepth = 10 * pistons.Count;

            // Ready position is 10m deep. So each piston needs to be extended by a fraction of it.
            // Pistons only support precision up to 4 places after the comma (0.1234m)
            readyPositionPerPiston = (float)Math.Round(10f / pistons.Count, 1);
            readyPositon = (float)Math.Round(readyPositionPerPiston * pistons.Count, 1);

            drillSpeed = 0.5f / pistons.Count;
            positioningSpeed = 1f / pistons.Count;

            string msg = string.Format("\n"
            + "  maxDepth              : {0}\n"
            + "  readyPosition         : {1}\n"
            + "  readyPositionPerPiston: {2}\n"
            + "  drillSpeed            : {3}\n"
            + "  positioningSpeed      : {4}\n"
            + "  pistonCount           : {5}\n",
            maxDepth, readyPositon, readyPositionPerPiston, drillSpeed, positioningSpeed, pistons.Count
            );
            commonFunctions.TryRun("DrillController->Constructor;log;DEBUG;New controller:" + msg);
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
            float newPistonVelocity;
            if (getPistonDistance(pistons) < readyPositionPerPiston)
            {
                // Extend pistons if they are above the ready positon
                newPistonVelocity = positioningSpeed;
            }
            else
            {
                // Retract the pistons if they are under the ready positon
                newPistonVelocity = -positioningSpeed;
            }
            setPistonGroupVelocity("GetReady", newPistonVelocity);
            setPistonGroupLimits("GetReady", readyPositionPerPiston, readyPositionPerPiston);

            setRotorVelocity("GetTransport", 1);
            setRotorLimits("GetTransport", 0, 1);

            // TODO: Disable merge block
        }

        // Returns the drill into its transport position.
        private void getTransport()
        {
            setPistonGroupVelocity("GetTransport", -positioningSpeed);
            setPistonGroupLimits("GetTransport", 0, 0);

            // Make sure the drill head is in the "ready" positon
            setRotorVelocity("GetTransport", 1);
            setRotorLimits("GetTransport", 0, 1);

            //TODO: Remove this echo
            Echo(startDrillingCaller.ToString());
            Echo(stopDrillingCaller.ToString());

            // Ensure no other actions are running
            startDrillingCaller.StopCountdown();
            stopDrillingCaller.StopCountdown();
        }

        // Starts one drilling. Triggers a caller for returning to the ready position once done with this hole.
        // Needs to be in the "ready" position.
        private void start()
        {
            double totalPistonDistance = getPistonDistance(pistons);
            if (Math.Round(totalPistonDistance, 1) != readyPositon)
            {
                string msg = String.Format("Pistons are in wrong positon. They need to be exactly at {0}m. Where at {1}", readyPositon, totalPistonDistance);
                Echo(msg);
                commonFunctions.TryRun("DrillController->start;Log;INFO;" + msg);
                return;
            }

            setPistonGroupVelocity("start", drillSpeed);
            setPistonGroupLimits("start", readyPositionPerPiston, maxDepth);

            setRotorVelocity("start", 4);
            setRotorLimits("start", -361, 361);

            setDrillStatus(true);

            startDrillingCaller.StopCountdown();
            stopDrillingCaller.StartCountdown();
        }

        // Stops the drilling. If not forced (aborting the drill hole), it will not do anything until max depth is reached
        private void stop(Boolean force)
        {
            // TODO: Make depth configurable via CustomData
            if (!force)
            {
                double totalPistonDistance = getPistonDistance(pistons);
                if (totalPistonDistance != maxDepth)
                {
                    string msg = String.Format("Pistons are in wrong positon. They need to be exactly at {0}m. Where at {1}", maxDepth, totalPistonDistance);
                    Echo(msg);
                    commonFunctions.TryRun("DrillController->stop;Log;INFO;" + msg);
                    return;
                }
            }

            setPistonGroupVelocity("stop", -positioningSpeed);
            setPistonGroupLimits("stop", readyPositionPerPiston, readyPositionPerPiston);

            setRotorVelocity("stop", 1);
            setRotorLimits("stop", 0, 1);

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

        private double getPistonDistance(List<IMyExtendedPistonBase> pistons)
        {
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