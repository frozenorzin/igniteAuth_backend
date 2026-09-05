using System;
using System.Collections.Generic;
using System.Text;

using IgniteAuth.Interfaces;


namespace IgniteAuth.Processors.Validation
{
    public sealed class IntentCommandMapper
    
    {
        

        bool IsValidMapping(string intentHash, string command, string subsystem)
        {
            // verify Intent from valid intents list of sub system and verify command is valid for that intent

            // 1. Identify sub system
            // 2. Fetch valid intents for that sub system
            // 3. Fetch valid commands for that sub system
            // 4. Verify if intent is valid for current command and subsystem



            // procedure

            // from json, check for subsystem
            // if subsystem is found : proceed

            // go through intent-Command mapping json file  





            // deny at any point if any of the above fails
            return false;

        }



    }


}
