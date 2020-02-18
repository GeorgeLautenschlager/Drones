using Sandbox.Game.EntityComponents;
using Sandbox.ModAPI.Ingame;
using Sandbox.ModAPI.Interfaces;
using SpaceEngineers.Game.ModAPI.Ingame;
using System.Collections.Generic;
using System.Collections;
using System.Linq;
using System.Text;
using System;
using VRage.Collections;
using VRage.Game.Components;
using VRage.Game.GUI.TextPanel;
using VRage.Game.ModAPI.Ingame.Utilities;
using VRage.Game.ModAPI.Ingame;
using VRage.Game.ObjectBuilders.Definitions;
using VRage.Game;
using VRage;
using VRageMath;

namespace IngameScript
{
    partial class Program : MyGridProgram
    {
        
        Drone Drone;
        public Program()
        {
            Drone = new Drone(this);
        }

        public void Main(string argument, UpdateType updateSource)
        {
            Echo($"{DateTime.Now.ToString()}");

            if (argument == null)
            {
                // No argument
            }
            else if (argument.StartsWith("callback"))
            {
                handleCallback(argument);
            }
            else
            {   
                string[] splitArgs = argument.Split(",");
                if (splitArgs == null || splitArgs.Count == 2)
                    throw new Exception("Expected two comma separated arguments!");

                Drone.NetworkService.DroneControllerEntityId = Convert.ToInt64(splitArgs[0]);

                // For now, assume one role
                Role role = Drone.roles[0];
                role.AcceptArgument(splitArgs[1]);
            }
           
            Drone.Perform();
        }

        public void handleCallback(string callback)
        {
            Echo($"Processing callback: {callback}");
            //TODO: this needs to be waaaaaay more general, obviously
            switch (callback)
            {
                case "callback_docking_request_pending":
                    Echo("Responding to docking request");
                    DroneController dc = Drone.roles[0] as DroneController;
                    dc.ProcessDockingRequest();
                    break;
                case "callback_docking_request_granted":
                    Echo("Docking clearance received.");
                    Miner m = Drone.roles[0] as Miner;
                    m.AcceptDockingClearance();
                    break;
            }
        }

        public void Save()
        {
            // Called when the program needs to save its state. Use
            // this method to save your state to the Storage field
            // or some other means. 
            // 
            // This method is optional and can be removed if not
            // needed.
        }
    }
}