using System;
using IgniteAuth.Results;
using IgniteAuth.Interfaces;

using System.Threading;
using System.Threading.Tasks;
using System.Text.Json;
/*
The main purpose of this Processors is 
      1. To capture the CPC request from the system
      2. To Normalize the request with respect to Control plane policy
      3. To send normalized request, to the Policy Enforcement Engine (PEE) for evaluation
      4. Have a response pattern which is in the Domains, that is response needed to system 
      5. Run the three validation checks, Intent validation, Command validation and Intent-Command mapping validation
*/



namespace IgniteAuth.Processors
{
    public sealed class ControlPlaneProcessor : IControlPlaneProcessor

    {

        private readonly IIntentValidator _intentValidator;
        private readonly ICommandValidator _commandValidator;
        private readonly IIntentCommandMapper _intentCommandMapper;


        public ControlPlaneProcessor(

               IIntentValidator intentValidator,
               ICommandValidator commandValidator,
               IIntentCommandMapper intentCommandMapper)

        {
            _intentValidator = intentValidator;
            _commandValidator = commandValidator;
            _intentCommandMapper = intentCommandMapper;
        }

        public Task<ControlPlaneResult> ProcessAsync(
            string authId,
            string subSystem,
            string intentHash,
            string command,
            string target,
            string rawDataJson,
            CancellationToken cancellationToken = default)
        {

            var deny = new ControlPlaneResult
            {

                Decision = ControlPlaneDecisionVerdict.Deny,
                ReasonCode = "DENY_DEFAULT",
                Message = "Request denied by default policy."

            };

            cancellationToken.ThrowIfCancellationRequested();

            // Require a JSON object and an authenticated caller.
            if (!MatchesPayload(rawDataJson, authId, subSystem, intentHash, command, target) || string.IsNullOrWhiteSpace(authId))
                return Task.FromResult(deny with
                {
                    ReasonCode = "SCHEMA_INVALID",
                    Message = "The request payload must match the decision fields and include an authenticated caller."
                });

            // The reference catalog identifies each target by its subsystem name.
            if (string.IsNullOrWhiteSpace(target) ||
                !string.Equals(target, subSystem, StringComparison.Ordinal))
                return Task.FromResult(deny with
                {
                    ReasonCode = "TARGET_INVALID",
                    Message = "Target must match the registered subsystem identifier."
                });

            // Intent and command validation

            if (!_intentValidator.IsValidIntent(subSystem, intentHash))
                return Task.FromResult(deny with
                {
                    ReasonCode = "INTENT_INVALID",
                    Message = "Intent hash is missing or invalid."
                });

            if (!_commandValidator.IsValidCommand(subSystem, command))
                return Task.FromResult(deny with
                {
                    ReasonCode = "COMMAND_INVALID",
                    Message = "Command is missing or invalid."
                });

            // intent - command mapping validation

            if (!_intentCommandMapper.IsValidMapping(subSystem, intentHash, command))
                return Task.FromResult(deny with
                {
                    ReasonCode = "ICT_MAPPING_FAILED",
                    Message = "Intent-command mapping failed."
                });




            return Task.FromResult(new ControlPlaneResult
            {
                Decision = ControlPlaneDecisionVerdict.Allow,
                ReasonCode = "ALLOW_VALIDATED",
                Message = "Intent, command, and intent-command mapping validated."
            });
        }

        private static bool MatchesPayload(string rawDataJson, string authId, string subSystem,
            string intent, string command, string target)
        {
            if (string.IsNullOrWhiteSpace(rawDataJson)) return false;
            try
            {
                using var document = JsonDocument.Parse(rawDataJson);
                if (document.RootElement.ValueKind != JsonValueKind.Object) return false;
                var fields = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
                var propertyNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                foreach (var property in document.RootElement.EnumerateObject())
                {
                    if (!propertyNames.Add(property.Name)) return false;
                    if (property.Value.ValueKind == JsonValueKind.String)
                        fields.Add(property.Name, property.Value.GetString() ?? string.Empty);
                }
                return fields.TryGetValue("UserId", out var user) && user == authId &&
                    fields.TryGetValue("SubSystem", out var subsystemValue) && subsystemValue == subSystem &&
                    fields.TryGetValue("Intent", out var intentValue) && intentValue == intent &&
                    fields.TryGetValue("Command", out var commandValue) && commandValue == command &&
                    fields.TryGetValue("SubSystemId", out var targetValue) && targetValue == target;
            }
            catch (JsonException)
            {
                return false;
            }
        }
    }
}
