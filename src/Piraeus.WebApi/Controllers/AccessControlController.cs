using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Orleans;
using Piraeus.Core.Logging;
using Piraeus.Grains;
using AuthorizationPolicy = Capl.Authorization.AuthorizationPolicy;

namespace Piraeus.WebApi.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AccessControlController : ControllerBase
    {
        private readonly GraphManager graphManager;

        private readonly ILogger logger;

        public AccessControlController(IClusterClient clusterClient, Logger logger = null)
        {
            if (!GraphManager.IsInitialized)
            {
                graphManager = GraphManager.Create(clusterClient);
            }
            else
            {
                graphManager = GraphManager.Instance;
            }

            this.logger = logger;
        }

        [HttpDelete("DeleteAccessControlPolicy")]
        [Authorize]
        public async Task<IActionResult> DeleteAccessControlPolicy(string policyUriString)
        {
            try
            {
                _ = policyUriString ?? throw new ArgumentNullException(nameof(policyUriString));

                await graphManager.ClearAccessControlPolicyAsync(policyUriString);
                return StatusCode(200);
            }
            catch (Exception ex)
            {
                logger?.LogError(ex, "Error deleting CAPL policy.");
                return StatusCode(500, ex.Message);
            }
        }

        [HttpGet("GetAccessControlPolicy")]
        [Authorize]
        [Produces("application/xml")]
        public async Task<ActionResult<AuthorizationPolicy>> GetAccessControlPolicy(string policyUriString)
        {
            try
            {
                _ = policyUriString ?? throw new ArgumentNullException(nameof(policyUriString));

                AuthorizationPolicy policy = await graphManager.GetAccessControlPolicyAsync(policyUriString);
                return StatusCode(200, policy);
            }
            catch (Exception ex)
            {
                logger?.LogError(ex, "Error getting CAPL policy.");
                return StatusCode(500, ex.Message);
            }
        }

        [HttpPut("UpsertAccessControlPolicy")]
        [Authorize]
        public async Task<IActionResult> UpsertAccessControlPolicy(AuthorizationPolicy policy)
        {
            try
            {
                _ = policy ?? throw new ArgumentNullException(nameof(policy));

                await graphManager.UpsertAcessControlPolicyAsync(policy.PolicyId.ToString(), policy);
                return StatusCode(200);
            }
            catch (Exception ex)
            {
                logger?.LogError(ex, "Error upserting CAPL policy.");
                return StatusCode(500, ex.Message);
            }
        }
    }
}