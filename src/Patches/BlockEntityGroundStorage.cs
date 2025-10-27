using System;
using HarmonyLib;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.MathTools;
using Vintagestory.GameContent;

namespace PickupArtist.Patches;

[HarmonyPatch(typeof(BlockEntityGroundStorage), nameof(BlockEntityGroundStorage.OnPlayerInteractStart))]
public static class BlockEntityGroundStorage_OnPlayerInteractStart_Patch {
  public static bool Prefix(BlockEntityGroundStorage __instance, ref bool __result, IPlayer player, BlockSelection bs) {
    if (player.WorldData.CurrentGameMode == EnumGameMode.Creative) {
      __instance.Api.World.Logger.PickupDebug("Ignoring ground storage interaction start due to creative mode");
      return true;
    }
    IWorldAccessor world = __instance.Api.World;
    BlockPos pos = __instance.Pos;
    if (!BlockBehaviorReinforcable.AllowRightClickPickup(world, pos, player)) {
      __instance.Api.World.Logger.PickupDebug("Ignoring ground storage interaction due to disallowed right click pickup block behavior");
      return true;
    }

    // Vanilla behavior: determine storage props from active hotbar slot
    // Tweaked behavior: determine storage props from best determined source slot
    // This is important because storage props determines transfer quantity
    ItemSlot? sourceSlot = PickupArtistUtil.GetBestSourceSlotForPile(__instance, player);
    __instance.DetermineStorageProperties(sourceSlot);
    if (__instance.StorageProps?.Layout != EnumGroundStorageLayout.Stacking) {
      __instance.Api.World.Logger.PickupDebug("Ignoring ground storage interaction start due to incompatible storage layout: '{0}'", __instance.StorageProps?.Layout);
      return true;
    }

    __instance.Api.World.Logger.PickupDebug("Modifying pile interaction start at {0}", pos);
    bool success = __instance.CallMethod<bool>("putOrGetItemStacking", new object[] { player, bs });
    if (success) __instance.MarkDirty(true);

    InventoryGeneric? inventory = __instance.GetField<InventoryGeneric>("inventory");
    if (inventory?.Empty != false) {
      __instance.Api.World.Logger.PickupDebug("Removing pile block due to empty inventory");
      __instance.Api.World.BlockAccessor.SetBlock(0, __instance.Pos);
      __instance.Api.World.BlockAccessor.TriggerNeighbourBlockUpdate(__instance.Pos);
    }
    __result = success;
    return false;
  }
}

