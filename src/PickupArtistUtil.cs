using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;
using Vintagestory.GameContent;

namespace PickupArtist;

public static class PickupArtistUtil {
  public static void GiveToPlayer(IWorldAccessor world, IPlayer player, BlockPos position, ItemStack stack) {
    GiveToPlayer(world, player, position, new ItemStack[] { stack });
  }
  public static void GiveToPlayer(IWorldAccessor world, IPlayer player, BlockPos position, ItemStack[] stacks) {
    foreach (var stack in stacks) {
      var original = stack.Clone(); // TryPutInto modifies the stack
      DummySlot sourceSlot = new DummySlot(stack);
      List<ItemSlot> skipSlots = new List<ItemSlot>(); // Helps prevent infinite loop
      while (stack.StackSize > 0) {
        ItemSlot? destinationSlot = GetBestDestinationSlot(world, player, stack, skipSlots);
        if (destinationSlot == null) break;
        sourceSlot.TryPutInto(world, destinationSlot, stack.StackSize);
        skipSlots.Add(destinationSlot);
      }
      if (stack.StackSize > 0) world.SpawnItemEntity(stack, position.ToVec3d().AddCopy(0.5, 0.1, 0.5));
      TreeAttribute tree = new TreeAttribute();
      tree["itemstack"] = new ItemstackAttribute(original);
      tree["byentityid"] = new LongAttribute(player.Entity.EntityId);
      world.Api.Event.PushEvent("onitemcollected", tree);
    }
  }

  public static ItemSlot? GetBestDestinationSlot(IWorldAccessor world, IPlayer player, ItemStack stack, List<ItemSlot>? skipSlots = null) {
    skipSlots ??= new();

    IInventory hotbar = player.InventoryManager.GetOwnInventory(GlobalConstants.hotBarInvClassName);
    ItemSlot? destinationSlot = FindSmallestMergableSlot(world, hotbar, stack);
    if (destinationSlot != null && !skipSlots.Contains(destinationSlot)) return destinationSlot;

    destinationSlot = player.InventoryManager.ActiveHotbarSlot;
    if (destinationSlot.Empty) return destinationSlot;
    if (IsMergable(world, destinationSlot, stack) && !skipSlots.Contains(destinationSlot)) return destinationSlot;

    IInventory backpack = player.InventoryManager.GetOwnInventory(GlobalConstants.backpackInvClassName);
    destinationSlot = FindSmallestMergableSlot(world, backpack, stack);
    if (destinationSlot != null && !skipSlots.Contains(destinationSlot)) return destinationSlot;

    var haystack = backpack.Concat(hotbar);
    return haystack
      .Where(slot => slot.Empty && !skipSlots.Contains(slot))
      .DefaultIfEmpty(null)
      .First();
  }

  public static ItemSlot? GetBestSourceSlotForPile(BlockEntityGroundStorage pile, IPlayer player) {
    ItemSlot? storageSlot = pile.GetField<InventoryGeneric>("inventory")?[0];
    if (storageSlot?.Empty != false) {
      BlockPos belowPos = pile.Pos.DownCopy();
      BlockEntity belowEntity = pile.Api.World.BlockAccessor.GetBlockEntity(belowPos);
      if (belowEntity is BlockEntityGroundStorage belowStorage) {
        storageSlot = belowStorage.GetField<InventoryGeneric>("inventory")?[0];
      }
    }
    if (storageSlot == null) return null;
    return GetBestSourceSlot(pile.Api.World, player, storageSlot.Itemstack);
  }

  public static ItemSlot? GetBestSourceSlot(IWorldAccessor world, IPlayer player, ItemStack stack) {
    ItemSlot activeSlot = player.InventoryManager.ActiveHotbarSlot;
    if (IsMergable(world, activeSlot, stack)) return activeSlot;
    IInventory backpack = player.InventoryManager.GetOwnInventory(GlobalConstants.backpackInvClassName);
    IInventory hotbar = player.InventoryManager.GetOwnInventory(GlobalConstants.hotBarInvClassName);
    return
      FindSmallestMergableSlot(world, backpack, stack) ??
      FindSmallestMergableSlot(world, hotbar, stack);
  }

  public static ItemSlot? FindSmallestMergableSlot(IWorldAccessor world, IEnumerable<ItemSlot> slots, ItemStack stack) =>
    slots
      .Where(slot => IsMergable(world, slot, stack))
      .OrderBy(slot => slot.StackSize)
      .DefaultIfEmpty(null)
      .First();

  /// <summary>
  /// Note: an empty slot is not mergable
  /// </summary>
  public static bool IsMergable(IWorldAccessor world, ItemSlot slot, ItemStack stack) =>
    slot.Itemstack?.Equals(world, stack, GlobalConstants.IgnoredStackAttributes) == true;

  [Conditional("DEBUG")]
  public static void PickupDebug(this ILogger logger, string msg) =>
    logger.Debug("[PickupArtist] " + msg);

  [Conditional("DEBUG")]
  public static void PickupDebug(this ILogger logger, string format, params object?[] args) =>
    logger.Debug("[PickupArtist] " + format, args);
}
