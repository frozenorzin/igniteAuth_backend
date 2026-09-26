using IgAPI.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Reflection.PortableExecutable;

namespace IgAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class ReferenceSystemController : ControllerBase
    {
        [HttpGet]
        public IActionResult GetReferenceSystem()
        {
            var referenceSystem = new ReferenceSystem
            {
                ReferenceSystemId = 1,
                SystemName = "IgniteAuth Reference System",
                SystemType = "Control Plane Security Framework",
                SystemVersion = "v0.9",
                Description = "Reference system containing subsystems, policies, intents, commands, and targets.",
                Status = "Active",
                CreatedAt = DateTime.UtcNow,

                Subsystems =
                [
                    new SubSystem
                    {
                        SubSystemId = 1,
                        ReferenceSystemId = 1,
                        Name = "Embedded-X",
                        Domain = "Defense Airborne",
                        Description = "Embedded subsystem for validating aircraft threat-related operations.",
                        Status = "Active"
                    },
                    new SubSystem
                    {
                        SubSystemId = 2,
                        ReferenceSystemId = 1,
                        Name = "Cloud Compute Engine",
                        Domain = "Cloud Infrastructure",
                        Description = "Cloud subsystem for provisioning and scaling compute resources.",
                        Status = "Active"
                    }
                ],

                Policies =
                [
                    new Policy
                    {
                        PolicyId = 1,
                        ReferenceSystemId = 1,
                        PolicyName = "Policy_Embedded_Defense",
                        PolicyType = "DenyByDefault",
                        AllowedIntents =
                        [
                            "DetectEnemyAircraft",
                            "ValidateThreatPattern"
                        ],
                        AllowedCommands =
                        [
                            "SCAN_RADAR_PATTERN",
                            "LOCK_TARGET_PROFILE"
                        ],
                        AllowedTargets =
                        [
                            "AircraftSignatureDB",
                            "RadarModule"
                        ]
                    },
                    new Policy
                    {
                        PolicyId = 2,
                        ReferenceSystemId = 1,
                        PolicyName = "Policy_Cloud_Provisioning",
                        PolicyType = "IntentCommandTargetMapping",
                        AllowedIntents =
                        [
                            "ProvisionCompute",
                            "ScaleResources"
                        ],
                        AllowedCommands =
                        [
                            "CREATE_VM",
                            "ALLOCATE_STORAGE",
                            "SCALE_NODE"
                        ],
                        AllowedTargets =
                        [
                            "ComputeCluster",
                            "StoragePool",
                            "NetworkZone"
                        ]
                    }
                ]
            };

            return Ok(referenceSystem);
        }
    }
}