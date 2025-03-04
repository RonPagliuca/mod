# Pants Mod for SPTarkov

## Overview

Pants Mod is a plugin for SPTarkov, a single-player mod for Escape From Tarkov. This mod enhances the gameplay experience by adding dynamic enemy markers on the screen, which display the role of AI enemies. The markers adjust their color, size and transparency based on the distance from the player, providing a clear and informative visual aid during gameplay.

## Features

- **Dynamic Enemy Markers**: Displays markers for AI enemies on the screen.
- **Distance-Based Scaling**: Marker size adjusts based on the distance to the enemy.
- **Distance-Based Transparency and Color**: Marker transparency adjusts based on the distance to the enemy.
- **Stats Display**: Shows the role of the enemy (e.g., Scav, Raider), distance, hp, and level on the marker.
- **Toggle Effect**: Using ctrl-shift-m turn toggle markers off and on.

## Installation

1. **Download the Mod**: Download the latest release of Pants Mod from the [Download Pants Mod](./PantsMod.zip).
2. **Unzip the Files**: Unzip the downloaded files into the root of `SPTarkov` folder of your SPTarkov installation.
3. **Start the Game**: Launch SPTarkov, and the mod will be loaded automatically.

## Bugs

## How It Works

### Initialization

When the mod is loaded, it creates a new GameObject called `PantsModUpdater` and attaches a `PantsModUpdater` component to it. This GameObject is set to persist across scene loads.

### Enemy Marker Updates

The `PantsModUpdater` component continuously updates the enemy markers every frame:

1. **Check Game State**: It first checks if the game world and the main player are available.
2. **Update Markers**: It then updates the enemy markers:
   - **Deactivate All Markers**: Deactivates all existing enemy markers.
   - **Find Enemies**: Finds all alive AI enemies in the game world.
   - **Calculate Positions**: Calculates the screen position for each enemy based on their head position.
   - **Create Markers**: Creates a new marker if one does not already exist for the enemy.
   - **Adjust Marker Properties**: Adjusts the size, transparency, and text of the marker based on the distance to the enemy.

### UI Canvas

The mod attaches the enemy markers to an existing UI canvas in the game. If no suitable canvas is found, it creates a temporary canvas for the markers.

## Logging

The mod logs important events to a log file located at `.\PantsMod.log`. This includes messages such as when the mod is loaded and when the updater starts.

## Contributing

If you would like to contribute to the development of Pants Mod, please fork the repository and submit a pull request with your changes. All contributions are welcome!

## License

This project is licensed under the MIT License. See the [LICENSE](LICENSE) file for details.

## Contact

For any questions or support, please open an issue on the [GitHub repository](#).
