using System;
using System.Collections.Generic;
using System.Text;


using IgniteAuth.Results;
using IgniteAuth.Interfaces;
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

                Decision = ControlPlaneDecision.deny,
                ReasonCode = "DENY_DEFAULT",
                Message = "Request denied by default policy."

            };

            // Data schema validation
            if (string.IsNullOrWhiteSpace(rawDataJson))
                return Task.FromResult(deny with
                {
                    ReasonCode = "SCHEMA_INVALID",
                    Message = "Raw payload is empty."
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




            return Task.FromResult(deny with
            {
                ReasonCode = "POLICY_NOT_LOADED",
                Message = "Policy store not initialized."
            });
        }
    }

}

