using System;
using System.Collections.Generic;
using System.Text;

using IgniteAuth.Data;
using IgniteAuth.Processors.Validation;
using IgniteAuth.Results;

namespace IgniteAuth;

public static class Program
{
    public static void Main(string[] args)
    {
        while (true)
        {
            try
            {

                Console.WriteLine("------------IgniteAuth Engine------------");

                Console.WriteLine("Ready to get inputs");

                Console.WriteLine("Enter SubSystem name: ");
                string subSystem = Console.ReadLine();

                Console.WriteLine("Enter Operation, Resource");
                string operation = Console.ReadLine();
                string resource = Console.ReadLine();

                Console.WriteLine("Why are you doing this? Enter a reason: ");
                string intent = Console.ReadLine();


                // Verifiers

                var IntentDataJson = Path.Combine(AppContext.BaseDirectory, "Data", "IntentHashes.json");
                Console.WriteLine($"Loading: {IntentDataJson}");

                var CommandDataJson = Path.Combine(AppContext.BaseDirectory, "Data", "CommandData.json");
                Console.WriteLine($"Loading: {CommandDataJson}");

                var commandData = CommandData.LoadFromJson(CommandDataJson);
                var commandValidator = new CommandValidator(commandData);

                var intentHashes = IntentHashes.LoadFromJson(IntentDataJson);
                var intentValidator = new IntentValidator(intentHashes);

                var inputIntentHash = args.Length > 0 ? args[0] : "IntentHash1";
                var isValid = intentValidator.IsValidIntent(inputIntentHash);

                var inputCommand = args.Length > 1 ? args[1] : "CreateModel";
                var isCommandValid = commandValidator.IsValidCommand(inputCommand);

                Console.WriteLine($"Input hash: {inputIntentHash}");
                Console.WriteLine($"Is valid: {isValid}");
                Console.WriteLine($"Input command: {inputCommand}");
                Console.WriteLine($"Is command valid: {isCommandValid}");



            }
            catch (Exception ex)
            {
                Console.WriteLine("ERROR:");
                Console.WriteLine(ex.Message);
                Console.WriteLine(ex.StackTrace);

                Console.ReadKey();
            }
        }

        
    }
}