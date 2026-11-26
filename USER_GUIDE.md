# Kerbal Logistics Program (KLP)
## User Guide | Version 1.0

### 1. Introduction

Kerbal Logistics Program provides a lightweight logistics system for Kerbal Space Program.

Rather than repeatedly flying supply missions between established bases and stations, KLP allows you to deposit resources into virtual storage, ship those resources between locations, and withdraw them when needed.

You still need to establish your infrastructure, produce your resources and provide appropriate storage aboard your vessels. KLP handles the repetitive transportation portion of that process.

### 2. Installation

1. Download the latest release from the GitHub Releases page.
2. Extract the downloaded ZIP archive.
3. Copy the included GameData folder into your Kerbal Space Program installation directory.
4. Launch Kerbal Space Program.

Your installation should contain the following file:

```
Kerbal Space Program/
  GameData/
    KerbalLogisticsProgram/
      KerbalLogisticsProgram.dll
```

KLP does not require ModuleManager, Harmony or any other external mod.

It can be installed into an existing save or used with a new game. Backing up your save before installing any new mod is recommended.

### 3. Establishing a logistics station

KLP uses the stock KSP vessel classification system to determine which vessels can access the logistics network.

To establish a logistics station:

1. Launch or select an existing vessel.
2. Place the vessel in orbit or land it at your desired location.
3. Open the stock vessel rename or classification interface.
4. Change its vessel type to **Station**.
5. Click the KLP icon in the stock application toolbar.

KLP will automatically recognize the station and determine its mailbox location.

A planetary base must also be classified as Station, even if its physical purpose is a mining facility, refinery or surface colony.

No special KLP parts, docking ports or resource containers are required.

### 4. Understanding mailboxes

A mailbox is a persistent virtual resource inventory associated with a location rather than an individual vessel.

Locations are identified using the celestial body and biome.

Examples:

- Kerbin:Orbit
- Mun:Orbit
- Mun:Midlands
- Minmus:Greater Flats
- Dres:Highlands

All qualifying stations in the same location share the same mailbox.

For example, two stations located in orbit around Kerbin share the Kerbin:Orbit mailbox, regardless of their altitude, inclination or physical distance from one another.

Two surface stations in the same Minmus biome also share a mailbox, even if they are physically separated.

Stations in different biomes have separate mailboxes. Orbital and surface mailboxes are also separate.

#### Mailbox properties

- Mailboxes have no gameplay-defined storage capacity limit.
- Mailboxes can hold multiple resource types simultaneously.
- Mailbox contents persist when saving and loading a game.
- Each saved game has its own independent mailbox inventories.
- Mailbox contents are not tied to the survival of an individual station.

If the last station at a location is removed or reclassified, its mailbox inventory remains intact. Establishing another qualifying station at that location restores access to the stored resources.

### 5. Opening the KLP interface

Click the KLP icon in the stock KSP application toolbar while in Flight.

The KLP window contains two primary tabs:

- **Mailbox:** Transfer resources between your active station and its local mailbox.
- **Shipping:** Transfer resources between the local mailbox and another established mailbox.

The location header displays your current mailbox location and access status.

If the interface displays **Station access required**, verify that your active vessel is classified as Station and occupies a supported location.

#### General window controls

| Control | Function |
|---|---|
| KLP toolbar icon | Opens the interface |
| Mailbox tab | Displays local resource transfers |
| Shipping tab | Displays remote resource transfers |
| Refresh | Updates the displayed inventories and available destinations |
| X | Closes the window |
| Escape | Closes the window |
| Window title bar | Drag to reposition the window |
| Bottom-right corner | Drag to resize the window |

Normal game input is locked while the KLP window is open to help prevent accidental vessel commands.

### 6. The Mailbox tab

The Mailbox tab allows you to deposit resources from your active vessel into the local mailbox or withdraw resources from the mailbox into the vessel.

#### Resource inventory

The inventory table displays resources available aboard your vessel or stored in the local mailbox.

Select a resource by clicking its row.

The search field allows you to filter the displayed resources by name. Use the adjacent clear button to remove the search filter.

#### Resource information

After selecting a resource, the transfer panel displays:

- Current vessel inventory.
- Total vessel storage capacity.
- Remaining vessel storage capacity.
- Current local mailbox inventory.
- Requested transfer quantity.
- Projected inventory balances following a transfer.

A resource displaying **No storage** is present in the mailbox but has no compatible storage aboard the active vessel.

#### Depositing resources

1. Open the Mailbox tab.
2. Select the resource you want to deposit.
3. Enter the quantity in the amount field.
4. Review the projected vessel and mailbox balances.
5. Click Deposit.

The specified resources are removed from the active vessel and added to the local mailbox.

Use **All vessel** to automatically enter the full available vessel quantity.

#### Withdrawing resources

1. Open the Mailbox tab.
2. Select the resource you want to withdraw.
3. Enter the desired quantity.
4. Review the projected inventory balances.
5. Click Withdraw.

The resources are removed from the local mailbox and placed into compatible storage aboard the active vessel.

