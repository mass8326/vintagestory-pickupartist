using System.Collections.Generic;
using System.Linq;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;

namespace PickupArtist;

public static class PickupArtistUtil {
  public static void TryGiveToPlayer(IWorldAccessor world, IPlayer player, BlockPos position, ItemStack stack) {
    TryGiveToPlayer(world, player, position, new ItemStack[] { stack });
  }
  public static void TryGiveToPlayer(IWorldAccessor world, IPlayer player, BlockPos position, ItemStack[] stacks) {
    foreach (var stack in stacks) {
      var original = stack.Clone(); // TryPutInto modifies the stack
      var sourceSlot = new DummySlot(stack);
      var activeSlot = player.InventoryManager.ActiveHotbarSlot;
      if (!CanMergeInto(world, activeSlot, stack)) {
        var hotbar = player.InventoryManager.GetOwnInventory(GlobalConstants.hotBarInvClassName);
        foreach (var slot in hotbar) {
          if (CanMergeInto(world, slot, stack)) sourceSlot.TryPutInto(world, slot, stack.StackSize);
        }
      }
      if (stack.StackSize > 0) sourceSlot.TryPutInto(world, activeSlot, stack.StackSize);
      if (stack.StackSize > 0) { // If stack size is 0, it will make TryGiveItemstack return false
        bool success = player.InventoryManager.TryGiveItemstack(stack, true);
        if (!success) world.SpawnItemEntity(stack, position.ToVec3d().AddCopy(0.5, 0.1, 0.5));
      }
      TreeAttribute tree = new();
      tree["itemstack"] = new ItemstackAttribute(original);
      tree["byentityid"] = new LongAttribute(player.Entity.EntityId);
      world.Api.Event.PushEvent("onitemcollected", tree);
    }
  }

  public static ItemSlot GetBestMergableSlot(IWorldAccessor world, IPlayer player, ItemStack stack) {
    var slot = player.InventoryManager.ActiveHotbarSlot;
    var empty = slot.Empty;
    var mergable = CanMergeInto(world, slot, stack);
    if (empty || !mergable) {
      var haystack = GetInventorySlots(player);
      slot = FindSmallestMergableSlot(world, haystack, stack) ?? haystack.First((slot) => slot.Empty);
    }
    return slot;
  }

  public static ItemSlot FindSmallestMergableSlot(IWorldAccessor world, IEnumerable<ItemSlot> slots, ItemStack stack) =>
    slots
      .Where(slot => CanMergeInto(world, slot, stack))
      .OrderBy(slot => slot.StackSize)
      .FirstOrDefault();

  public static bool CanMergeInto(IWorldAccessor world, ItemSlot slot, ItemStack stack) =>
    slot.Itemstack?.Equals(world, stack, GlobalConstants.IgnoredStackAttributes) == true;

  public static IEnumerable<ItemSlot> GetInventorySlots(IPlayer player) {
    var hotbar = player.InventoryManager.GetOwnInventory(GlobalConstants.hotBarInvClassName);
    var backpack = player.InventoryManager.GetOwnInventory(GlobalConstants.backpackInvClassName);
    return hotbar.Concat(backpack);
  }
}

