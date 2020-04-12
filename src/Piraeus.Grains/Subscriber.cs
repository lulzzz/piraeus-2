﻿using Orleans;
using Orleans.Providers;
using Piraeus.GrainInterfaces;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Piraeus.Grains
{
    [StorageProvider(ProviderName = "store")]
    [Serializable]
    public class Subscriber : Grain<SubscriberState>, ISubscriber
    {
        #region Activate/Deactivate

        public override Task OnActivateAsync()
        {
            if (State.Container == null)
            {
                List<string> list = new List<string>();
                State.Container = list;
            }

            return Task.CompletedTask;
        }

        public override async Task OnDeactivateAsync()
        {
            await WriteStateAsync();
        }

        #endregion Activate/Deactivate

        #region List/Add/Remove Subscriptions

        public async Task AddSubscriptionAsync(string subscriptionUriString)
        {
            if (!State.Container.Contains(subscriptionUriString))
            {
                State.Container.Add(subscriptionUriString);
            }

            await WriteStateAsync();
        }

        public async Task<IEnumerable<string>> GetSubscriptionsAsync()
        {
            return await Task.FromResult<IEnumerable<string>>(State.Container);
        }

        public async Task RemoveSubscriptionAsync(string subscriptionUriString)
        {
            State.Container.Remove(subscriptionUriString);
            await WriteStateAsync();
        }

        #endregion List/Add/Remove Subscriptions

        #region Clear

        public async Task ClearAsync()
        {
            await ClearStateAsync();
        }

        #endregion Clear
    }
}