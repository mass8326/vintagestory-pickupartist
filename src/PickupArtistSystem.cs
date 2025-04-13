using System.Runtime.CompilerServices;
using HarmonyLib;
using Vintagestory.API.Common;
using Vintagestory.API.Datastructures;
using Vintagestory.GameContent;

namespace PickupArtist;

public class PickupArtistSystem : ModSystem {
  public Harmony Harmony = new("pickupartist");

  // Building in release configuration will break the mod without this
  [MethodImpl(MethodImplOptions.NoOptimization)]
  public override void Start(ICoreAPI api) {
    base.Start(api);
    api.RegisterBlockBehaviorClass("RightClickPickup", typeof(BlockBehaviorRightClickPickup));
    Harmony.PatchAll();
  }

  public override void Dispose() {
    Harmony.UnpatchAll();
    base.Dispose();
  }
}

public class BlockBehaviorRightClickPickup : BlockBehavior {
  bool BlockIsPickupable;
  AssetLocation? SoundEffectLocation;

  public BlockBehaviorRightClickPickup(Block block) : base(block) { }

  public override void Initialize(JsonObject properties) {
    base.Initialize(properties);

    BlockIsPickupable = properties["dropsPickupMode"].AsBool(false);

    string? soundProperty =
      properties["sound"].AsString() ??
      block.Attributes?["placeSound"].AsString();
    if (soundProperty != null) SoundEffectLocation = AssetLocation.Create(soundProperty, block.Code.Domain);
  }

  public override bool OnBlockInteractStart(IWorldAccessor world, IPlayer player, BlockSelection selection, ref EnumHandling handling) {
    handling = EnumHandling.PreventDefault;
    if (player.Entity.Controls.ShiftKey || !world.Claims.TryAccess(player, selection.Position, EnumBlockAccessFlags.BuildOrBreak))
      return false;

    // This default value is needed for lamps, buckets, etc
    ItemStack[] dropStacks = new ItemStack[] { block.OnPickBlock(world, selection.Position) };
    if (BlockIsPickupable) {
      float dropMultiplier =
        block.Attributes?.IsTrue("forageStatAffected") == true
          ? player.Entity.Stats.GetBlended("forageDropRate")
          : 1f;
      dropStacks = block.GetDrops(world, selection.Position, player, dropMultiplier);
    }
    if (dropStacks.Length == 0) return false;

    if (world.Side == EnumAppSide.Server && BlockBehaviorReinforcable.AllowRightClickPickup(world, selection.Position, player)) {
      PickupArtistUtil.GiveToPlayer(world, player, selection.Position, dropStacks);
      world.BlockAccessor.SetBlock(0, selection.Position);
      world.BlockAccessor.TriggerNeighbourBlockUpdate(selection.Position);
      world.PlaySoundAt(SoundEffectLocation ?? block.GetSounds(world.BlockAccessor, selection).Place, player, null);
    }
    return true;
  }
}
