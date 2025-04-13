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

    bool bulk = byPlayer.Entity.Controls.CtrlKey;
    bool take = !byPlayer.Entity.Controls.ShiftKey;

    bool drawIconLabel = !take &&
      byPlayer.InventoryManager.ActiveHotbarSlot?.Itemstack?.ItemAttributes?["pigment"]?["color"].Exists == true &&
      blockSel.SelectionBoxIndex == 1;
    if (drawIconLabel) return true;

    InventoryGeneric? inventory = __instance.GetField<InventoryGeneric>("inventory");
    if (inventory == null) return true;
    var storageSlot = inventory.FirstNonEmptySlot;
    if (storageSlot == null) return true;

    __result = true;

    if (take) {
      ItemStack stack = bulk ? storageSlot.TakeOutWhole() : storageSlot.TakeOut(1);
      PickupArtistUtil.GiveToPlayer(world, byPlayer, pos, stack);
      __instance.CallMethod("didMoveItems", new object[] { stack, byPlayer });
      if (inventory.Empty) __instance.SetField("labelMesh", null);
      storageSlot.MarkDirty();
    } else {
      var playerSlot = PickupArtistUtil.GetBestSourceSlot(world, byPlayer, storageSlot.Itemstack);
      if (playerSlot == null) return false;
      List<ItemSlot> skipSlots = new();
      while (playerSlot.StackSize > 0 && skipSlots.Count < inventory.Count) {
        var wslot = inventory.GetBestSuitedSlot(playerSlot, null, skipSlots);
        if (wslot.slot == null) break;
        if (playerSlot.TryPutInto(world, wslot.slot, bulk ? playerSlot.StackSize : 1) > 0) {
          __instance.CallMethod("didMoveItems", new object[] { wslot.slot.Itemstack, byPlayer });
          if (!bulk) break;
        }
        skipSlots.Add(wslot.slot);
      }
      playerSlot.MarkDirty();
    }
    __instance.MarkDirty();
    return false;
  }
}
