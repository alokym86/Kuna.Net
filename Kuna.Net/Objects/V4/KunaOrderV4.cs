using System;
using System.Collections.Generic;
using Newtonsoft.Json;

namespace Kuna.Net.Objects.V4
{
    public class KunaOrderOnPlacingV4
    {
        [JsonProperty("id")]
        public Guid Id { get; set; }

        [JsonProperty("type")]
        public KunaOrderTypeV4 Type { get; set; }

        /// <summary>
        /// Original order quantity
        /// </summary>
        [JsonProperty("quantity")]
        public decimal Quantity { get; set; }

        /// <summary>
        /// Traded quantity in stock (>0 if traded)
        /// </summary>
        [JsonProperty("executedQuantity")]
        public decimal ExecutedQuantity { get; set; }

        [JsonProperty("pair")]
        public string Pair { get; set; }

        [JsonProperty("price")]
        public decimal Price { get; set; }

        [JsonProperty("status")]
        public KunaOrderStatusV4 Status { get; set; }

        [JsonProperty("createdAt")]
        public DateTimeOffset CreatedAt { get; set; }

        [JsonProperty("updatedAt")]
        public DateTimeOffset UpdatedAt { get; set; }
    }

    public class KunaOrderV4 : KunaOrderOnPlacingV4
    {
        /// <summary>
        /// Traded quantity in money (>0 if traded)
        /// </summary>
        [JsonProperty("cumulativeQuoteQty")]
        public decimal CumulativeQuoteQty { get; set; }

        /// <summary>
        /// Total amount
        /// </summary>
        [JsonProperty("cost")]
        public decimal Cost { get; set; }

        /// <summary>
        /// Bid for buying base asset, Ask for selling base asset.
        /// </summary>
        [JsonProperty("side")]
        public KunaOrderSideV4 Side { get; set; }

        /// <summary>
        ///  Date-time of order finish time, UTC
        /// </summary>
        [JsonProperty("closedAt")]
        public DateTimeOffset? ClosedAt { get; set; }

        /// <summary>
        /// Attention!
        /// Non null only for KunaV4RestApiClient.GetOrderAsync(withTrades : true)!
        /// For socket updates it will be null too!
        /// </summary>
        [JsonProperty("trades")]
        public List<KunaUserTradeV4> Trades { get; set; }
    }
}