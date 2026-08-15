using System.Text.Json.Serialization;

namespace ProductsPlugin.Domain.Entities;

public enum InventoryStatus
{
    [JsonStringEnumMemberName("INSTOCK")]
    Instock,

    [JsonStringEnumMemberName("LOWSTOCK")]
    Lowstock,

    [JsonStringEnumMemberName("OUTOFSTOCK")]
    Outofstock
}