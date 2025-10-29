# PickupArtist - Fast And Easy Transfers

Picking things up and putting them down is now much more convenient!

- You can add to and remove from piles and crates without the relevant item being in your active hotbar slot
- You no longer need an empty hand to pick rocks and sticks up from the ground

> [!WARNING]
> I haven't had much time to keep up this mod up to date, so the current prerelease still has [several outstanding issues](#known-issues) and [reported conflicts](#reported-conflicts).
>
> Please feel free to make a fork or open a pull request if you want to take a stab at the code yourself!

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

### Known Issues

- Crash caused by NullReferenceException when adding one stone to a full stone pile
  - Does not happen when adding two or more stones to a full stone pile
  - Does not happen when adding one stick to a full stick pile
- Some piles should not be stackable across multiple vertical blocks (stick and stone piles)
- Some piles seem like they are not affected by the mod at all (coal piles)

### Partial Compatibility

- [Carry On](https://mods.vintagestory.at/carryon) - Carrying a crate takes priority over placing items into it when both hands are empty

### Known Conflicts

- [Crateful](https://mods.vintagestory.at/crateful) - Pickup Artist should offer the similar capabilities

### Reported Conflicts

- [Fix Crate](https://github.com/mass8326/vintagestory-pickupartist/issues/5) - Both mods attempt to patch the same code
- [Primitive Survival](https://github.com/mass8326/vintagestory-pickupartist/issues/1) - Fishing line and other interactions seem to be broken
- [Salty's Manual Scraping](https://github.com/mass8326/vintagestory-pickupartist/issues/4) - Pickup conflicts with using a knife

## Acknowledgements

Thank you to everyone who helps make this mod worth the time and effort!

- [SparksSkywere](https://github.com/SparksSkywere) - for [contributing code](https://github.com/mass8326/vintagestory-pickupartist/pull/7) that fixed some NullReferenceExceptions
- [DanaCraluminum](https://github.com/Craluminum2413) - for providing code from their [ExtraInfo](https://github.com/Craluminum-Mods/ExtraInfo) mod under an open source license
- [SalieriC](https://github.com/SalieriC) - for giving me the [gentle nudge](https://github.com/mass8326/vintagestory-nohands/issues/1) to make this mod

## Contributing

This repository is set up to easily launch and debug in [VSCode](https://code.visualstudio.com/). Use an entry from the "Run And Debug" pane to test your code. If you just want to build the mod, you can use "Tasks: Run Task" from the command palette.

Make sure to follow the [Vintage Story modding guide](https://wiki.vintagestory.at/Modding:Preparing_For_Code_Mods) to make sure the proper .NET SDK is installed and your `$VINTAGE_STORY` environment variable is set up.
