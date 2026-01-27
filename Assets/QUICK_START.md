# Quick Start - Setting Up Game Assets

## Step-by-Step Guide

### 1. Identify What You Want to Automate

Before capturing images, plan what the bot should do:
- Click specific buttons?
- Detect UI changes?
- Monitor health bars?
- Find items on screen?
- Read text/numbers?

### 2. Capture Template Images

#### Method 1: Using Windows Snipping Tool
1. Open your game
2. Press `Win + Shift + S`
3. Select the UI element you want to detect
4. Save as PNG in `Assets/GameTemplates/[YourGame]/`

#### Method 2: Using the Bot
1. Start the bot
2. Click "Capture Screenshot"
3. Crop the specific element from the full screenshot
4. Save to the appropriate folder

### 3. Organize Your Assets

Create a folder structure like this:

```
Assets/GameTemplates/MyGame/
├── buttons/
│   ├── play_button.png          ← Exact screenshot of the play button
│   ├── pause_button.png
│   └── exit_button.png
├── ui/
│   ├── health_full.png          ← Different states of health
│   ├── health_low.png
│   └── inventory_icon.png
└── items/
    ├── gold_coin.png
    └── health_potion.png
```

### 4. Image Quality Tips

✅ **DO:**
- Use PNG format
- Capture at your game's native resolution
- Crop tightly around the element
- Save multiple variations if needed (different states)
- Use descriptive filenames

❌ **DON'T:**
- Use JPG (lossy compression)
- Include unnecessary background
- Mix resolutions
- Use vague names like "image1.png"

### 5. Example Asset Names

Good examples:
```
button_start_normal.png
button_start_hover.png
ui_healthbar_100.png
ui_healthbar_50.png
ui_healthbar_10.png
item_sword_legendary.png
enemy_boss_dragon.png
text_victory.png
```

### 6. Testing Your Templates

1. Open the WPF UI
2. Enter your game window title
3. Click "START BOT"
4. Check the "Vision and Detection" tab to see if templates are detected
5. View logs for detection confidence scores

### 7. AI Provider Setup

#### Option 1: No AI (EmguCV Only)
- **Cost:** Free
- **Speed:** Very fast
- **Best for:** Template matching, color detection
- **Setup:** None required

#### Option 2: ML.NET (Local AI)
- **Cost:** Free
- **Speed:** Fast
- **Best for:** Custom object classification
- **Setup:** Train model with TrainingData folder

#### Option 3: OpenAI GPT-4 Vision
- **Cost:** API usage charges
- **Speed:** ~2-5 seconds per request
- **Best for:** Complex scene understanding
- **Setup:** Get API key from https://platform.openai.com/

#### Option 4: Claude Vision
- **Cost:** API usage charges
- **Speed:** ~2-5 seconds per request
- **Best for:** Strategic decision making
- **Setup:** Get API key from https://console.anthropic.com/

## Common Workflows

### Example 1: Auto-Farming
1. Detect "Start Battle" button
2. Click it
3. Wait for "Victory" screen
4. Detect "Collect Rewards" button
5. Click it
6. Repeat

**Required Assets:**
```
buttons/start_battle.png
screens/victory.png
buttons/collect_rewards.png
```

### Example 2: Health Monitoring
1. Continuously detect health bar
2. If health < 30%, use health potion
3. Detect potion icon in inventory
4. Click it

**Required Assets:**
```
ui/health_bar_critical.png
items/health_potion_icon.png
```

### Example 3: Resource Collection
1. Detect resource nodes (trees, rocks, etc.)
2. Click to collect
3. Wait for animation
4. Repeat until inventory full

**Required Assets:**
```
resources/tree.png
resources/rock.png
ui/inventory_full.png
```

## Troubleshooting

### Template Not Detected
- Check image resolution matches game
- Ensure element is fully visible on screen
- Try adjusting confidence threshold (lower = more sensitive)
- Verify PNG format is used
- Check if UI has transparency issues

### False Positives
- Crop template more tightly
- Increase confidence threshold
- Add more context around the element
- Use AI detection for better accuracy

### Slow Performance
- Reduce screen capture frequency
- Use EmguCV instead of AI when possible
- Limit detection region to specific screen areas
- Optimize template image sizes

## Need Help?

1. Check the main README.md for detailed documentation
2. Review the logs in the WPF application
3. Test templates individually before combining workflows
4. Start simple and add complexity gradually

## Ready to Start?

1. Build the solution: `dotnet build`
2. Add your first template image to `Assets/GameTemplates/`
3. Run the WPF UI: `dotnet run --project src/UI/GameAutomation.UI.WPF/GameAutomation.UI.WPF.csproj`
4. Configure and START!

Happy Automating! 🎮🤖
