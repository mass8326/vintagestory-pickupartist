using HarmonyLib;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.MathTools;
using Vintagestory.GameContent;

namespace PickupArtist.Patches;

[HarmonyPatch(typeof(BlockEntityGroundStorage), nameof(BlockEntityGroundStorage.OnPlayerInteractStart))]
public static class BlockEntityGroundStorage_OnPlayerInteractStart_Patch {
  public static bool Prefix(BlockEntityGroundStorage __instance, ref bool __result, IPlayer player) {
    var world = __instance.Api.World;
    var pos = __instance.Pos;
    if (!BlockBehaviorReinforcable.AllowRightClickPickup(world, pos, player)) return true;

    var storageSlot = __instance.GetField<InventoryGeneric>("inventory").FirstNonEmptySlot;
    if (storageSlot?.Empty != false) return true;

    // Vanilla behavior: determine storage props from active hotbar slot
    // Tweaked behavior: use the item slot from storage instead
    // This is important because storage props determines transfer quantity
    __instance.DetermineStorageProperties(storageSlot);
    if (__instance.StorageProps?.Layout != EnumGroundStorageLayout.Stacking) return true;

    BlockPos abovePos = __instance.Pos.UpCopy();
    BlockEntity aboveEntity = __instance.Api.World.BlockAccessor.GetBlockEntity(abovePos);
    if (aboveEntity is BlockEntityGroundStorage) return true;

    var sourceSlot = PickupArtistUtil.GetBestSourceSlot(world, player, storageSlot.Itemstack);
    if (sourceSlot == null) return true;

    bool sneaking = player.Entity.Controls.ShiftKey;
    if (sneaking && sourceSlot.Empty) return true;

    if (sneaking && __instance.TotalStackSize >= __instance.Capacity) return true;

    lock (__instance.inventoryLock) {
      var success = sneaking ? __instance.TryPutItem(player) : __instance.TryTakeItem(player);
      if (success) {
        __instance.MarkDirty(true);
        __instance.updateMeshes();
      }
      var inventory = __instance.GetField<InventoryGeneric>("inventory");
      if (inventory.Empty) {
        __instance.Api.World.BlockAccessor.SetBlock(0, __instance.Pos);
        __instance.Api.World.BlockAccessor.TriggerNeighbourBlockUpdate(__instance.Pos);
      }
      __result = success;
      return false;
    }
  }
}

[HarmonyPatch(typeof(BlockEntityGroundStorage), nameof(BlockEntityGroundStorage.TryPutItem))]
public static class BlockEntityGroundStorage_TryPutItem_Patch {
  public static bool Prefix(BlockEntityGroundStorage __instance, ref bool __result, IPlayer player) {
    if (__instance.StorageProps?.Layout != EnumGroundStorageLayout.Stacking) return true;

    var storageSlot = __instance.GetField<InventoryGeneric>("inventory")[0];
    if (storageSlot.Empty) return true;

    var world = __instance.Api.World;
    var sourceSlot = PickupArtistUtil.GetBestSourceSlot(world, player, storageSlot.Itemstack);
    if (sourceSlot == null) return true;

    bool sprinting = player.Entity.Controls.CtrlKey;
    int transfer = sprinting ? __instance.BulkTransferQuantity : __instance.TransferQuantity;
    int remaining = __instance.Capacity - __instance.TotalStackSize;
    int quantity = GameMath.Min(sourceSlot.StackSize, transfer, remaining);

    // Add to the pile and average item temperatures
    int oldSize = storageSlot.Itemstack.StackSize;
    storageSlot.Itemstack.StackSize += quantity;
    if (storageSlot.Itemstack.StackSize > 0) {
      float tempPile = storageSlot.Itemstack.Collectible.GetTemperature(world, storageSlot.Itemstack);
      float tempAdded = sourceSlot.Itemstack.Collectible.GetTemperature(world, sourceSlot.Itemstack);
      storageSlot.Itemstack.Collectible.SetTemperature(world, storageSlot.Itemstack, (tempPile * oldSize + tempAdded * quantity) / storageSlot.Itemstack.StackSize, false);
    }

    if (player.WorldData.CurrentGameMode != EnumGameMode.Creative) {
      sourceSlot.TakeOut(quantity);
      sourceSlot.OnItemSlotModified(null);
    }

    var pos = __instance.Pos;
    var sound = __instance.StorageProps.PlaceRemoveSound.WithPathPrefixOnce("sounds/");
    world.PlaySoundAt(sound, pos.X, pos.Y, pos.Z, null, 0.88f + (float)world.Rand.NextDouble() * 0.24f, 16);

    __instance.MarkDirty();

    Cuboidf[] collBoxes = world.BlockAccessor.GetBlock(pos).GetCollisionBoxes(world.BlockAccessor, pos);
    if (collBoxes != null && collBoxes.Length > 0 && CollisionTester.AabbIntersect(collBoxes[0], pos.X, pos.Y, pos.Z, player.Entity.SelectionBox, player.Entity.SidedPos.XYZ)) {
      player.Entity.SidedPos.Y += collBoxes[0].Y2 - (player.Entity.SidedPos.Y - (int)player.Entity.SidedPos.Y);
    }

    __result = true;
    return false;
  }
}

[HarmonyPatch(typeof(BlockEntityGroundStorage), nameof(BlockEntityGroundStorage.TryTakeItem))]
public static class BlockEntityGroundStorage_TryTakeItem_Patch {
  public static bool Prefix(BlockEntityGroundStorage __instance, ref bool __result, IPlayer player) {
    if (__instance.StorageProps?.Layout != EnumGroundStorageLayout.Stacking) return true;

    var storageSlot = __instance.GetField<InventoryGeneric>("inventory")[0];
    if (storageSlot.Empty) return true;

    bool sprinting = player.Entity.Controls.CtrlKey;
    int transfer = sprinting ? __instance.BulkTransferQuantity : __instance.TransferQuantity;
    int quantity = GameMath.Min(transfer, __instance.TotalStackSize);

    var world = __instance.Api.World;
    var pos = __instance.Pos;
    ItemStack stack = storageSlot.TakeOut(quantity);
    PickupArtistUtil.TryGiveToPlayer(world, player, pos, stack);

    if (__instance.TotalStackSize == 0) __instance.Api.World.BlockAccessor.SetBlock(0, pos);

    world.PlaySoundAt(__instance.StorageProps.PlaceRemoveSound, pos.X, pos.Y, pos.Z, null, 0.88f + (float)world.Rand.NextDouble() * 0.24f, 16);

    __instance.MarkDirty();

    (player as IClientPlayer)?.TriggerFpAnimation(EnumHandInteract.HeldItemInteract);

    __result = true;
    return false;
  }
}
