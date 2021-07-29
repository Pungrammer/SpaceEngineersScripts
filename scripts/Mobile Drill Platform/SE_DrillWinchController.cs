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
namespace DrillWinchController
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
        #region DrillWinchController

        IMyProgrammableBlock commonFunctions;

        List<IMyMotorAdvancedStator> hinges;
        List<IMyShipDrill> drills;
        IMyMotorAdvancedStator drillWinch;

        IMySensorBlock sensorFullyRolledUp;
        IMySensorBlock sensorFullyRolledDown;
        IMySensorBlock sensorDrillReady;
        public Program()
        {
            commonFunctions = GridTerminalSystem.GetBlockWithName("[PG]CommonFunctions") as IMyProgrammableBlock;
            if (commonFunctions == null)
            {
                Echo("Needs a block with common functions called \"[PG]CommonFunctions\"");
                return;
            }

            IMyBlockGroup group = GridTerminalSystem.GetBlockGroupWithName("Drill Hinges");
            if (group == null)
            {
                commonFunctions.TryRun("DrillWinchController->Constructor;log;ERROR;Missing 'Drill Hinges' group");
                return;
            }
            hinges = new List<IMyMotorAdvancedStator>();
            group.GetBlocksOfType(hinges, hinge => hinge.Enabled);

            group = GridTerminalSystem.GetBlockGroupWithName("Drills");
            if (group == null)
            {
                commonFunctions.TryRun("DrillWinchController->Constructor;log;ERROR;Missing 'Drills' group");
                return;
            }
            drills = new List<IMyShipDrill>();
            group.GetBlocksOfType(drills);

            drillWinch = GridTerminalSystem.GetBlockWithName("Drill Winch") as IMyMotorAdvancedStator;
            if (drillWinch == null)
            {
                commonFunctions.TryRun("DrillWinchController->Constructor;log;ERROR;Missing 'Drill Winch'");
                return;
            }

            // Check that the sensors exist
            sensorFullyRolledUp = GridTerminalSystem.GetBlockWithName("Sensor Fully Rolled Up") as IMySensorBlock;
            if (sensorFullyRolledUp == null)
            {
                commonFunctions.TryRun("DrillWinchController->Constructor;log;ERROR;Missing 'Sensor Fully Rolled Up'");
                return;
            }

            sensorFullyRolledDown = GridTerminalSystem.GetBlockWithName("Sensor Fully Rolled Down") as IMySensorBlock;
            if (sensorFullyRolledDown == null)
            {
                commonFunctions.TryRun("DrillWinchController->Constructor;log;ERROR;Missing 'Sensor Fully Rolled Down'");
                return;
            }

            sensorDrillReady = GridTerminalSystem.GetBlockWithName("Sensor Drill Ready") as IMySensorBlock;
            if (sensorDrillReady == null)
            {
                commonFunctions.TryRun("DrillWinchController->Constructor;log;ERROR;Missing 'Sensor Drill Ready'");
                return;
            }
        }

        public void Save() { }

        public void Main(string arg)
        {

            switch (arg)
            {
                case "stop":
                    stop();
                    break;
                case "readyPositionReached":
                    readyPositionReached();
                    break;
                case "start":
                    start();
                    break;
                case "fullyRolledDown":
                    stop();
                    break;
                case "fullyRolledUp":
                    fullyRolledUp();
                    break;
                default:
                    commonFunctions.TryRun("DrillWinchController;log;ERROR;Invalid arg: " + arg);
                    return;
            }
        }

        // Stops all drilling operations and rolls the drill rope back up.
        private void stop()
        {
            setDrillStatus(false);
            setDrillRotorRPM("stop", -0.5f);
            setDrillWinchLock(false);

            setHingeLock(false);

            sensorFullyRolledUp.Enabled = true;
            sensorFullyRolledDown.Enabled = false;
            sensorDrillReady.Enabled = false;
        }

        // Signals the controller, that the ready position is reached and activates the drilling process
        // Must not be called while rolling the rope UP
        private void readyPositionReached()
        {
            setDrillStatus(true);
            setDrillRotorRPM("readyPositionReached", 0.5f);
            setDrillWinchLock(false);

            setHingeLock(false);

            sensorFullyRolledDown.Enabled = true;
            sensorFullyRolledUp.Enabled = false;
            sensorDrillReady.Enabled = false;
        }

        private void start()
        {
            setDrillStatus(false);
            setDrillRotorRPM("start", 0.5f);
            setDrillWinchLock(false);

            setHingeLock(false);

            sensorFullyRolledDown.Enabled = true;
            sensorFullyRolledUp.Enabled = false;
            sensorDrillReady.Enabled = true;
        }

        private void fullyRolledUp()
        {
            setDrillStatus(false); // Just to make sure
            setDrillRotorRPM("fullyRolledUp", 0f);
            setDrillWinchLock(true);

            setHingeLock(true);

            sensorFullyRolledDown.Enabled = false;
            sensorFullyRolledUp.Enabled = false;
            sensorDrillReady.Enabled = false;
        }

        private void setHingeLock(bool on)
        {
            foreach (var hinge in hinges)
            {
                hinge.RotorLock = on;
            }
        }

        private void setDrillRotorRPM(string actionName, float rpm)
        {
            commonFunctions.TryRun(String.Format("DrillController->{0};SetBlockVelocity;{1};{2}", actionName, drillWinch.Name, rpm));
            commonFunctions.TryRun(String.Format("DrillController->{0};SetRotorLimit;{1};{2};{3}", actionName, drillWinch.Name, -361, 361));
        }

        private void setDrillWinchLock(bool on)
        {
            drillWinch.RotorLock = on;
        }

        private void setDrillStatus(bool on)
        {
            foreach (var drill in drills)
            {
                drill.Enabled = on;
            }
        }

        #endregion
    }
}