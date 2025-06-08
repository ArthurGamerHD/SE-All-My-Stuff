
# All My Stuff (version 1.6.0)
*A script to keep track of your inventory*

Configure using Custom Data. In the Programmable Block, the Inventory section
sets the options while the Translation section provides alternative labels. Put an
Inventory section into any screen's Custom Data to activate it, which can be empty.

~~Arthur maintains a fork for use with the Colorful Icons mod [here](https://github.com/ArthurGamerHD/SE-All-My-Stuff/tree/Colorful-Icons).~~ (You are already here)
Original script without the Colorful Icons patch [Here](https://github.com/Brianetta/SE-All-My-Stuff)

Here are the defaults:

```ini
[inventory]
# Set this to change the update frequency.
delay=3
# Use this to control a display for output (they start at 0)
# For example, use this on a cockpit. The order is the same
# as in the cockpit's terminal menu.
display=0
# Make things on the display larger or smaller
scale = 1.0
# Use this on a second screen if the lines don't all fit on the first, to skip lines
skip = 0
# Use this to customise the header color of a screen
color = 00FF55
# change to true to display numbers in the monospace font
mono = false
# Change to true to stop showing lines with zero stock levels
suppress_zeros = false
# Enable filtering
enablefilter = true
filter = ConsumableItem, Datapad, PhysicalGunObject, AmmoMagazine, Ore, Ingot, Component
# Enable saved game persistence
savetypes = true
```

An alternative, for those who wish to use more than one screen on a given
block at once, is to configure displays in the following manner (this example
works on the Sci-Fi Button Panel):

```ini
[inventory_display0]
scale=0.4

[inventory_display1]
scale=0.4
skip=5

[inventory_display2]
scale=0.4
skip=10

[inventory_display3]
scale=0.4
skip=15
```

You can set up a filter by adding a filter option. The options in the example above are all the valid ones, and you can have any selection that you want. As with colour, scale and skip, the filter is a per-screen option.

To re-scan for new inventories and reload the configuration, run with the argument `rebuild`

To clear any items from the inventory display that have a zero count, run with the argument `clear`

You can configure translations in the Custom Data section of the Programmable Block (this is global). This allows you to change the labels from the default internals. It might harm performance; removing the configuration will disable it.

Here is a useful translation configuration to use. It can be pasted into the Custom Data, before or after the rest of the configuration:

```ini
[Translation]
Component/BulletproofGlass=Bulletproof Glass
Component/Construction=Construction Comp.
Component/Detector=Detector Comp.
Component/EngineerPlushie=Engineer Plushie
Component/GravityGenerator=Gravity Comp.
Component/InteriorPlate=Interior Plate
Component/LargeTube=Large Steel Tube
Component/Medical=Medical Comp.
Component/MetalGrid=Metal Grid
Component/PowerCell=Power Cell
Component/RadioCommunication=Radio-comm Comp.
Component/Reactor=Reactor Comp.
Component/SabiroidPlushie=Saberoid Plushie
Component/SmallTube=Small Steel Tube
Component/SolarCell=Solar Cell
Component/SteelPlate=Steel Plate
Component/Thrust=Thruster Comp.
Component/ZoneChip=Zone Chip
Ingot/Cobalt=Cobalt Ingot
Ingot/Gold=Gold Ingot
Ingot/Stone=Gravel
Ingot/Iron=Iron Ingot
Ingot/Magnesium=Magnesium Powder
Ingot/Nickel=Nickel Ingot
Ingot/Scrap=Old Scrap Metal
Ingot/Platinum=Platinum Ingot
Ingot/Silicon=Silicon Wafer
Ingot/Silver=Silver Ingot
Ingot/Uranium=Uranium Ingot
Ore/Cobalt=Cobalt Ore
Ore/Gold=Gold Ore
Ore/Iron=Iron Ore
Ore/Magnesium=Magnesium Ore
Ore/Nickel=Nickel Ore
Ore/Platinum=Platinum Ore
Ore/Scrap=Scrap Metal
Ore/Silicon=Silicon Ore
Ore/Silver=Silver Ore
Ore/Uranium=Uranium Ore
ConsumableItem/ClangCola=Clang Kola
ConsumableItem/CosmicCoffee=Cosmic Coffee
PhysicalObject/SpaceCredit=Space Credit
PhysicalGunObject/AngleGrinder4Item=Elite Grinder
PhysicalGunObject/HandDrill4Item=Elite Hand Drill
PhysicalGunObject/Welder4Item=Elite Welder
PhysicalGunObject/AngleGrinder2Item=Enhanced Grinder
PhysicalGunObject/HandDrill2Item=Enhanced Hand Drill
PhysicalGunObject/Welder2Item=Enhanced Welder
PhysicalGunObject/AngleGrinderItem=Grinder
PhysicalGunObject/HandDrillItem=Hand Drill
GasContainerObject/HydrogenBottle=Hydrogen Bottle
PhysicalGunObject/AutomaticRifleItem=MR-20 Rifle
PhysicalGunObject/UltimateAutomaticRifleItem=MR-30E Rifle
PhysicalGunObject/RapidFireAutomaticRifleItem=MR-50A Rifle
PhysicalGunObject/PreciseAutomaticRifleItem=MR-8P Rifle
OxygenContainerObject/OxygenBottle=Oxygen Bottle
PhysicalGunObject/AdvancedHandHeldLauncherItem=PRO-1 Rocket Launcher
PhysicalGunObject/AngleGrinder3Item=Proficient Grinder
PhysicalGunObject/HandDrill3Item=Proficient Hand Drill
PhysicalGunObject/Welder3Item=Proficient Welder
PhysicalGunObject/BasicHandHeldLauncherItem=RO-1 Rocket Launcher
PhysicalGunObject/SemiAutoPistolItem=S-10 Pistol
PhysicalGunObject/ElitePistolItem=S-10E Pistol
PhysicalGunObject/FullAutoPistolItem=S-20A Pistol
PhysicalGunObject/WelderItem=Welder
AmmoMagazine/NATO_5p56x45mm=5.56x45mm NATO magazine
AmmoMagazine/LargeCalibreAmmo=Artillery Shell
AmmoMagazine/MediumCalibreAmmo=Assault Cannon Shell
AmmoMagazine/AutocannonClip=Autocannon Magazine
AmmoMagazine/NATO_25x184mm=Gatling Ammo Box
AmmoMagazine/LargeRailgunAmmo=Large Railgun Sabot
AmmoMagazine/AutomaticRifleGun_Mag_20rd=MR-20 Rifle Magazine
AmmoMagazine/UltimateAutomaticRifleGun_Mag_30rd=MR-30E Rifle Magazine
AmmoMagazine/RapidFireAutomaticRifleGun_Mag_50rd=MR-50A Rifle Magazine
AmmoMagazine/PreciseAutomaticRifleGun_Mag_5rd=MR-8P Rifle Magazine
AmmoMagazine/Missile200mm=Rocket
AmmoMagazine/SemiAutoPistolMagazine=S-10 Pistol Magazine
AmmoMagazine/ElitePistolMagazine=S-10E Pistol Magazine
AmmoMagazine/FullAutoPistolMagazine=S-20A Pistol Magazine
AmmoMagazine/SmallRailgunAmmo=Small Railgun Sabot
```

