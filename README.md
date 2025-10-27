# Fast And Easy Transfers

Picking things up and putting them down is now much more convenient!

- You can add to and remove from piles and crates without the relevant item being in your active hotbar slot
- You no longer need an empty hand to pick rocks and sticks up from the ground

> [!NOTE]
> I haven't had much time to keep up this mod up to date with every game update. Please feel free to make a fork or open a pull request if you want to take a stab at the code yourself!

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

### Known Issues

- Client and server desyncs where item counts are inaccurate
- Some piles should not be stackable across multiple vertical blocks (sticks, stones)

## Technical Info

### Partial Compatibility

- [Carry On](https://mods.vintagestory.at/carryon) - Carrying a crate takes priority over placing items into it when both hands are empty

### Known Conflicts

- [Crateful](https://mods.vintagestory.at/crateful) - Pickup Artist should offer the similar capabilities

### Reported Conflicts

- [Fix Crate](https://github.com/mass8326/vintagestory-pickupartist/issues/5) - Both mods attempt to patch the same code
- [MorePiles](https://github.com/mass8326/vintagestory-pickupartist/issues/6) - Crashes occur during some edge cases
- [Primitive Survival](https://github.com/mass8326/vintagestory-pickupartist/issues/1) - Fishing line and other interactions seem to be broken
- [Salty's Manual Scraping](https://github.com/mass8326/vintagestory-pickupartist/issues/4) - Pickup conflicts with using a knife

## Acknowledgements

Many thanks to DanaCraluminum! They've made a ton of great mods and have provided them under an open source license. I've used one of their utilities from [ExtraInfo](https://github.com/Craluminum-Mods/ExtraInfo) to make this.

Thanks to [SalieriC](https://github.com/mass8326/vintagestory-nohands/issues/1) as well for giving me the gentle nudge to make this mod.

## Contributing

This repository is set up to easily launch and debug in [VSCode](https://code.visualstudio.com/). Use an entry from the "Run And Debug" pane to test your code.

Make sure to follow the [Vintage Story modding guide](https://wiki.vintagestory.at/Modding:Preparing_For_Code_Mods) to make sure the proper .NET SDK is installed and your `$VINTAGE_STORY` environment variable is set up.