Use **Max withdrawal** to automatically select the maximum quantity that can be withdrawn, based on both mailbox inventory and available vessel storage.

#### Partial transfers

If the requested quantity exceeds the available resource inventory or remaining vessel storage capacity, KLP calculates the maximum valid transfer.

The transaction preview and button label display the actual quantity that will be transferred.

For example, requesting 1,000 units when your vessel has only 600 units of free storage will result in a withdrawal of 600 units, provided the mailbox contains enough resources.

Invalid or unavailable transactions are disabled.

### 7. The Shipping tab

The Shipping tab allows you to transfer resources from your local mailbox to another established location.

Shipping operates between mailboxes, not directly between vessels.

Resources must first be deposited into the origin mailbox before they can be shipped.

#### Shipping resources

1. Open KLP from an established station.
2. Select the Shipping tab.
3. Select a destination using the destination mailbox dropdown.
4. Select the resource you want to ship.
5. Enter the requested shipment quantity.
6. Review the projected origin and destination inventories.
7. Click Ship.

The resources are removed from the origin mailbox and added to the destination mailbox.

Use **All origin inventory** to automatically enter the entire available quantity of the selected resource.

The resource table displays the quantities available in both the origin and destination mailboxes.

#### Receiving shipments

There is no separate shipment acceptance process.

Once a shipment is completed, its resources are available in the destination mailbox.

To retrieve them:

1. Switch to a qualifying station at the destination.
2. Open KLP.
3. Select the Mailbox tab.
4. Select the delivered resource.
5. Withdraw the required quantity.

The destination vessel must have compatible storage to receive the resources.

### 8. Example: Shipping Ore from Minmus to Kerbin

Consider a mining station on Minmus and an orbital processing station around Kerbin.

Both vessels must be classified as Station.

#### At the Minmus mining station

1. Produce 1,000 units of Ore.
2. Open the KLP Mailbox tab.
3. Select Ore.
4. Enter 1,000 and click Deposit.
5. Open the Shipping tab.
6. Select Kerbin:Orbit as the destination.
7. Select Ore.
8. Enter 1,000 and click Ship.

#### At the Kerbin orbital station

1. Switch to the orbital station.
2. Open the KLP Mailbox tab.
3. Select Ore.
4. Enter the desired withdrawal quantity or use Max withdrawal.
5. Click Withdraw.

The Ore is now available aboard the processing station.

No additional cargo vessel or repetitive transfer mission is required.

### 9. Using KLP with other mods

KLP supports resources provided by stock KSP and other installed mods.

A modded mining facility can deposit its produced resources into a mailbox, ship them to another location, and supply a remote manufacturing facility using the same interface.

KLP does not replace resource production, converters, drills, manufacturing systems or life support mechanics.

Resources must still be produced by your existing equipment, and withdrawals require compatible vessel storage.

No specific resource production or colony management mod is required.

### 10. Gameplay mechanics and limitations

KLP version 1.0 intentionally abstracts resource transportation.

The following rules apply:

- Shipping is instantaneous.
- Shipping does not consume fuel, funds or transportation credits.
- There are no scheduled cargo flights or simulated transit times.
- There is no distance-based shipping cost.
- There is no mailbox storage capacity limit imposed by gameplay.
- Shipping destinations require an established qualifying station.
- Resources are not automatically deposited, shipped or withdrawn.
- KLP is designed for single-player gameplay.

The system is intended for players who enjoy building industrial and logistical infrastructure but do not wish to repeatedly fly routine supply missions.

Players may establish their own transportation rules or restrictions to suit their preferred gameplay experience.

### 11. Troubleshooting

**KLP displays Station access required.**

Verify that your active vessel is classified as Station and is in a supported orbital or surface location.

**My destination does not appear in the dropdown.**

Verify that a qualifying station exists at the destination. Click Refresh to update the available destinations. The current mailbox location cannot be selected as a shipping destination.

**I cannot deposit a resource.**

Verify that the active vessel contains the selected resource and that you have entered a valid positive quantity.

**I cannot withdraw a resource.**

Verify that the mailbox contains the resource and that the active vessel has compatible storage with available capacity.

**I cannot ship a resource.**

Verify that the origin mailbox contains the resource, a valid destination is selected and the requested quantity is positive.

**A resource disappeared from the displayed inventory.**

Check the search filter. Clear the filter and click Refresh. Also verify that you are accessing the intended celestial body and biome.

**My base was destroyed or reclassified. Are my stored resources lost?**

No. Mailbox inventories are associated with locations. Establish another qualifying station at the same location to regain access.

### 12. Reporting issues

Please report reproducible issues through the GitHub Issues page.

Include the following information where possible:

- KLP version.
- KSP version.
- Other relevant installed mods.
- Steps required to reproduce the issue.
- Expected and actual behavior.
- Relevant screenshots or KSP.log entries.

Repository:

https://github.com/VillageId10t/KerbalLogisticsProgram

### 13. License

Kerbal Logistics Program is distributed under the MIT License.

See the LICENSE file included in the repository for full licensing terms.