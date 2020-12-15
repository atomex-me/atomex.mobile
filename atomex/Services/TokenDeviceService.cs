using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using atomex.Models;
using Atomex;
using Atomex.Common;
using Newtonsoft.Json;
using Serilog;

namespace atomex.Services
{
    public class TokenDeviceService
    {
        public static async Task<bool> SendTokenToServerAsync(string deviceToken, string fileSystem, IAtomexApp atomexApp, CancellationToken cancellationToken = default)
        {
            string token = await GetAuthToken(atomexApp, cancellationToken);

            if (token == null)
                return false;

            var headers = new HttpRequestHeaders
            {
                new KeyValuePair<string, IEnumerable<string>>("Authorization", new string[] {$"Bearer {token}"})
            };

            string baseUri = atomexApp.Account.Network == Atomex.Core.Network.MainNet ? "https://api.atomex.me/" : "https://api.test.atomex.me";

            var requestUri = $"v1/guard/push?token={deviceToken}&platform={fileSystem}";

            try
            {
                var authResult = await HttpHelper.PostAsync(
                    baseUri: baseUri,
                    requestUri: requestUri,
                    headers: headers,
                    content: null,
                    responseHandler: response =>
                    {
                        if (!response.IsSuccessStatusCode)
                            return false;

                        var responseContent = response
                            .Content
                            .ReadAsStringAsync()
                            .Result;

                        if (bool.TryParse(responseContent, out var result))
                            return result;

                        return false;
                    },
                    cancellationToken: cancellationToken)
                .ConfigureAwait(false);

                return authResult;
            }
            catch (Exception e)
            {
                Log.Error(e, "Auth error");
                return false;
            }
        }

        private static async Task<string> GetAuthToken(IAtomexApp atomexApp, CancellationToken cancellationToken = default)
        {
            var securePublicKey = atomexApp.Account.Wallet.GetServicePublicKey(0);
            var publicKey = securePublicKey.ToUnsecuredBytes();

            var timeStamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
            var message = "Sign by Atomex Mobile Client";
            var publicKeyHex = Hex.ToHexString(publicKey);
            var algorithm = "Sha256WithEcdsa:BtcMsg";

            var signature = await atomexApp.Account.Wallet
                .SignByServiceKeyAsync(Encoding.UTF8.GetBytes($"{message}{timeStamp}"), 0)
                .ConfigureAwait(false);

            var signatureHex = Hex.ToHexString(signature);

            var baseUri = "https://api.atomex.me/";
            var requestUri = "v1/token";

            var jsonRequest = JsonConvert.SerializeObject(new
            {
                timeStamp = timeStamp,
                message = message,
                publicKey = publicKeyHex,
                signature = signatureHex,
                algorithm = algorithm
            });

            var requestContent = new StringContent(jsonRequest, Encoding.UTF8, "application/json");

            try
            {
                var authTokenResult = await HttpHelper.PostAsyncResult<string>(
                        baseUri: baseUri,
                        requestUri: requestUri,
                        content: requestContent,
                        responseHandler: (response, responseContent) => JsonConvert.DeserializeObject<AuthToken>(responseContent).Token,
                        cancellationToken: cancellationToken)
                    .ConfigureAwait(false);

                return authTokenResult?.Value;
            }
            catch(Exception e)
            {
                Log.Error(e, "Get auth token error");
                return null;
            }
        }
    }
}
