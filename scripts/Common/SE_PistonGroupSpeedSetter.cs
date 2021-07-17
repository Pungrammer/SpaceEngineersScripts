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
namespace PistonGroupSpeedSetter
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
        #region PistonGroupSpeedSetter

        string pistonGroupName;
        public Program()
        {
            pistonGroupName = "DT Pistons";
        }

        public void Save() { }

        public void Main(string arg, UpdateType updateSource)
        {
            float newSpeed = float.Parse(arg);

            IMyBlockGroup group = GridTerminalSystem.GetBlockGroupWithName(pistonGroupName);
            List<IMyPistonBase> pistons = new List<IMyPistonBase>();
            group.GetBlocksOfType(pistons, piston => piston.Enabled);

            if (pistons.Count == 0)
            {
                Echo("No pistons in group");
                return;
            }

            foreach (var piston in pistons)
            {
                piston.SetValue("Velocity", newSpeed);
                Echo(piston.DisplayName + " Speed Value: " + newSpeed);
            }
        }

        #endregion // Template
    }
}