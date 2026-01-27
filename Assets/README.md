# Game Assets Folder

This folder contains all game-related images and templates used for computer vision and AI detection.

## Folder Structure

### GameTemplates/
Store template images that the bot will search for in the game.

**Naming Convention:**
- `[GameName]_[ElementName]_[State].png`
- Example: `MyGame_StartButton_Normal.png`
- Example: `MyGame_HealthBar_Full.png`

**Organization by Game:**
```
GameTemplates/
├── MyGame1/
│   ├── buttons/
│   │   ├── start_button.png
│   │   ├── pause_button.png
│   │   └── exit_button.png
│   ├── ui_elements/
│   │   ├── health_bar.png
│   │   └── energy_bar.png
│   └── items/
│       ├── sword.png
│       └── potion.png
└── MyGame2/
    └── ...
```

### Screenshots/
Runtime screenshots captured during bot execution for debugging and analysis.

**Auto-organized by date:**
```
Screenshots/
├── 2026-01-27/
│   ├── 14-30-25_game_state.png
│   └── 14-31-10_detected_button.png
└── ...
```

### TrainingData/
Images for training ML.NET models for custom game object detection.

**Organization:**
```
TrainingData/
├── labels.txt          # List of labels/categories
├── category1/
│   ├── image1.png
│   ├── image2.png
│   └── ...
├── category2/
│   └── ...
└── model/              # Saved ML models
    └── game_classifier.zip
```

## Supported Formats
- PNG (recommended for transparency)
- JPG/JPEG
- BMP

## Tips
1. Use high-quality screenshots at the same resolution as your game
2. Template images should be exact matches (pixel-perfect)
3. Keep template images small (just the UI element, not the whole screen)
4. Name files descriptively for easy identification
