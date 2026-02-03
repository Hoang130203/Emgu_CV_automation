namespace GameAutomation.Core.Models.Vision;

/// <summary>
/// Central registry of all image resources with their search regions
/// Consolidates scattered image definitions across workflows for centralized management
/// </summary>
public static class ImageResourceRegistry
{
    // Common search regions - reusable across multiple resources
    private static readonly SearchRegion DailyQuestRegion = new(1.0 / 4, 1.0 / 2, 9.5 / 10, 4.5 / 5);
    private static readonly SearchRegion FullScreenRegion = SearchRegion.FullScreen;

    /// <summary>
    /// All registered image resources keyed by their identifier
    /// Format: "flow/filename" -> ImageResourceDefinition
    /// </summary>
    public static readonly Dictionary<string, ImageResourceDefinition> Resources = new()
    {
        // ========== DAILY FLOW (ingame_daily) ==========
        // 01_ prefix - full screen (UI buttons at edge)
        ["daily/01_daily_openbutton"] = new("daily/01_daily_openbutton", "nth/ingame_daily/01_daily_openbutton.png"),

        // 02_-06_ prefix - use DailyQuestRegion (startX=1/3, startY=1/2, endX=9/10, endY=4/5)
        ["daily/02_daily_maintale"] = new("daily/02_daily_maintale", "nth/ingame_daily/02_daily_maintale.png", DailyQuestRegion),
        ["daily/02_daily_maintale_done"] = new("daily/02_daily_maintale_done", "nth/ingame_daily/02_daily_maintale_done.png", DailyQuestRegion),
        ["daily/03_daily_adventure"] = new("daily/03_daily_adventure", "nth/ingame_daily/03_daily_adventure.png", DailyQuestRegion),
        ["daily/03_daily_adventure_done"] = new("daily/03_daily_adventure_done", "nth/ingame_daily/03_daily_adventure_done.png", DailyQuestRegion),
        ["daily/04_daily_photograph"] = new("daily/04_daily_photograph", "nth/ingame_daily/04_daily_photograph.png", DailyQuestRegion),
        ["daily/04_daily_photograph_done"] = new("daily/04_daily_photograph_done", "nth/ingame_daily/04_daily_photograph_done.png", DailyQuestRegion),
        ["daily/05_daily_reset"] = new("daily/05_daily_reset", "nth/ingame_daily/05_daily_reset.png", DailyQuestRegion),
        ["daily/05_daily_reset_2"] = new("daily/05_daily_reset_2", "nth/ingame_daily/05_daily_reset_2.png", DailyQuestRegion),
        ["daily/06_daily_mhl"] = new("daily/06_daily_mhl", "nth/ingame_daily/06_daily_mhl.png", DailyQuestRegion),
        ["daily/06_daily_mhl_2"] = new("daily/06_daily_mhl_2", "nth/ingame_daily/06_daily_mhl_2.png", DailyQuestRegion),
        ["daily/06_daily_mhl_done"] = new("daily/06_daily_mhl_done", "nth/ingame_daily/06_daily_mhl_done.png", DailyQuestRegion),

        // 07_+ prefix - full screen
        ["daily/07_daily_dungoan"] = new("daily/07_daily_dungoan", "nth/ingame_daily/07_daily_dungoan.png"),
        ["daily/08_daily_f1"] = new("daily/08_daily_f1", "nth/ingame_daily/08_daily_f1.png"),

        // ========== CAMERA FLOW (ingame_camera) ==========
        ["camera/00_skiptalebutton"] = new("camera/00_skiptalebutton", "nth/ingame_camera/00_skiptalebutton.png"),
        ["camera/00_skiptalebutton2"] = new("camera/00_skiptalebutton2", "nth/ingame_camera/00_skiptalebutton2.png"),
        ["camera/00_skiptalebutton3"] = new("camera/00_skiptalebutton3", "nth/ingame_camera/00_skiptalebutton3.png"),
        ["camera/00_skiptalebutton4"] = new("camera/00_skiptalebutton4", "nth/ingame_camera/00_skiptalebutton4.png"),
        ["camera/00_skiptalebutton5"] = new("camera/00_skiptalebutton5", "nth/ingame_camera/00_skiptalebutton5.png"),
        ["camera/01_menubutton"] = new("camera/01_menubutton", "nth/ingame_camera/01_menubutton.png"),
        ["camera/02_camerabutton"] = new("camera/02_camerabutton", "nth/ingame_camera/02_camerabutton.png"),
        ["camera/03_camera_enterbutton"] = new("camera/03_camera_enterbutton", "nth/ingame_camera/03_camera_enterbutton.png"),
        ["camera/04_camera_skipbutton"] = new("camera/04_camera_skipbutton", "nth/ingame_camera/04_camera_skipbutton.png"),
        ["camera/04_camera_skipbutton_2"] = new("camera/04_camera_skipbutton_2", "nth/ingame_camera/04_camera_skipbutton_2.png"),
        ["camera/05_camera_closeimage"] = new("camera/05_camera_closeimage", "nth/ingame_camera/05_camera_closeimage.png"),
        ["camera/06_camera_backbutton"] = new("camera/06_camera_backbutton", "nth/ingame_camera/06_camera_backbutton.png"),

        // ========== DUNGOAN FLOW (ingame_dungoan) ==========
        ["dungoan/01_dungoan_skipbutton"] = new("dungoan/01_dungoan_skipbutton", "nth/ingame_dungoan/01_dungoan_skipbutton.png"),
        ["dungoan/02_dungoan_maintale"] = new("dungoan/02_dungoan_maintale", "nth/ingame_dungoan/02_dungoan_maintale.png"),
        ["dungoan/03_dungoan_startevent"] = new("dungoan/03_dungoan_startevent", "nth/ingame_dungoan/03_dungoan_startevent.png"),
        ["dungoan/04_dungoan_nameevent"] = new("dungoan/04_dungoan_nameevent", "nth/ingame_dungoan/04_dungoan_nameevent.png"),
        ["dungoan/05_dungoan_godungeon"] = new("dungoan/05_dungoan_godungeon", "nth/ingame_dungoan/05_dungoan_godungeon.png"),
        ["dungoan/06_dungoan_skip"] = new("dungoan/06_dungoan_skip", "nth/ingame_dungoan/06_dungoan_skip.png"),
        ["dungoan/07_dungoan_reborn"] = new("dungoan/07_dungoan_reborn", "nth/ingame_dungoan/07_dungoan_reborn.png"),
        ["dungoan/08_dungoan_doneevent"] = new("dungoan/08_dungoan_doneevent", "nth/ingame_dungoan/08_dungoan_doneevent.png"),
        ["dungoan/09_dungoan_closeevent"] = new("dungoan/09_dungoan_closeevent", "nth/ingame_dungoan/09_dungoan_closeevent.png"),
        ["dungoan/10_dungoan_dailydone"] = new("dungoan/10_dungoan_dailydone", "nth/ingame_dungoan/10_dungoan_dailydone.png"),

        // ========== MAP FLOW (ingame_map) ==========
        ["map/02_map_zoomupbutton"] = new("map/02_map_zoomupbutton", "nth/ingame_map/02_map_zoomupbutton.png"),
        ["map/02_settingcombat_weapon"] = new("map/02_settingcombat_weapon", "nth/ingame_map/02_settingcombat_weapon.png"),
        ["map/03_map_skipinstruction"] = new("map/03_map_skipinstruction", "nth/ingame_map/03_map_skipinstruction.png"),
        ["map/04_map_inputbutton"] = new("map/04_map_inputbutton", "nth/ingame_map/04_map_inputbutton.png"),
        ["map/05_map_follow"] = new("map/05_map_follow", "nth/ingame_map/05_map_follow.png"),
        ["map/06_map_zindex555"] = new("map/06_map_zindex555", "nth/ingame_map/06_map_zindex555.png"),
        ["map/07_map_ruong"] = new("map/07_map_ruong", "nth/ingame_map/07_map_ruong.png"),
        ["map/07_map_ruong2"] = new("map/07_map_ruong2", "nth/ingame_map/07_map_ruong2.png"),
        ["map/map_0"] = new("map/map_0", "nth/ingame_map/map_0.png"),
        ["map/map_1"] = new("map/map_1", "nth/ingame_map/map_1.png"),
        ["map/map_2"] = new("map/map_2", "nth/ingame_map/map_2.png"),
        ["map/map_3"] = new("map/map_3", "nth/ingame_map/map_3.png"),
        ["map/map_4"] = new("map/map_4", "nth/ingame_map/map_4.png"),
        ["map/map_5"] = new("map/map_5", "nth/ingame_map/map_5.png"),
        ["map/map_6"] = new("map/map_6", "nth/ingame_map/map_6.png"),
        ["map/map_7"] = new("map/map_7", "nth/ingame_map/map_7.png"),
        ["map/map_8"] = new("map/map_8", "nth/ingame_map/map_8.png"),
        ["map/map_9"] = new("map/map_9", "nth/ingame_map/map_9.png"),
        ["map/map_confirm"] = new("map/map_confirm", "nth/ingame_map/map_confirm.png"),
        ["map/map_delete"] = new("map/map_delete", "nth/ingame_map/map_delete.png"),

        // ========== MONGHOALUC FLOW (ingame_monghoaluc) ==========
        ["mhl/01_mhl_mailbox"] = new("mhl/01_mhl_mailbox", "nth/ingame_monghoaluc/01_mhl_mailbox.png"),
        ["mhl/01_mhl_mailbox2"] = new("mhl/01_mhl_mailbox2", "nth/ingame_monghoaluc/01_mhl_mailbox2.png"),
        ["mhl/02_mhl_backbuttonn"] = new("mhl/02_mhl_backbuttonn", "nth/ingame_monghoaluc/02_mhl_backbuttonn.png"),
        ["mhl/03_mhl_posttab"] = new("mhl/03_mhl_posttab", "nth/ingame_monghoaluc/03_mhl_posttab.png"),
        ["mhl/04_mhl_discover"] = new("mhl/04_mhl_discover", "nth/ingame_monghoaluc/04_mhl_discover.png"),
        ["mhl/05_mhl_like"] = new("mhl/05_mhl_like", "nth/ingame_monghoaluc/05_mhl_like.png"),

        // ========== SETTING COMBAT FLOW (ingame_settingcombat) ==========
        ["combat/01_settingcombat_searchbutton"] = new("combat/01_settingcombat_searchbutton", "nth/ingame_settingcombat/01_settingcombat_searchbutton.png"),
        ["combat/02_settingcombat_automedicine"] = new("combat/02_settingcombat_automedicine", "nth/ingame_settingcombat/02_settingcombat_automedicine.png"),
        ["combat/03_settingcombat_usemedicine"] = new("combat/03_settingcombat_usemedicine", "nth/ingame_settingcombat/03_settingcombat_usemedicine.png"),
        ["combat/04_settingcombat_skip"] = new("combat/04_settingcombat_skip", "nth/ingame_settingcombat/04_settingcombat_skip.png"),
        ["combat/05_settingcombat_openauto"] = new("combat/05_settingcombat_openauto", "nth/ingame_settingcombat/05_settingcombat_openauto.png"),
        ["combat/06_settingcombat_quickswap"] = new("combat/06_settingcombat_quickswap", "nth/ingame_settingcombat/06_settingcombat_quickswap.png"),

        // ========== SIGNIN FLOW (signin) ==========
        ["signin/01_usernamebox"] = new("signin/01_usernamebox", "nth/signin/01_usernamebox.png"),
        ["signin/02_passwordbox"] = new("signin/02_passwordbox", "nth/signin/02_passwordbox.png"),
        ["signin/03_loginbutton"] = new("signin/03_loginbutton", "nth/signin/03_loginbutton.png"),
        ["signin/04_checkbox"] = new("signin/04_checkbox", "nth/signin/04_checkbox.png"),
        ["signin/05_agree"] = new("signin/05_agree", "nth/signin/05_agree.png"),
        ["signin/05_closeposter"] = new("signin/05_closeposter", "nth/signin/05_closeposter.png"),
        ["signin/06_startbutton"] = new("signin/06_startbutton", "nth/signin/06_startbutton.png"),
        ["signin/06_ready"] = new("signin/06_ready", "nth/signin/06_ready.png"),
        ["signin/07_backbutton"] = new("signin/07_backbutton", "nth/signin/07_backbutton.png"),
        ["signin/08_startbutton2"] = new("signin/08_startbutton2", "nth/signin/08_startbutton2.png"),

        // ========== SIGNOUT FLOW (signout) ==========
        ["signout/01_signout_setting"] = new("signout/01_signout_setting", "nth/signout/01_signout_setting.png"),
        ["signout/02_signout_signout"] = new("signout/02_signout_signout", "nth/signout/02_signout_signout.png"),
        ["signout/03_signout_signout_button"] = new("signout/03_signout_signout_button", "nth/signout/03_signout_signout_button.png"),
        ["signout/04_signout_otheraccount"] = new("signout/04_signout_otheraccount", "nth/signout/04_signout_otheraccount.png"),
        ["signout/05_signout_zingsignin"] = new("signout/05_signout_zingsignin", "nth/signout/05_signout_zingsignin.png"),
    };

