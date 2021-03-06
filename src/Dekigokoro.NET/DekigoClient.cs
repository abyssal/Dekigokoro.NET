﻿using Newtonsoft.Json;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;

namespace Dekigokoro.NET
{
    /// <summary>
    ///     Represents an HTTP client that can be used to interface with the Dekigokoro API.
    /// </summary>
    public class DekigoClient
    {
        private const string ApiBaseUrl = "https://dekigokoro.io/api/v1";

        internal HttpClient HttpClient { get; }

        private readonly string _token;
        private readonly DekigoClientOptions _clientOptions;

        /// <summary>
        ///     Creates a new <see cref="DekigoClient"/>.
        /// </summary>
        /// <param name="token">The API token to use when authorizing.</param>
        /// <param name="clientOptions">The client configuration.</param>
        public DekigoClient(string token, DekigoClientOptions clientOptions = null)
        {
            _token = token;
            _clientOptions = clientOptions ?? new DekigoClientOptions();

            if (string.IsNullOrWhiteSpace(_token)) throw new ArgumentException("Token must not be null, or whitespace.", nameof(token));

            HttpClient = new HttpClient();
            HttpClient.DefaultRequestHeaders.TryAddWithoutValidation("Authorization", _token);
        }

        private async Task<string> InternalRequestAsync(Uri requestUri, HttpMethod method, HttpContent content = null)
        {
            if (content != null && content.Headers.ContentType.MediaType != "application/json" && method != HttpMethod.Get)
            {
                content.Headers.Clear();
                content.Headers.ContentType = new MediaTypeHeaderValue("application/json");
            }

            var message = new HttpRequestMessage
            {
                Method = method,
                RequestUri = requestUri,
                Content = content
            };

            var response = await HttpClient.SendAsync(message);

            var contentString = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
            {
                Console.WriteLine(contentString);
            }
            response.EnsureSuccessStatusCode(); // I should probably do something about this, but this is good for now

            response.Dispose();
            message.Dispose();

            return contentString;
        }

        private async Task<T> RequestModelAsync<T>(string requestUri, HttpMethod method, object content = null)
        {
            var value = await InternalRequestAsync(new Uri(ApiBaseUrl + requestUri), method, new StringContent(JsonConvert.SerializeObject(content)));
            return JsonConvert.DeserializeObject<T>(value);
        }

        #region Currency

        /// <summary>
        ///     Fetches the current currency balance for a player.
        /// </summary>
        /// <param name="playerId">The ID of the player.</param>
        /// <param name="subKey">The subkey to use. Optional.</param>
        /// <returns>A <see cref="Task"/> representing the asynchronous fetch operation.</returns>
        public Task<PlayerCurrency> GetPlayerCurrencyAsync(ulong playerId, string subKey = null)
        {
            if (subKey != null)
                return RequestModelAsync<PlayerCurrency>("/currency/" + playerId + "/" + subKey, HttpMethod.Get);

            return RequestModelAsync<PlayerCurrency>("/currency/" + playerId, HttpMethod.Get);
        }

        /// <summary>
        ///     Sets the currency balance for a player.
        /// </summary>
        /// <param name="playerId">The ID of the player.</param>
        /// <param name="newBalance">The new balance to set for the player.</param>
        /// <param name="subKey">The subkey to use. Optional.</param>
        /// <returns>A <see cref="Task"/> representing the asynchronous put operation.</returns>
        public Task<PlayerCurrency> SetPlayerCurrencyAsync(ulong playerId, long newBalance, string subKey = null)
        {
            var data = new
            {
                balance = newBalance.ToString()
            };

            if (subKey != null)
                return RequestModelAsync<PlayerCurrency>("/currency/" + playerId + "/" + subKey, HttpMethod.Put, data);

            return RequestModelAsync<PlayerCurrency>("/currency/" + playerId, HttpMethod.Put, data);
        }

        /// <summary>
        ///     Increments or decrements the currency balance for a player.
        /// </summary>
        /// <param name="playerId">The ID of the player.</param>
        /// <param name="increment">The amount to change the balance by. Could be negative, to decrement the player's balance.</param>
        /// <param name="subKey">The subkey to use. Optional.</param>
        /// <returns>A <see cref="Task"/> representing the asynchronous patch operation.</returns>
        public Task<PlayerCurrency> ChangePlayerCurrencyAsync(ulong playerId, long increment, string subKey = null)
        {
            var data = new
            {
                increment = increment.ToString()
            };

            if (subKey != null)
                return RequestModelAsync<PlayerCurrency>("/currency/" + playerId + "/" + subKey, new HttpMethod("PATCH") /* wtf .net? */, data);

            return RequestModelAsync<PlayerCurrency>("/currency/" + playerId, new HttpMethod("PATCH"), data);
        }

        /// <summary>
        ///     Fetches the leaderboard of player currency, optionally using a subkey.
        /// </summary>
        /// <param name="offset">The position to get results after. Must be positive.</param>
        /// <param name="limit">The aximum number of values to return. Must be between 1 and 100.</param>
        /// <param name="subKey">The subkey to use. Optional.</param>
        /// <returns>A <see cref="Task"/> representing the asynchronous fetch operation.</returns>
        public Task<IEnumerable<PlayerCurrencyRanked>> GetPlayerCurrencyLeaderboardAsync(int offset = 0, int limit = 100, string subKey = null)
        {
            if (limit < 1) throw new ArgumentException("Limit cannot be below 1.", nameof(limit));
            if (limit > 100) throw new ArgumentException("Limit cannot be over 100.", nameof(limit));
            if (offset < 0) throw new ArgumentException("Offset cannot be below 0.", nameof(offset));

            if (subKey != null)
                return RequestModelAsync<IEnumerable<PlayerCurrencyRanked>>("/currency/rankings/" + subKey, HttpMethod.Get);

            return RequestModelAsync<IEnumerable<PlayerCurrencyRanked>>("/currency/rankings", HttpMethod.Get);
        }

        #endregion
    }
}
