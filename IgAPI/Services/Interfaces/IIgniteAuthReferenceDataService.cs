using IgAPI.DTOs.ReferenceSystem;

namespace IgAPI.Services.Interfaces;

/// <summary>Provides a read-only projection of the policy catalog owned by IgniteAuth.</summary>
public interface IIgniteAuthReferenceDataService
{
    Task<IgniteAuthReferenceDataDto> GetReferenceDataAsync(
        CancellationToken cancellationToken = default);
}
