using System.Collections.Generic;
using HarmonyLib;
using Vintagestory.API.Common;
using Vintagestory.GameContent;

namespace PickupArtist.Patches;

[HarmonyPatch(typeof(BlockEntityCrate), nameof(BlockEntityCrate.OnBlockInteractStart))]
public static class BlockEntityCrate_OnBlockInteractStart_Patch {
  public static bool Prefix(BlockEntityCrate __instance, ref bool __result, IPlayer byPlayer, BlockSelection blockSel) {
    var world = __instance.Api.World;
    var pos = __instance.Pos;

    bool put = byPlayer.Entity.Controls.ShiftKey;
    bool take = !put;
    bool bulk = byPlayer.Entity.Controls.CtrlKey;

    bool drawIconLabel = put &&
      byPlayer.InventoryManager.ActiveHotbarSlot?.Itemstack?.ItemAttributes?["pigment"]?["color"].Exists == true &&
      blockSel.SelectionBoxIndex == 1;
    if (drawIconLabel) return true;

    var inventory = __instance.GetField<InventoryGeneric>("inventory");
    var storageSlot = inventory.FirstNonEmptySlot;
    if (storageSlot == null) return true;

    var playerSlot = PickupArtistUtil.GetBestMergableSlot(world, byPlayer, storageSlot.Itemstack);
    if (take && storageSlot != null) {
      ItemStack stack = bulk ? storageSlot.TakeOutWhole() : storageSlot.TakeOut(1);
      PickupArtistUtil.TryGiveToPlayer(world, byPlayer, pos, stack);
      if (inventory.Empty) __instance.SetField("labelMesh", null);
      storageSlot.MarkDirty();
      __instance.MarkDirty();
    }

    if (put && !playerSlot.Empty) {
      if (storageSlot == null) {
        var moved = playerSlot.TryPutInto(world, inventory[0], bulk ? playerSlot.StackSize : 1);
        if (moved > 0) __instance.CallMethod("didMoveItems", new object[] { inventory[0].Itemstack, byPlayer });
      } else {
        var mergable = PickupArtistUtil.CanMergeInto(world, playerSlot, storageSlot.Itemstack);
        if (mergable) {
          List<ItemSlot> skipSlots = new();
          while (playerSlot.StackSize > 0 && skipSlots.Count < inventory.Count) {
            var wslot = inventory.GetBestSuitedSlot(playerSlot, skipSlots);
            if (wslot.slot == null) break;
            if (playerSlot.TryPutInto(world, wslot.slot, bulk ? playerSlot.StackSize : 1) > 0) {
              __instance.CallMethod("didMoveItems", new object[] { wslot.slot.Itemstack, byPlayer });
              if (!bulk) break;
            }
            skipSlots.Add(wslot.slot);
          }
        }
      }
      playerSlot.MarkDirty();
      __instance.MarkDirty();
    }
    __result = true;
    return false;
  }
}