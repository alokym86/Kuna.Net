﻿using CryptoExchange.Net;
using CryptoExchange.Net.Converters;
using CryptoExchange.Net.Interfaces;
using CryptoExchange.Net.Objects;
using Kuna.Net.Converters;
using Kuna.Net.Helpers;
using Kuna.Net.Interfaces;
using Kuna.Net.Objects.V2;
using Kuna.Net.Objects.V3;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace Kuna.Net
{
    public class KunaClient : RestClient, IKunaClientV2
    {
        public KunaClient() : base("KunaApiClient",new KunaClientOptions(), null)
        {
        }
        public KunaClient(KunaClientOptions options, string clientName= "KunaApiClient") : base(clientName, options, options.ApiCredentials == null ? null : new KunaAuthenticationProvider(options.ApiCredentials))
        {
            postParametersPosition = PostParameters.InUri;
            requestBodyFormat = RequestBodyFormat.Json;
        }
        #region Endpoints
        private const string ServerTimeEndpoint = "timestamp";
        private const string MarketInfoV2Endpoint = "tickers/{}";
        private const string OrderBookV2Endpoint = "depth";
        private const string AllTradesEndpoint = "trades";
        private const string AccountInfoEndpoint = "members/me";
        private const string OrdersV2Endpoint = "orders";
        private const string SingleOrderEndpoint = "order";
        private const string CancelOrderEndpoint = "order/delete";
        private const string MyTradesEndpoint = "trades/my";
        private const string CandlesHistoryEndpoint = "tv/history";
        private const string OrdersEndpoint = "auth/r/orders/";
        private const string CurrenciesEndpoint = "currencies";
        private const string TickersEndpoint = "tickers";
        private const string OrderBookEndpoint = "book/{}";
        private const string PlaceOrderEndpoint = "auth/w/order/submit";
        
        #endregion
        public CallResult<DateTime> GetServerTimeV2() => GetServerTimeV2Async().Result;
        public async Task<CallResult<DateTime>> GetServerTimeV2Async(CancellationToken ct = default)
        {
            var result = await SendRequest<string>(GetUrl(ServerTimeEndpoint), HttpMethod.Get, ct,null,false,false).ConfigureAwait(false);
            long seconds = long.Parse(result.Data);
            var dateTime = new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc).AddSeconds(seconds);
            return new CallResult<DateTime>(dateTime, null);
        }
        public CallResult<KunaTickerInfoV2> GetMarketInfoV2(string market) => GetMarketInfoV2Async(market).Result;
        public async Task<CallResult<KunaTickerInfoV2>> GetMarketInfoV2Async(string market, CancellationToken ct = default)
        {
            var result = await SendRequest<KunaTickerInfoV2>(GetUrl(FillPathParameter(MarketInfoV2Endpoint, market)), HttpMethod.Get, ct,null, false, false).ConfigureAwait(false);
            return new CallResult<KunaTickerInfoV2>(result.Data, result.Error);
        }
        public CallResult<KunaOrderBookV2> GetOrderBookV2(string market, int limit = 1000) => GetOrderBookV2Async(market, limit).Result;
        public async Task<CallResult<KunaOrderBookV2>> GetOrderBookV2Async(string market, int limit = 1000, CancellationToken ct = default)
        {
            var parameters = new Dictionary<string, object>() { { "market", market }, { "limit", limit } };
            var result = await SendRequest<KunaOrderBookV2>(GetUrl(OrderBookV2Endpoint), HttpMethod.Get, ct, parameters).ConfigureAwait(false);
            return new CallResult<KunaOrderBookV2>(result.Data, result.Error);
        }
        public CallResult<List<KunaTradeV2>> GetTradesV2(string market, DateTime? toDate = null, long? fromId = null, long? toId = null, int limit = 1000, string sort = "desc") => GetTradesV2Async(market, toDate, fromId, toId, limit, sort).Result;

        public async Task<CallResult<List<KunaTradeV2>>> GetTradesV2Async(string market, DateTime? toDate = null, long? fromId = null, long? toId = null, int limit = 1000, string sort = "desc", CancellationToken ct = default)
        {
            var parameters = new Dictionary<string, object>() { { "market", market }, { "order_by", sort } };
            if (toDate != null)
            {
                parameters.AddOptionalParameter("timestamp", JsonConvert.SerializeObject(toDate, new TimestampSecondsConverter()));
            }
            parameters.AddOptionalParameter("from", fromId);
            parameters.AddOptionalParameter("to", toId);
            if (limit > 1000)
            {
                limit = 1000;
            }
            parameters.AddOptionalParameter("limit", limit);

            var result = await SendRequest<List<KunaTradeV2>>(GetUrl(AllTradesEndpoint), HttpMethod.Get, ct, parameters, false, false).ConfigureAwait(false);
            return new CallResult<List<KunaTradeV2>>(result.Data, result.Error);
        }
        public CallResult<KunaAccountInfoV2> GetAccountInfoV2() => GetAccountInfoV2Async().Result;

        public async Task<CallResult<KunaAccountInfoV2>> GetAccountInfoV2Async(CancellationToken ct = default)
        {
            var result = await SendRequest<KunaAccountInfoV2>(GetUrl(AccountInfoEndpoint), HttpMethod.Get, ct, null, true,false).ConfigureAwait(false);
            return new CallResult<KunaAccountInfoV2>(result.Data, result.Error);
        }
        public async Task<CallResult<List<KunaPlacedOrder>>> GetOrdersAsync(KunaOrderState state, string market = null, DateTime? from = null, DateTime? to = null, int? limit = null, bool? sortDesc = null, CancellationToken ct = default)
        {
            var endpoint = OrdersEndpoint;
            if (!String.IsNullOrEmpty(market))
            {
                endpoint += $"{market}";
            }
            if (state == KunaOrderState.Done || state == KunaOrderState.Cancel)
            {
                if (!endpoint.EndsWith("/"))
                {
                    endpoint += "/";
                }
                endpoint += "hist";
            }
            var url = GetUrl(endpoint, "3");

            var parameters = new Dictionary<string, object>();
            if (from.HasValue)
                parameters.AddOptionalParameter("start", JsonConvert.SerializeObject(from, new TimestampConverter()));
            if (to.HasValue)
                parameters.AddOptionalParameter("end", JsonConvert.SerializeObject(to, new TimestampConverter()));
            if (limit.HasValue)
                parameters.AddOptionalParameter("limit", limit.Value);
            if (sortDesc.HasValue)
                parameters.AddOptionalParameter("sort", sortDesc.Value ? -1 : 1);
            var result = await SendRequest<List<KunaPlacedOrder>>(url, HttpMethod.Post, ct, parameters, true,false);
            return result;
        }
        public CallResult<List<KunaPlacedOrder>> GetOrders(KunaOrderState state, string market = null, DateTime? from = null, DateTime? to = null, int? limit = null, bool? sortDesc = null)
   => GetOrdersAsync(state, market, from, to, limit, sortDesc).Result;

        public CallResult<KunaPlacedOrderV2> PlaceOrderV2(KunaOrderTypeV2 type, KunaOrderSideV2 side, decimal volume, decimal price, string market) => PlaceOrderV2Async(type, side, volume, price, market).Result;

        public async Task<CallResult<KunaPlacedOrderV2>> PlaceOrderV2Async(KunaOrderTypeV2 type, KunaOrderSideV2 side, decimal volume, decimal price, string market, CancellationToken ct = default)
        {
            var parameters = new Dictionary<string, object>()
            {
                { "side", JsonConvert.SerializeObject(side,new OrderSideConverter()) },
                { "type", JsonConvert.SerializeObject(type,new OrderTypeV2Converter()) },
                { "volume", volume.ToString(CultureInfo.GetCultureInfo("en-US")) },
                { "market", market },
                { "price", price.ToString(CultureInfo.GetCultureInfo("en-US")) }
            };

            var result = await SendRequest<KunaPlacedOrderV2>(GetUrl(OrdersV2Endpoint), HttpMethod.Post, ct, parameters, true,false).ConfigureAwait(false);
            return new CallResult<KunaPlacedOrderV2>(result.Data, result.Error);
        }
        public CallResult<KunaPlacedOrderV2> CancelOrderV2(long orderId) => CancelOrderV2Async(orderId).Result;

        public async Task<CallResult<KunaPlacedOrderV2>> CancelOrderV2Async(long orderId, CancellationToken ct = default)
        {
            var parameters = new Dictionary<string, object>() { { "id", orderId } };
            var result = await SendRequest<KunaPlacedOrderV2>(GetUrl(CancelOrderEndpoint), HttpMethod.Post, ct, parameters, true,false).ConfigureAwait(false);
            return new CallResult<KunaPlacedOrderV2>(result.Data, result.Error);
        }
        public CallResult<List<KunaPlacedOrderV2>> GetMyOrdersV2(string market, KunaOrderStateV2 orderState = KunaOrderStateV2.Wait, int page = 1, string sort = "desc") => GetMyOrdersV2Async(market, orderState, page, sort).Result;

        public async Task<CallResult<List<KunaPlacedOrderV2>>> GetMyOrdersV2Async(string market, KunaOrderStateV2 orderState = KunaOrderStateV2.Wait, int page = 1, string sort = "desc", CancellationToken ct = default)
        {
            var parameters = new Dictionary<string, object>()
            {
                { "market", market },
                { "state", JsonConvert.SerializeObject(orderState, new OrderStatusV2Converter())},
                { "order_by", sort },
                { "page", page }
            };

            var result = await SendRequest<List<KunaPlacedOrderV2>>(GetUrl(OrdersV2Endpoint), HttpMethod.Get, ct, parameters, true,false).ConfigureAwait(false);
            return new CallResult<List<KunaPlacedOrderV2>>(result.Data, result.Error);
        }
        public CallResult<KunaPlacedOrderV2> GetOrderInfoV2(long orderId) => GetOrderInfoV2Async(orderId).Result;

        public async Task<CallResult<KunaPlacedOrderV2>> GetOrderInfoV2Async(long orderId, CancellationToken ct = default)
        {
            var parameters = new Dictionary<string, object>()
            {
                { "id", orderId },
            };

            var result = await SendRequest<KunaPlacedOrderV2>(GetUrl(SingleOrderEndpoint), HttpMethod.Get, ct, parameters, true, false).ConfigureAwait(false);
            return new CallResult<KunaPlacedOrderV2>(result.Data, result.Error);
        }
        public CallResult<List<KunaTradeV2>> GetMyTradesV2(string market, DateTime? toDate = null, long? fromId = null, long? toId = null, int limit = 1000, string sort = "desc") => GetMyTradesV2Async(market, toDate, fromId, toId, limit, sort).Result;

        public async Task<CallResult<List<KunaTradeV2>>> GetMyTradesV2Async(string market, DateTime? toDate = null, long? fromId = null, long? toId = null, int limit = 1000, string sort = "desc", CancellationToken ct = default)
        {
            var parameters = new Dictionary<string, object>() { { "market", market }, { "order_by", sort }, };
            if (toDate != null)
            {
                parameters.AddOptionalParameter("timestamp", JsonConvert.SerializeObject(toDate, new TimestampSecondsConverter()));
            }
            parameters.AddOptionalParameter("from", fromId);
            parameters.AddOptionalParameter("to", toId);
            if (limit > 1000)
            {
                limit = 1000;
            }
            parameters.AddOptionalParameter("limit", limit);
            var result = await SendRequest<List<KunaTradeV2>>(GetUrl(MyTradesEndpoint), HttpMethod.Get, ct, parameters, true,false).ConfigureAwait(false);
            return new CallResult<List<KunaTradeV2>>(result.Data, result.Error);
        }
        public async Task<CallResult<List<KunaOhclvV2>>> GetCandlesHistoryV2Async(string symbol, int resolution, DateTime from, DateTime to, CancellationToken ct = default)
        {
            var parameters = new Dictionary<string, object>() { { "symbol", symbol }, { "resolution", resolution }, { "from", JsonConvert.SerializeObject(from, new TimestampSecondsConverter()) }, { "to", JsonConvert.SerializeObject(to, new TimestampSecondsConverter()) } };
            var result = await SendRequest<TradingViewOhclvV2>(GetUrl(CandlesHistoryEndpoint, "3"), HttpMethod.Get, ct, parameters, false,false).ConfigureAwait(false);
            List<KunaOhclvV2> data = null;
            if (result.Success)
            {
                data = new List<KunaOhclvV2>();
                var t = result.Data;
                for (int i = 0; i < result.Data.Closes.Count; i++)
                {
                    var candle = new KunaOhclvV2(t.Timestamps[i], t.Opens[i], t.Highs[i], t.Lows[i], t.Closes[i], t.Volumes[i]);
                    data.Add(candle);
                }
            }
            return new CallResult<List<KunaOhclvV2>>(data, result.Error);
        }
        public CallResult<List<KunaTrade>> GetOrderTrades(string market, long id) => GetOrderTradesAsync(market, id).Result;

        public async Task<CallResult<List<KunaTrade>>> GetOrderTradesAsync(string market, long id, CancellationToken ct = default)
        {
            var url = GetUrl($"auth/r/order/{market}:{id}/trades", "3");
            var result = await SendRequest<List<KunaTrade>>(url, HttpMethod.Post, ct, new Dictionary<string, object>(), true,false);
            return result;
        }


        #region BaseMethodOverride
    

        protected override IRequest ConstructRequest(Uri uri, HttpMethod method, Dictionary<string, object> parameters, bool signed, PostParameters postParameterPosition, ArrayParametersSerialization arraySerialization, int requestId)
        {
            if (parameters == null)
                parameters = new Dictionary<string, object>();
            var uriString = uri.ToString();

            if (uriString.Contains("v2"))
            {
                if (authProvider != null && signed)
                    parameters = authProvider.AddAuthenticationToParameters(new Uri(uriString).PathAndQuery, method, parameters, signed, postParametersPosition, arraySerialization);

            }
            if ((method == HttpMethod.Get || method == HttpMethod.Delete || postParametersPosition == PostParameters.InUri) && parameters?.Any() == true)
            {
                uriString += "?" + parameters.CreateParamString(true, ArrayParametersSerialization.MultipleValues);
            }
            var request = RequestFactory.Create(method, uriString, requestId);
            // request.Content = requestBodyFormat == RequestBodyFormat.Json ? Constants.JsonContentHeader : Constants.FormContentHeader;
            request.Accept = Constants.JsonContentHeader;
            request.Method = method;
            //var headers = new Dictionary<string, string>();
            if (uriString.Contains("v3"))
            {
                if (authProvider != null)
                {
                    var headers = authProvider.AddAuthenticationToHeaders(uriString, method, parameters, signed, postParametersPosition, arraySerialization);
                    foreach (var header in headers)
                    {
                        request.AddHeader(header.Key, header.Value);
                    }
                    //  request.AddHeader("content-type", "application/json");
                }
            }
            if ((method == HttpMethod.Post || method == HttpMethod.Put) && postParametersPosition != PostParameters.InUri)
            {
                WriteParamBody(request, parameters, requestBodyFormat == RequestBodyFormat.Json ? Constants.JsonContentHeader : Constants.FormContentHeader);
            }

            return request;
        }

        protected Uri GetUrl(string endpoint, string version = null)
        {
            if (version != null)
            {
                postParametersPosition = PostParameters.InBody;
            }
            else
            {
                postParametersPosition = PostParameters.InUri;
            }
            return version == null ? new Uri($"{BaseAddress}{endpoint}") : new Uri($"https://api.kuna.io/v{version}/{endpoint}");

        }
        public CallResult<List<KunaTraidingPairV2>> GeMarketsV2() => GeMarketsV2Async().Result;

        public async Task<CallResult<List<KunaTraidingPairV2>>> GeMarketsV2Async(CancellationToken ct = default)
        {
            string url = "https://api.kuna.io/v3/markets";
            var result = await SendRequest<List<KunaTraidingPairV2>>(new Uri(url), HttpMethod.Get, ct, null, false,false).ConfigureAwait(false);
            return new CallResult<List<KunaTraidingPairV2>>(result.Data, result.Error);
        }

        public CallResult<List<KunaCurrencyV2>> GetCurrenciesV2(CancellationToken ct = default) => GetCurrenciesV2Async().Result;
        public async Task<CallResult<List<KunaCurrencyV2>>> GetCurrenciesV2Async(CancellationToken ct = default)
        {
            string url = "https://api.kuna.io/v3/currencies";
            var result = await SendRequest<List<KunaCurrencyV2>>(new Uri(url), HttpMethod.Get, ct, null, false, false).ConfigureAwait(false);
            return new CallResult<List<KunaCurrencyV2>>(result.Data, result.Error);
        }


        public CallResult<List<KunaOhclvV2>> GetCandlesHistoryV2(string symbol, int resolution, DateTime from, DateTime to) => GetCandlesHistoryV2Async(symbol, resolution, from, to).Result;

        #endregion

        #region V3
        CallResult<IEnumerable<KunaTicker>> GetTickers(params string[] symbols)
        {
            throw new NotImplementedException();
        }
        public async Task<CallResult<IEnumerable<KunaTicker>>> GetTickersAsync(CancellationToken ct = default, params string[] symbols)
        {
            var request = new Dictionary<string, object>();
            string symb = symbols.AsStringParameterOrNull() ?? "ALL";
            request.AddOptionalParameter("symbols", symb);
            return await SendRequest<IEnumerable<KunaTicker>>(GetUrl(TickersEndpoint, "3"), HttpMethod.Post, ct, request, false, false);

        }
        CallResult<KunaOrderBook> GetOrderBook(string symbol)
        {
            throw new NotImplementedException();
        }
        public async Task<CallResult<KunaOrderBook>> GetOrderBookV2Async(string symbol, CancellationToken ct = default)
        {
            var result = await SendRequest<IEnumerable<KunaOrderBookEntry>>(GetUrl(FillPathParameter(OrderBookEndpoint, symbol)), HttpMethod.Get, ct).ConfigureAwait(false);
            return new CallResult<KunaOrderBook>(new KunaOrderBook(result.Data), result.Error);
        }
        #endregion V3
    }
}
