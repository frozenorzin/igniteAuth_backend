using System;
using System.IO;
using System.Text;
using IgniteAuth.Data;
using IgniteAuth.Processors.Validation;



namespace IgniteAuth;

public static class Program
{
    public static void Main(string[] args)
    {

        // Get secret key from environment variable or use a default (for testing only!)
        string? secretKeyEnv = Environment.GetEnvironmentVariable("IGNITE_AUTH_SECRET");
        byte[] secretKey = string.IsNullOrEmpty(secretKeyEnv)
            ? Encoding.UTF8.GetBytes("your-secret-key-change-this")  // ⚠️ Change this or use env var!
            : Encoding.UTF8.GetBytes(secretKeyEnv);

        // Convert plain intents to hashed intents (run once)
        Console.WriteLine("========================================");
        Console.WriteLine("Converting plain intents to hashed...");
        Console.WriteLine("========================================\n");

        string intentDataJsonPath =
            Path.Combine(
                AppContext.BaseDirectory,
                "Data",
                "IntentHashes.json");

        string hashedIntentDataJsonPath =
            Path.Combine(
                AppContext.BaseDirectory,
                "..",
                "..",
                "..",
                "Data",
                "IntentHashes.hashed.json");

        hashedIntentDataJsonPath = Path.GetFullPath(hashedIntentDataJsonPath);

        string mappingDataJsonPath =
            Path.Combine(
                AppContext.BaseDirectory,
                "Data",
                "IntentCommandMapping.json");

        string hashedMappingDataJsonPath =
            Path.Combine(
                AppContext.BaseDirectory,
                "..",
                "..",
                "..",
                "Data",
                "IntentCommandMapping.hashed.json");

        hashedMappingDataJsonPath = Path.GetFullPath(hashedMappingDataJsonPath);

        try
        {
            IntentHashes.ConvertPlainIntentToHashedJson(
                intentDataJsonPath,
                hashedIntentDataJsonPath,
                secretKey);

            Console.WriteLine($"✓ Intent hashes conversion complete!");
            Console.WriteLine($"  Output: {hashedIntentDataJsonPath}\n");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"✗ Intent hashes conversion failed: {ex.Message}\n");
        }

        try
        {
            IntentCommandMapping.ConvertPlainIntentToHashedJson(
                mappingDataJsonPath,
                hashedMappingDataJsonPath,
                secretKey);

            Console.WriteLine($"✓ Intent command mapping conversion complete!");
            Console.WriteLine($"  Output: {hashedMappingDataJsonPath}\n");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"✗ Intent command mapping conversion failed: {ex.Message}\n");
        }


        // Main engine loop for processing requests
        while (true)
        {
            try
            {
                Console.WriteLine("------------ IgniteAuth Engine ------------");
                Console.WriteLine("Ready to get inputs\n");

                // -----------------------------
                // 1. Collect Request
                // -----------------------------

                Console.Write("Enter SubSystem name: ");
                string subSystem = Console.ReadLine() ?? string.Empty;

                Console.Write("Enter Operation: ");
                string operation = Console.ReadLine() ?? string.Empty;

               // Console.Write("Enter Resource: ");
               // string resource = Console.ReadLine() ?? string.Empty;

                Console.Write("Why are you doing this? Enter a reason: ");
                string intent = Console.ReadLine() ?? string.Empty;

                Console.WriteLine("\n------------ Request ------------");

                Console.WriteLine($"Subsystem : {subSystem}");
                Console.WriteLine($"Operation : {operation}");
                //Console.WriteLine($"Resource  : {resource}");
                Console.WriteLine($"Intent    : {intent}");

                // -----------------------------
                // 2. Load Policy Data
                // -----------------------------

                // Load from source folder where hashed files were written
                string intentDataJson =
                    Path.Combine(
                        AppContext.BaseDirectory,
                        "..",
                        "..",
                        "..",
                        "Data",
                        "IntentHashes.hashed.json");

                intentDataJson = Path.GetFullPath(intentDataJson);

                string commandDataJson =
                    Path.Combine(
                        AppContext.BaseDirectory,
                        "..",
                        "..",
                        "..",
                        "Data",
                        "CommandData.json");

                commandDataJson = Path.GetFullPath(commandDataJson);

                string mappingDataJson =
                    Path.Combine(
                        AppContext.BaseDirectory,
                        "..",
                        "..",
                        "..",
                        "Data",
                        "IntentCommandMapping.hashed.json");

                mappingDataJson = Path.GetFullPath(mappingDataJson);

                Console.WriteLine("\n------------ Loading Policy ------------");

                Console.WriteLine($"Intent data  : {intentDataJson}");
                Console.WriteLine($"Command data : {commandDataJson}");
                Console.WriteLine($"Mapping data : {mappingDataJson}");

                var commandData =
                    CommandData.LoadFromJson(commandDataJson);

                var intentHashes =
                    IntentHashes.LoadFromJson(intentDataJson);

                var mappingData =
                    IntentCommandMapping.LoadFromJson(mappingDataJson);

                // -----------------------------
                // 3. Create Validators
                // -----------------------------

                var commandValidator =
                    new CommandValidator(commandData);

                var intentValidator =
                    new IntentValidator(intentHashes, secretKey);

                var intentCommandMapper =
                    new IntentCommandMapper(mappingData, secretKey);

                // -----------------------------
                // 4. Prepare Request
                // -----------------------------

                string inputCommand = operation;

                // The user enters a plain intent string.
                // The validator will hash it internally using the same secret key
                // and compare against stored hashes.
                string inputPlainIntent = intent;

                // -----------------------------
                // 5. Validate Intent
                // -----------------------------

                bool isIntentValid =
                    intentValidator.IsValidIntent(
                        subSystem,
                        inputPlainIntent);

                // -----------------------------
                // 6. Validate Command
                // -----------------------------

                bool isCommandValid =
                    commandValidator.IsValidCommand(
                        subSystem,
                        inputCommand);

                // -----------------------------
                // 7. Validate Intent → Command
                // -----------------------------

                bool isMappingValid =
                    intentCommandMapper.IsValidMapping(
                        subSystem,
                        inputPlainIntent,
                        inputCommand);

                // -----------------------------
                // 8. Validation Result
                // -----------------------------

                Console.WriteLine("\n------------ Validation Results ------------");

                Console.WriteLine($"Subsystem         : {subSystem}");

                Console.WriteLine("\n---- Intent Validation ----");
                Console.WriteLine($"Entered Intent    : {inputPlainIntent}");
                Console.WriteLine($"Valid             : {isIntentValid}");

                Console.WriteLine("\n---- Command Validation ----");
                Console.WriteLine($"Command           : {inputCommand}");
                Console.WriteLine($"Valid             : {isCommandValid}");

                Console.WriteLine("\n---- Intent↔Command Mapping ----");
                Console.WriteLine($"Mapping Valid     : {isMappingValid}");

                // -----------------------------
                // 9. Authorization Decision
                // -----------------------------

                bool authorized =
                    isIntentValid &&
                    isCommandValid &&
                    isMappingValid;

                Console.WriteLine("\n------------ Decision ------------");

                if (authorized)
                {
                    Console.WriteLine("DECISION: ALLOW");
                }
                else
                {
                    Console.WriteLine("DECISION: DENY");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("\nERROR:");
                Console.WriteLine(ex.Message);
                Console.WriteLine(ex.StackTrace);
            }
        }
    }
}