[HarmonyPatch(typeof(BlockEntityGroundStorage), "putOrGetItemStacking")]
public static class BlockEntityGroundStorage_putOrGetItemStacking_Patch {
  public static bool Prefix(BlockEntityGroundStorage __instance, ref bool __result, IPlayer byPlayer, BlockSelection bs) {
    IWorldAccessor world = __instance.Api.World;
    BlockPos pilePos = __instance.Pos;
    BlockPos abovePos = pilePos.UpCopy();
    BlockEntity aboveEntity = __instance.Api.World.BlockAccessor.GetBlockEntity(abovePos);

    if (aboveEntity is BlockEntityGroundStorage aboveEntityGroundStorage) {
      GroundStorageProperties? aboveProps = aboveEntityGroundStorage.GetStoragePropsWithWarning(world.Logger);
      // TODO: Implement logic for MaxStackingHeight
      if (aboveProps?.Layout == EnumGroundStorageLayout.Stacking) {
        world.Logger.PickupDebug("Forwarding pile interaction start to pile above");
        __result = aboveEntityGroundStorage.OnPlayerInteractStart(byPlayer, bs);
        return false;
      }
    }

    bool sneaking = byPlayer.Entity.Controls.ShiftKey;
    ItemSlot? sourceSlot = PickupArtistUtil.GetBestSourceSlotForPile(__instance, byPlayer);
    if (sneaking && sourceSlot?.Empty != false) {
      world.Logger.PickupDebug("Ignoring put attempt due to empty/unavailable source slot");
      return true;
    }

    if (sneaking && __instance.TotalStackSize >= __instance.Capacity) {
      BlockGroundStorage pileBlock = (BlockGroundStorage)world.BlockAccessor.GetBlock(pilePos);
      Block aboveBlock = world.BlockAccessor.GetBlock(abovePos);
      if (!aboveBlock.IsReplacableBy(pileBlock)) {
        world.Logger.PickupDebug("Cannot create new pile block: space above is occupied");
        __result = false;
        return false;
      }


      // Vanilla behavior: simulate creating a new storage pile block from active hotbar slot as if clicking at the top of the stack
      // Tweaked behavior: manually create the store pile block at the top using the item of the current pile block
      if (!world.Claims.TryAccess(byPlayer, bs.Position, EnumBlockAccessFlags.BuildOrBreak)) {
        world.Logger.PickupDebug("Cannot create new pile block: player does not have claim access");
        byPlayer.InventoryManager.ActiveHotbarSlot.MarkDirty();
        __result = false;
        return false;
      }

      double dx = byPlayer.Entity.Pos.X - (abovePos.X + bs.HitPosition.X);
      double dz = byPlayer.Entity.Pos.Z - (abovePos.Z + bs.HitPosition.Z);
      float angleHor = (float)Math.Atan2(dx, dz);
      float deg90 = GameMath.PIHALF;
      float roundRad = ((int)Math.Round(angleHor / deg90)) * deg90;

      world.Logger.PickupDebug("Creating new pile in space above");
      world.BlockAccessor.SetBlock(pileBlock.BlockId, abovePos);
      if (world.BlockAccessor.GetBlockEntity(abovePos) is BlockEntityGroundStorage blockEntityGroundStorage) {
        blockEntityGroundStorage.MeshAngle = roundRad;
        blockEntityGroundStorage.clientsideFirstPlacement = world.Side == EnumAppSide.Client;
        world.Logger.PickupDebug("Forwarding pile interaction start to new pile");
        blockEntityGroundStorage.OnPlayerInteractStart(byPlayer, bs);
      } else {
        __result = false;
      }

      (byPlayer as IClientPlayer)?.TriggerFpAnimation(EnumHandInteract.HeldItemInteract);
      return false;
    }

    lock (__instance.inventoryLock) {
      __result = sneaking ? __instance.TryPutItem(byPlayer) : __instance.TryTakeItem(byPlayer);
    }
    return false;
  }
}

