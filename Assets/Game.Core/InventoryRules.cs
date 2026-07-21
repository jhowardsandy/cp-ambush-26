#nullable enable

using System;
using System.Linq;

namespace TacticalStrategyGame.Core
{

public static class InventoryRules
{
    public static int QuantityOf(UnitState unit, string itemId) =>
        (unit.Inventory ?? Array.Empty<InventoryItemState>()).SingleOrDefault(item => StringComparer.Ordinal.Equals(item.ItemId, itemId))?.Quantity ?? 0;

    public static UnitState Consume(UnitState unit, string itemId, int quantity) =>
    {
        if (quantity <= 0) return unit;
        return unit with
        {
            Inventory = (unit.Inventory ?? Array.Empty<InventoryItemState>())
                .Select(item => StringComparer.Ordinal.Equals(item.ItemId, itemId) ? item with { Quantity = item.Quantity - quantity } : item)
                .ToArray()
        };
    }
}

}
