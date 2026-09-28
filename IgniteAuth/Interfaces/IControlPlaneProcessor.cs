using System.Threading;
using System.Threading.Tasks;
using IgniteAuth.Results;

namespace IgniteAuth.Interfaces
{
    public interface IControlPlaneProcessor
    {
        Task<ControlPlaneResult> ProcessAsync(
            string authId,
            string subSystem,
            string intentHash,
            string command,
            string target,
            string rawDataJson,
            CancellationToken cancellationToken = default);
    }
}