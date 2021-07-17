using System;

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
namespace RotorSpeedSetter
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
        #region RotorSpeedSetter

        public Program() { }

        public void Save() { }

        /*
        *  Sets the Velocity of the given block. Use it for Rotors, Hinges or Pistons
        */
        public void Main(string arg, UpdateType updateSource)
        {
            String[] args = arg.Split(';');
            if (args.Length != 2)
            {
                Echo("Wrong usage. Usage: <rotorName>;<newSpeed>");
                Echo("Example: My Super Rotor;4");
            }

            string blockName = args[0];
            float newSpeed = float.Parse(args[1]);

            var block = GridTerminalSystem.GetBlockWithName(blockName);
            block.SetValueFloat("Velocity", newSpeed);
        }

        #endregion // Template
    }
}