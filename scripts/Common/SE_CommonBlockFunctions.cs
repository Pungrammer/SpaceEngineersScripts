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
namespace CommonBlockFunctions
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
        #region CommonBlockFunctions

        Dictionary<String, Function> availableFunctions;
        public Program()
        {
            this.availableFunctions = new Dictionary<string, Function>();
            addFunction(availableFunctions, new SetBlockVelocity(this));
            addFunction(availableFunctions, new SetRotorLimit(this));
            addFunction(availableFunctions, new SetPistonGroupVelocity(this));
            addFunction(availableFunctions, new SetPistonGroupLimits(this));
            addFunction(availableFunctions, new Log(this));
        }

        public void Save() { }

        /*
        *  Contains various functions which are useful and needed in most of my builds
        */
        public void Main(string arg, UpdateType updateSource)
        {
            String[] args = arg.Split(';');
            if (args.Length < 2)
            {
                Echo("Invalid usage. Expected usage: <caller>;<functionName>;<functionArgs>");
                return;
            }

            string caller = args[0];
            string functionName = args[1];

            Logger logger = new Logger(GridTerminalSystem.GetBlockWithName("DEBUGGER") as IMyTextPanel);
            logger.addToCallStack(caller);

            String[] functionArgs = new String[args.Length - 2];
            Array.Copy(args, 2, functionArgs, 0, args.Length - 2);
            logger.log(LogLevel.DEBUG, "Split args for function calls");

            if (!availableFunctions.ContainsKey(functionName.ToLower()))
            {
                logger.log(LogLevel.DEBUG, "Could not find function with name " + functionName);
                string possibleFunctions = "";
                foreach (var f in availableFunctions.Values)
                {
                    possibleFunctions = possibleFunctions + String.Format("    {0}: {1} | {2}\n", f.getName(), f.usage(), f.example());
                }
                string msg = String.Format("Unknown function: {0}\n    Possible functions and arguments:{1}\n", functionName, possibleFunctions);
                logger.log(LogLevel.ERROR, msg);
                throw new ExecutionException(msg);
            }

            try
            {
                logger.log(LogLevel.DEBUG, "Executing " + functionName);
                logger.addToCallStack(functionName);
                availableFunctions[functionName.ToLower()].execute(logger, functionArgs);
            }
            catch (Exception e)
            {
                logger.log(LogLevel.ERROR, e.ToString());
                throw e;
            }
        }

        private void addFunction(Dictionary<String, Function> availableFunctions, Function f)
        {
            availableFunctions.Add(f.getName().ToLower(), f);
        }

        private interface Function
        {
            string getName();
            string usage();
            string example();
            void execute(Logger logger, String[] args);
        }

        private class SetBlockVelocity : Function
        {
            Program myProgram;
            public SetBlockVelocity(Program myProgram)
            {
                this.myProgram = myProgram;
            }
            public string getName()
            {
                return "SetBlockVelocity";
            }

            public string usage()
            {
                return "<blockName>;<newVelocity>";
            }

            public string example()
            {
                return "My super rotor;4";
            }

            public void execute(Logger logger, String[] args)
            {
                if (args.Length != 2)
                {
                    throw myProgram.invalidUsageException(logger, this);
                }

                string blockName = args[0];
                float newVelocity = float.Parse(args[1]);

                var block = myProgram.GridTerminalSystem.GetBlockWithName(blockName);
                block.SetValueFloat("Velocity", newVelocity);
                logger.log(LogLevel.INFO, "Set " + blockName + " velocity to " + newVelocity);
            }
        }

        private class SetRotorLimit : Function
        {
            Program myProgram;
            public SetRotorLimit(Program myProgram)
            {
                this.myProgram = myProgram;
            }
            public string getName()
            {
                return "SetRotorLimit";
            }

            public string usage()
            {
                return "<rotorName>;<lower imit>;<uppder limit>";
            }

            public string example()
            {
                return "superRotor;15;22";
            }

            public void execute(Logger logger, String[] args)
            {
                if (args.Length != 3)
                {
                    throw myProgram.invalidUsageException(logger, this);
                }

                string blockName = args[0];
                float lowerLimit = float.Parse(args[1]);
                float upperLimit = float.Parse(args[2]);

                if (lowerLimit < -360)
                {
                    lowerLimit = float.MinValue;
                }
                if (upperLimit > 360)
                {
                    upperLimit = float.MaxValue;
                }

                var block = myProgram.GridTerminalSystem.GetBlockWithName(blockName) as IMyMotorStator;
                block.LowerLimitDeg = lowerLimit;
                block.UpperLimitDeg = upperLimit;
            }
        }

        private class SetPistonGroupVelocity : Function
        {
            Program myProgram;
            public SetPistonGroupVelocity(Program myProgram)
            {
                this.myProgram = myProgram;
            }
            public string getName()
            {
                return "SetPistonGroupVelocity";
            }

            public string usage()
            {
                return "<pistonGroupname>;<newSpeed>";
            }

            public string example()
            {
                return "My Pistons;3.5";
            }

            public void execute(Logger logger, String[] args)
            {
                if (args.Length != 2)
                {
                    throw myProgram.invalidUsageException(logger, this);
                }

                string groupName = args[0];
                float newSpeed = float.Parse(args[1]);

                logger.log(LogLevel.DEBUG, "Looking for group " + groupName);
                IMyBlockGroup group = myProgram.GridTerminalSystem.GetBlockGroupWithName(groupName);
                logger.log(LogLevel.DEBUG, "Found group");
                List<IMyPistonBase> pistons = new List<IMyPistonBase>();
                group.GetBlocksOfType(pistons, piston => piston.Enabled);
                logger.log(LogLevel.DEBUG, "Converted them to pistons");

                if (pistons.Count == 0)
                {
                    logger.log(LogLevel.ERROR, "No pistons in group " + groupName);
                    return;
                }

                foreach (var piston in pistons)
                {
                    piston.Velocity = newSpeed;
                }
                logger.log(LogLevel.INFO, groupName + " new speed: " + newSpeed);
            }
        }

        private class SetPistonGroupLimits : Function
        {
            Program myProgram;
            public SetPistonGroupLimits(Program myProgram)
            {
                this.myProgram = myProgram;
            }
            public string getName()
            {
                return "SetPistonGroupLimits";
            }

            public string usage()
            {
                return "<pistonGroupname>;<lowerLimit>;<upperLimit>";
            }

            public string example()
            {
                return "My Pistons;3.5;4.5";
            }

            public void execute(Logger logger, String[] args)
            {
                logger.log(LogLevel.DEBUG, String.Format("Executing {0}", getName()));
                if (args.Length != 3)
                {
                    throw myProgram.invalidUsageException(logger, this);
                }

                string groupName = args[0];
                float lowerLimit = float.Parse(args[1]);
                float upperLimit = float.Parse(args[2]);

                logger.log(LogLevel.DEBUG, "Looking for group " + groupName);
                IMyBlockGroup group = myProgram.GridTerminalSystem.GetBlockGroupWithName(groupName);
                logger.log(LogLevel.DEBUG, "Found group");
                List<IMyPistonBase> pistons = new List<IMyPistonBase>();
                group.GetBlocksOfType(pistons, piston => piston.Enabled);
                logger.log(LogLevel.DEBUG, "Converted them to pistons");

                if (pistons.Count == 0)
                {
                    logger.log(LogLevel.ERROR, "No pistons in group " + groupName);
                    return;
                }

                foreach (var piston in pistons)
                {
                    piston.MinLimit = lowerLimit;
                    piston.MaxLimit = upperLimit;
                }
                logger.log(LogLevel.INFO, groupName + " new limit: " + lowerLimit + "-" + upperLimit);
            }
        }

        private class Log : Function
        {
            Program myProgram;
            public Log(Program myProgram)
            {
                this.myProgram = myProgram;
            }
            public string getName()
            {
                return "Log";
            }

            public string usage()
            {
                return "<logLevel>;<logMessage>";
            }

            public string example()
            {
                return "ERROR;Something bad happened";
            }

            public void execute(Logger logger, String[] args)
            {
                if (args.Length != 2)
                {
                    throw myProgram.invalidUsageException(logger, this);
                }

                LogLevel logLevel = (LogLevel)Enum.Parse(typeof(LogLevel), args[0], true);
                string msg = args[1];

                logger.log(logLevel, msg);
            }
        }

        // TODO: Clear Log function

        // Internal methods:

        private ExecutionException invalidUsageException(Logger logger, Function f)
        {
            string err = String.Format("Wrong usage. Usage: {0}\nExample: {1}", f.usage(), f.example());
            return new ExecutionException(err);
        }



        public class ExecutionException : Exception
        {
            public ExecutionException(string message)
                : base(message)
            {
            }
        }

        public enum LogLevel
        {
            ERROR,
            INFO,
            DEBUG
        }

        public class Logger
        {

            IMyTextPanel debugTextPanel;
            List<String> callStack;
            public Logger(IMyTextPanel debugTextPanel)
            {
                this.debugTextPanel = debugTextPanel;
                this.callStack = new List<string>();
            }

            /*
            System.InvalidOperationException: Invalid property
               at Sandbox.ModAPI.Interfaces.TerminalPropertyExtensions.Cast[TValue](ITerminalProperty property)
               at Program.SetPistonGroupLimits.execute(Logger logger, String[] args)
               at Program.Main(String arg, UpdateType updateSource)
               */

            public void log(LogLevel logLevel, string message)
            {
                // TODO: Filter debug level if the debug is not set in CustomData
                string callers = "";
                foreach (var caller in callStack)
                {
                    callers = callers + "->" + caller;
                }
                string logLine = String.Format("{0}: [{1}]: {2}", callers, logLevel, message);

                string existingText = debugTextPanel.GetText();
                debugTextPanel.WriteText(logLine + "\n" + existingText, false);
            }

            public void addToCallStack(string nextCaller)
            {
                callStack.Add(nextCaller);
            }
        }



        #endregion
    }
}