    /// <summary>
    /// Get resource definition by key
    /// </summary>
    public static ImageResourceDefinition? Get(string key) =>
        Resources.TryGetValue(key, out var resource) ? resource : null;

    /// <summary>
    /// Get search region for a template filename
    /// Returns null if not found (will use full screen)
    /// </summary>
    public static SearchRegion? GetRegionByFileName(string fileName)
    {
        // Extract just filename without path
        var name = Path.GetFileNameWithoutExtension(fileName);

        // Find matching resource by filename
        foreach (var kvp in Resources)
        {
            var resourceFileName = Path.GetFileNameWithoutExtension(kvp.Value.RelativePath);
            if (resourceFileName.Equals(name, StringComparison.OrdinalIgnoreCase))
            {
                return kvp.Value.Region;
            }
        }

        return null;
    }

    /// <summary>
    /// Check if a filename should use limited region search based on prefix pattern
    /// For files starting with 02_, 03_, 04_, 05_, 06_ in daily flow
    /// </summary>
    public static SearchRegion? GetRegionByPrefix(string fileName, string flowName)
    {
        var name = Path.GetFileNameWithoutExtension(fileName);

        // Apply DailyQuestRegion for 02_-06_ prefix in daily flow
        if (flowName.Equals("daily", StringComparison.OrdinalIgnoreCase) ||
            flowName.Contains("ingame_daily", StringComparison.OrdinalIgnoreCase))
        {
            if (name.StartsWith("02_") || name.StartsWith("03_") ||
                name.StartsWith("04_") || name.StartsWith("05_") ||
                name.StartsWith("06_"))
            {
                return DailyQuestRegion;
            }
        }

        return null;
    }
}
