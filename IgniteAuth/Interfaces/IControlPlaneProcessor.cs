using System;
using System.Collections.Generic;
using System.Text;

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
            CancellationToken cancellationToken = default

          );
    }
}
