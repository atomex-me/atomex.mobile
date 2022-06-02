using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using atomex.Models;
using Atomex;
using Atomex.Common;
using Atomex.Cryptography.Abstract;
using NBitcoin;
using NBitcoin.Protocol;
using Newtonsoft.Json;
using Serilog;

namespace atomex.Services
{
    public class TokenDeviceService
    {
        public static async Task<bool> SendTokenToServerAsync(string deviceToken, string fileSystem, IAtomexApp atomexApp, CancellationToken cancellationToken = default)
        {
            string token = null;
            try
            {
                token = await GetAuthToken(atomexApp, cancellationToken);
            }
            catch (Exception e)
            {
                Log.Error(e, "Get auth token error");
            }

            if (token == null)
                return false;

            var headers = new HttpRequestHeaders
            {
                new KeyValuePair<string, IEnumerable<string>>("Authorization", new string[] {$"Bearer {token}"})
            };

            string baseUri = atomexApp.Account.Network == Atomex.Core.Network.MainNet ? "https://api.atomex.me/" : "https://api.test.atomex.me/";

            var requestUri = $"v1/guard/push?token={deviceToken}&platform={fileSystem}";

            try
            {
                var result = await HttpHelper.PostAsync(
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

                return result;
            }
            catch (Exception e)
            {
                Log.Error(e, "Auth error");
                return false;
            }
        }

        private static async Task<string> GetAuthToken(IAtomexApp atomexApp, CancellationToken cancellationToken = default)
        {
            try
            {
                var securePublicKey = atomexApp.Account.Wallet.GetServicePublicKey(0);
                var publicKey = securePublicKey.ToUnsecuredBytes();

                var timeStamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
                var message = "Sign by Atomex Mobile Client";
                var publicKeyHex = Hex.ToHexString(publicKey);
                var algorithm = "Sha256WithEcdsa:BtcMsg";
                var messageToSign = $"{message}{timeStamp}";
                var messageBytesToSign = Encoding.UTF8.GetBytes(messageToSign);
                var messageToSignHex = messageBytesToSign.ToHexString();
                var hash = BtcMessageHash(messageBytesToSign);
                var hashHex = hash.ToHexString();

                var signature = await atomexApp.Account.Wallet
                    .SignByServiceKeyAsync(hash, 0)
                    .ConfigureAwait(false);
                var signatureHex = Hex.ToHexString(signature);

                string baseUri = atomexApp.Account.Network == Atomex.Core.Network.MainNet ? "https://api.atomex.me/" : "https://api.test.atomex.me/";
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

        private static string BitcoinSignedMessageHeader = "Bitcoin Signed Message:\n";
        private static byte[] BitcoinSignedMessageHeaderBytes = Encoding.UTF8.GetBytes(BitcoinSignedMessageHeader);

        private static byte[] BtcMessageHash(byte[] messageBytes)
        {
            var ms = new MemoryStream();

            ms.WriteByte((byte)BitcoinSignedMessageHeaderBytes.Length);
            ms.Write(BitcoinSignedMessageHeaderBytes);
            ms.Write(new VarInt((ulong)messageBytes.Length).ToBytes());
            ms.Write(messageBytes);

            var messageForSigning = ms.ToArray();

            return HashAlgorithm.Sha256.Hash(messageForSigning, iterations: 2);
        }
    }
}
