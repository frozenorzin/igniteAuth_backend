using System.Threading;
using System.Threading.Tasks;
using IgniteAuth.Interfaces;
using IgniteAuth.Processors;

namespace IgniteAuth.Results
{
    public sealed class ControlPlaneDecision
    {
        private readonly IControlPlaneProcessor _controlPlaneProcessor;

        public ControlPlaneDecision(IControlPlaneProcessor controlPlaneProcessor)
        {
            _controlPlaneProcessor = controlPlaneProcessor;
        }

        public async Task<ControlPlaneResult> DecideAsync(
            IControlPlaneDecision decision,
            string rawDataJson,
            CancellationToken cancellationToken = default)
        {
            return await _controlPlaneProcessor.ProcessAsync(
                authId: decision.UserId,
                subSystem: decision.SubSystem,
                intentHash: decision.Intent,
                command: decision.Command,
                target: decision.SubSystemId,
                rawDataJson: rawDataJson,
                cancellationToken: cancellationToken);
        }
    }
}