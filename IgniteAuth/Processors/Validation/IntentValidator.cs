using System;
using System.Collections.Generic;
using System.Text;
using IgniteAuth.Interfaces;
using IgniteAuth.Data;

namespace IgniteAuth.Processors.Validation
{
    public  class IntentValidator : IIntentValidator
    {
        private readonly HashSet<string> _validIntentHashes;
        public IntentValidator(IntentHashes intentHashes)
        {
            _validIntentHashes = intentHashes.GetAllIntentHashes();
        }

        public bool IsValidIntent(string intentHash)
        {
            return !string.IsNullOrWhiteSpace(intentHash) && _validIntentHashes.Contains(intentHash);
        }

    }
}
