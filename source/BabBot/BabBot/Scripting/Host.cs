﻿/*
    This file is part of BabBot.

    BabBot is free software: you can redistribute it and/or modify
    it under the terms of the GNU General Public License as published by
    the Free Software Foundation, either version 3 of the License, or
    (at your option) any later version.

    BabBot is distributed in the hope that it will be useful,
    but WITHOUT ANY WARRANTY; without even the implied warranty of
    MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
    GNU General Public License for more details.

    You should have received a copy of the GNU General Public License
    along with BabBot.  If not, see <http://www.gnu.org/licenses/>.
  
    Copyright 2009 BabBot Team
*/
using System;
using System.IO;
using System.Windows.Forms;
using BabBot.Manager;
using CSScriptLibrary;
using System.Reflection;
using BabBot.Wow;
using BabBot.States;

namespace BabBot.Scripting
{
    public class Host
    {
        private States.State<Wow.WowPlayer> script;

        public void Start(string iScript)
        {
            //script = Load("Scripts/PatTestScript.cs");
            script = Load(iScript);
            ProcessManager.Player.StateMachine.SetGlobalState(script);
        }

        private States.State<Wow.WowPlayer> Load(string iScript)
        {
            //variable to hold output
            State<WowPlayer> state = null;

            //true to share host assemblies
            CSScript.ShareHostRefAssemblies = true;
            // do not cache the scripts
            CSScript.CacheEnabled = false;

            Assembly asm = CSScript.Load(Path.GetFullPath(iScript), Path.GetTempFileName(), false);
            
            //get all types in assembly
            Type[] types = asm.GetTypes();

            //search through the included types
            foreach (Type type in types)
            {
                // and try and find a state<wowplayer> type
                if (type.IsClass && type.IsSubclassOf(typeof(State<WowPlayer>)))
                {
                    // Create the State using the Activator class.
                    state = (State<WowPlayer>)Activator.CreateInstance(type, true);

                    break;
                }
            }

            return state;
        }
    }
}