[HarmonyPatch(typeof(BlockEntityGroundStorage), nameof(BlockEntityGroundStorage.TryPutItem))]
public static class BlockEntityGroundStorage_TryPutItem_Patch {
  public static bool Prefix(BlockEntityGroundStorage __instance, ref bool __result, IPlayer player) {
    if (player.WorldData.CurrentGameMode == EnumGameMode.Creative) {
      __instance.Api.World.Logger.PickupDebug("Ignoring TryPutItem due to creative mode");
      return true;
    }
    if (__instance.StorageProps?.Layout != EnumGroundStorageLayout.Stacking) {
      __instance.Api.World.Logger.PickupDebug("Ignoring TryPutItem due to incompatible storage layout: '{0}'", __instance.StorageProps?.Layout);
      return true;
    }

    IWorldAccessor world = __instance.Api.World;
    BlockPos pos = __instance.Pos;
    ItemSlot? storageSlot = __instance.GetStorageSlot();
    if (storageSlot == null) {
      __instance.Api.World.Logger.PickupDebug("Ignoring TryPutItem due to indeterminable storage slot");
      return true;
    }

    ItemSlot? sourceSlot = PickupArtistUtil.GetBestSourceSlotForPile(__instance, player);
    if (sourceSlot?.Empty != false) {
      __instance.Api.World.Logger.PickupDebug("Ignoring TryPutItem due to indeterminable source slot");
      return true;
    }

    bool sprinting = player.Entity.Controls.CtrlKey;
    int transfer = sprinting ? __instance.BulkTransferQuantity : __instance.TransferQuantity;
    int remaining = __instance.Capacity - __instance.TotalStackSize;
    int addQty = GameMath.Min(sourceSlot.StackSize, transfer, remaining);

    // Add to the pile and average item temperatures
    // Do not use `sourceSlot.TryPutInto` because it breaks ingot piles for some reason
    // Probably due to some merge attribute checks failing
    if (storageSlot.Itemstack != null && storageSlot.Itemstack.StackSize > 0) {
      int oldSize = storageSlot.Itemstack.StackSize;
      float oldTemp = storageSlot.Itemstack.Collectible.GetTemperature(world, storageSlot.Itemstack);
      float addTemp = sourceSlot.Itemstack.Collectible.GetTemperature(world, sourceSlot.Itemstack);
      storageSlot.Itemstack.StackSize += addQty;
      storageSlot.Itemstack.Collectible.SetTemperature(world, storageSlot.Itemstack, (oldTemp * oldSize + addTemp * addQty) / storageSlot.Itemstack.StackSize, false);
    } else {
      storageSlot.Itemstack = sourceSlot.Itemstack.GetEmptyClone();
      storageSlot.Itemstack.StackSize = addQty;
    }
    sourceSlot.TakeOut(addQty);
    sourceSlot.OnItemSlotModified(null);
    AssetLocation? sound = __instance.StorageProps?.PlaceRemoveSound?.WithPathPrefixOnce("sounds/");
    if (sound != null) world.PlaySoundAt(sound, pos.X + 0.5, pos.Y, pos.Z + 0.5, null, 0.88f + (float)world.Rand.NextDouble() * 0.24f, 16);

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
    var world = __instance.Api.World;
    GroundStorageProperties? props = __instance.GetStoragePropsWithWarning(world.Logger);
    if (props?.Layout != EnumGroundStorageLayout.Stacking) return true;

    ItemSlot? storageSlot = __instance.GetStorageSlot();
    if (storageSlot?.Empty != false) return true;

    bool sprinting = player.Entity.Controls.CtrlKey;
    int transfer = sprinting ? __instance.BulkTransferQuantity : __instance.TransferQuantity;
    int quantity = GameMath.Min(transfer, __instance.TotalStackSize);

    var pos = __instance.Pos;
    ItemStack stack = storageSlot.TakeOut(quantity);
    PickupArtistUtil.GiveToPlayer(world, player, pos, stack);

    if (__instance.TotalStackSize == 0) __instance.Api.World.BlockAccessor.SetBlock(0, pos);

    AssetLocation? sound = props?.PlaceRemoveSound;
    if (sound != null) world.PlaySoundAt(sound, pos.X + 0.5, pos.Y, pos.Z + 0.5, null, 0.88f + (float)world.Rand.NextDouble() * 0.24f, 16);

    __instance.MarkDirty();

    (player as IClientPlayer)?.TriggerFpAnimation(EnumHandInteract.HeldItemInteract);

    __result = true;
    return false;
  }
}

[HarmonyPatch(typeof(BlockEntityGroundStorage), nameof(BlockEntityGroundStorage.putOrGetItemSingle))]
public static class BlockEntityGroundStorage_putOrGetItemSingle_Patch {
  public static bool Prefix(BlockEntityGroundStorage __instance, ref bool __result, ItemSlot ourSlot, IPlayer player, BlockSelection bs) {
    IWorldAccessor world = __instance.Api.World;
    BlockPos pos = __instance.Pos;

    if (ourSlot.Empty) {
      world.Logger.PickupDebug("Ignoring put/get item single due to storage being empty");
      return true;
    }

    // Forward interactions for contained interactables
    // e.g. removing food from a crock with a bowl
    if (ourSlot.Itemstack.Collectible is IContainedInteractable containedInteractable && containedInteractable.OnContainedInteractStart(__instance, ourSlot, player, bs)) {
      world.Logger.PickupDebug("Contained interactable handled put/get item single");
      BlockGroundStorage.IsUsingContainedBlock = true;
      __instance.SetField("isUsingSlot", ourSlot);
      __result = true;
      return false;
    }

    PickupArtistUtil.GiveToPlayer(world, player, pos, ourSlot.Itemstack);
    AssetLocation? sound = __instance.StorageProps?.PlaceRemoveSound;
    if (sound != null) world.PlaySoundAt(sound, pos.X + 0.5, pos.InternalY, pos.Z + 0.5, player, 0.88f + (float)world.Rand.NextDouble() * 0.24f, 16f);
    ourSlot.Itemstack = null;
    ourSlot.MarkDirty();
    __result = true;
    return false;
  }
}
