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
namespace RotorLimit
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
        #region RotorLimit
    
        string rotorName;
        public Program()
        {
            rotorName = "DT Advanced Rotor";
        }
      
        public void Save() { }

        public void Main(string arg)
        {
            String[] args = arg.Split(';');
            if (args.Length != 2)
            {
                Echo("Invalid usage. Usage: <lower imit>;<uppder limit>");
                Echo("Example: 15;22");
            }

            float lowerLimit = float.Parse(args[0]);
            float upperLimit = float.Parse(args[1]);

            var rotor = GridTerminalSystem.GetBlockWithName(rotorName);
            rotor.SetValue("LowerLimit", lowerLimit);
            rotor.SetValue("UpperLimit", upperLimit);
            Echo("New limits: " + arg);
        }

        #endregion // Template
    }
}