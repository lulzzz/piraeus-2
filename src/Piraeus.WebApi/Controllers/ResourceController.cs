using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Orleans;
using Piraeus.Core.Logging;
using Piraeus.Core.Messaging;
using Piraeus.Core.Metadata;
using Piraeus.GrainInterfaces;
using Piraeus.Grains;

namespace Piraeus.WebApi.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ResourceController : ControllerBase
    {
        private readonly GraphManager graphManager;

        private readonly ILogger logger;

        public ResourceController(IClusterClient clusterClient, Logger logger = null)
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

        [HttpDelete("DeletePiSystem")]
        [Authorize]
        public async Task<IActionResult> DeletePiSystem(string resourceUriString)
        {
            try
            {
                _ = resourceUriString ?? throw new ArgumentNullException(nameof(resourceUriString));

                await graphManager.ClearPiSystemAsync(resourceUriString);
                logger?.LogInformation($"Deleted pi-system '{resourceUriString}'.");
                return StatusCode(200);
            }
            catch (Exception ex)
            {
                logger?.LogError(ex, "Error deleting pi-system.");
                return StatusCode(500, ex.Message);
            }
        }

        [HttpGet("GetPiSystemMetadata")]
        [Authorize]
        [Produces("application/json")]
        public async Task<ActionResult<EventMetadata>> GetPiSystemMetadata(string resourceUriString)
        {
            try
            {
                _ = resourceUriString ?? throw new ArgumentNullException(nameof(resourceUriString));

                EventMetadata metadata = await graphManager.GetPiSystemMetadataAsync(resourceUriString);

                if (metadata == null)
                {
                    logger?.LogWarning($"Pi-system metadata '{resourceUriString}' is null.");
                }
                else
                {
                    logger?.LogInformation($"Return pi-system metadata '{resourceUriString}'");
                }

                return StatusCode(200, metadata);
            }
            catch (Exception ex)
            {
                logger?.LogError(ex, "Error getting pi-system metadata.");
                return StatusCode(500, ex.Message);
            }
        }

        [HttpGet("GetPiSystemMetrics")]
        [Authorize]
        [Produces("application/json")]
        public async Task<ActionResult<CommunicationMetrics>> GetPiSystemMetrics(string resourceUriString)
        {
            try
            {
                _ = resourceUriString ?? throw new ArgumentNullException(nameof(resourceUriString));

                CommunicationMetrics metrics = await graphManager.GetPiSystemMetricsAsync(resourceUriString);
                logger?.LogInformation($"Returning communication metrics for pi-system '{resourceUriString}'.");
                if (metrics == null)
                {
                    logger?.LogWarning($"Communication metrics for '{resourceUriString}' is null.");
                }
                else
                {
                    logger?.LogInformation("Communication metrics returned.");
                }

                return StatusCode(200, metrics);
            }
            catch (Exception ex)
            {
                logger?.LogError(ex, "Error getting communications metrics.");
                return StatusCode(500, ex.Message);
            }
        }

        [HttpGet("GetPiSystemSubscriptionList")]
        [Authorize]
        [Produces("application/json")]
        public async Task<ActionResult<IEnumerable<string>>> GetPiSystemSubscriptionList(string resourceUriString)
        {
            try
            {
                _ = resourceUriString ?? throw new ArgumentNullException(nameof(resourceUriString));

                IPiSystem pisystem = graphManager.GetPiSystem(resourceUriString);

                if (pisystem == null)
                {
                    logger?.LogWarning($"Pi-system '{resourceUriString}' is null.");
                }
                else
                {
                    logger?.LogInformation($"Returned pi-system '{resourceUriString}'.");
                }

                IEnumerable<string> list = await pisystem.GetSubscriptionListAsync();
                return StatusCode(200, list);
            }
            catch (Exception ex)
            {
                logger?.LogError(ex, "Error getting subscriptions from pi-system.");
                return StatusCode(500, ex.Message);
            }
        }

        [HttpGet("GetSigmaAlgebra")]
        [Authorize]
        [Produces("application/json")]
        public async Task<ActionResult<IEnumerable<string>>> GetSigmaAlgebra()
        {
            try
            {
                List<string> list = await graphManager.GetSigmaAlgebraAsync();

                logger?.LogInformation("Returning sigma algebra");
                if (list == null || list.Count == 0)
                {
                    logger?.LogWarning("No sigma algebras found.");
                }
                else
                {
                    logger?.LogInformation("Sigma algebras returned.");
                }

                return StatusCode(200, list.ToArray());
            }
            catch (Exception ex)
            {
                logger?.LogError(ex, "Error getting sigma algebra");
                return StatusCode(500, ex.Message);
            }
        }

        [HttpGet("GetSigmaAlgebraWithFilter")]
        [Authorize]
        [Produces("application/json")]
        public async Task<ActionResult<IEnumerable<string>>> GetSigmaAlgebra(string filter)
        {
            try
            {
                List<string> list = await graphManager.GetSigmaAlgebraAsync(filter);

                logger?.LogInformation("Returning sigma algebra");
                if (list == null || list.Count == 0)
                {
                    logger?.LogWarning("No sigma algebras found.");
                }
                else
                {
                    logger?.LogInformation("Sigma algebras returned.");
                }

                return StatusCode(200, list.ToArray());
            }
            catch (Exception ex)
            {
                logger?.LogError(ex, "Error getting sigma algebra");
                return StatusCode(500, ex.Message);
            }
        }

        [HttpPost("PageSigmaAlgebra")]
        [Authorize]
        [Produces("application/json")]
        public async Task<ActionResult<ListContinuationToken>> GetSigmaAlgebra(ListContinuationToken token)
        {
            try
            {
                ListContinuationToken continuationToken = await graphManager.GetSigmaAlgebraAsync(token);

                logger?.LogInformation("Returning sigma algebra");
                if (continuationToken == null)
                {
                    logger?.LogWarning("No sigma algebras found.");
                }
                else
                {
                    logger?.LogInformation("Sigma algebras returned.");
                }

                return StatusCode(200, continuationToken);
            }
            catch (Exception ex)
            {
                logger?.LogError(ex, "Error getting sigma algebra");
                return StatusCode(500, ex.Message);
            }
        }

        [HttpPost("Subscribe")]
        [Authorize]
        [Produces("application/json")]
        public async Task<ActionResult<string>> Subscribe(string resourceUriString, SubscriptionMetadata metadata)
        {
            try
            {
                _ = resourceUriString ?? throw new ArgumentNullException(nameof(resourceUriString));
                _ = metadata ?? throw new ArgumentNullException(nameof(metadata));

                string subscriptionUriString = await graphManager.SubscribeAsync(resourceUriString, metadata);
                logger?.LogInformation(
                    $"Subscribe to pi-system '{resourceUriString}' with subscriptionId '{subscriptionUriString}'.");

                return StatusCode(200, subscriptionUriString);
            }
            catch (Exception ex)
            {
                logger?.LogError(ex, "Error subscribing to pi-system.");
                return StatusCode(500, ex.Message);
            }
        }

        [HttpDelete("Unsubscribe")]
        [Authorize]
        public async Task<IActionResult> Unsubscribe(string subscriptionUriString)
        {
            try
            {
                _ = subscriptionUriString ?? throw new ArgumentNullException(nameof(subscriptionUriString));

                await graphManager.UnsubscribeAsync(subscriptionUriString.ToLowerInvariant());
                logger?.LogInformation($"Unsubscribed subscription '{subscriptionUriString}'.");
                return StatusCode(200);
            }
            catch (Exception ex)
            {
                logger?.LogError(ex, "Error unsubscribing.");
                return StatusCode(500, ex.Message);
            }
        }

        [HttpPut("UpsertPiSystemMetadata")]
        [Authorize]
        public async Task<IActionResult> UpsertPiSystemMetadata(EventMetadata metadata)
        {
            try
            {
                _ = metadata ?? throw new ArgumentNullException(nameof(metadata));

                await graphManager.UpsertPiSystemMetadataAsync(metadata);
                logger?.LogInformation($"Upserted pi-system metadata for '{metadata.ResourceUriString}'.");
                return StatusCode(200);
            }
            catch (Exception ex)
            {
                logger?.LogError(ex, "Error upserting pi-system metadata.");
                return StatusCode(500, ex.Message);
            }
        }
    }
}