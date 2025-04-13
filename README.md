# Fast And Easy Transfers

Picking things up and putting them down is now much more convenient!

- You can add to and remove from piles and crates without the relevant item being in your active hotbar slot
- You no longer need an empty hand to pick rocks and sticks up from the ground

## Less Hotbar Management

I've also made tweaks to how items are transferred to and from your inventory. This is an attempt to reduce the amount of hotbar management you need to do.

Items you place into piles or crates will be drawn from your inventory in following order:

1. Active hotbar slot (if possible)
1. Smallest backpack stack
1. Smallest hotbar stack

Items you pick up will fill your inventory in the following order:

1. Smallest partial hotbar stack
1. Active hotbar slot (if possible)
1. Smallest partial backpack stack
1. Empty backpack slot
1. Empty hotbar slot

For reference, vanilla will fill your hotbar left-to-right first.

Don't have control of the server? Want even more convenience when it comes to managing your inventory? Try my client-side only inventory mod: [NoHands](https://github.com/mass8326/vintagestory-nohands)

## Technical Info

This mod is required on both the server and client to work properly. Should work fine with any .net 7 build.

Fully compatible with:

- [Auto Map Markers](https://mods.vintagestory.at/show/mod/797) - Picking up ores will now trigger automatic marker creation (PickupArtist v0.2.0+)

Partially compatible with:

- [Carry On](https://mods.vintagestory.at/carryon) - Carrying a crate takes priority over placing items into it when both hands are empty

Incompatible with:

- [Crateful](https://mods.vintagestory.at/crateful) - I love Crateful and used it a ton before making this mod! PickupArtist offers tweaked take (not just put) behavior and aims to reduce hotbar management as well.
- [Primitive Survival](https://github.com/mass8326/vintagestory-pickupartist/issues/1) - Fishing line and other interactions seem to be broken

Future plans:

- Apply the tweaked behavior to tool racks ~~and non-pile ground storages~~
- Faster adding of grass, sticks, and fuel to pit kilns

This has been my most complicated work in C# so far. Please let me know if you run into any bugs by creating an issue.

Many thanks to DanaCraluminum! They've made a ton of great mods and have provided them under an open source license. I've used one of their utilities from [ExtraInfo](https://github.com/Craluminum-Mods/ExtraInfo) to make this.

Thanks to [SalieriC](https://github.com/mass8326/vintagestory-nohands/issues/1) as well for giving me the gentle nudge to make this mod.
