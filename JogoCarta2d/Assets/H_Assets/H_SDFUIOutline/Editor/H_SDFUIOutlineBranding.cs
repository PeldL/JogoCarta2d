#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

/// <summary>Branded header and support footer shared by the H SDF UI Outline inspector.</summary>
internal static class H_SDFUIOutlineBranding
{
    const string DiscordUrl = "https://dsc.gg/hworksinteractive";
    const string SupportEmail = "assetsupport@hworksinteractive.com";

    const float MaxBannerWidth = 430f;
    const float SupportIconSize = 34f;

    static Texture2D s_productBanner;
    static Texture2D s_footerBanner;
    static Texture2D s_emailIcon;
    static Texture2D s_discordIcon;
    static GUIStyle s_supportLabel;
    static GUIStyle s_iconButton;
    static bool s_resolved;

    static void EnsureResources()
    {
        if (!s_resolved)
        {
            s_resolved = true;
            s_productBanner = Resources.Load<Texture2D>("h_sdf_ui_outline_banner_PNG_spr");
            s_footerBanner = Resources.Load<Texture2D>("h_assets_h_works_interactive_footer_PNG_spr");
            s_emailIcon = Resources.Load<Texture2D>("email_ico_PNG_spr");
            s_discordIcon = Resources.Load<Texture2D>("discord_ico_PNG_spr");
        }

        if (s_supportLabel == null)
        {
            s_supportLabel = new GUIStyle(EditorStyles.label)
            {
                alignment = TextAnchor.MiddleCenter,
                fontStyle = FontStyle.Bold,
                wordWrap = true
            };
        }

        if (s_iconButton == null)
        {
            s_iconButton = new GUIStyle(GUIStyle.none) { imagePosition = ImagePosition.ImageOnly };
        }
    }

    /// <summary>Product banner, centred at the top of the inspector.</summary>
    public static void DrawHeader()
    {
        EnsureResources();

        EditorGUILayout.Space(6);
        DrawCenteredBanner(s_productBanner);
        EditorGUILayout.Space(4);
    }

    static void DrawCenteredBanner(Texture2D banner)
    {
        if (banner == null)
            return;

        // Aspect comes from the texture itself so any banner size fits without stretching or letterboxing.
        float aspect = banner.height > 0 ? banner.width / (float)banner.height : 1f;
        float width = Mathf.Min(EditorGUIUtility.currentViewWidth - 40f, MaxBannerWidth);
        float height = width / aspect;
        Rect row = GUILayoutUtility.GetRect(0f, height, GUILayout.ExpandWidth(true));
        GUI.DrawTexture(new Rect(row.x + (row.width - width) * 0.5f, row.y, width, height), banner, ScaleMode.ScaleToFit);
    }

    /// <summary>Studio banner plus the support links, drawn at the bottom of the inspector.</summary>
    public static void DrawFooter()
    {
        EnsureResources();

        EditorGUILayout.Space(12);
        DrawSeparator();
        EditorGUILayout.Space(10);

        DrawCenteredBanner(s_footerBanner);

        EditorGUILayout.Space(8);
        EditorGUILayout.LabelField("Have feedback or need support?", s_supportLabel);
        EditorGUILayout.Space(4);

        bool changed = GUI.changed;
        using (new EditorGUILayout.HorizontalScope())
        {
            GUILayout.FlexibleSpace();
            DrawLinkButton(s_emailIcon, "Email " + SupportEmail, () => Application.OpenURL("mailto:" + SupportEmail + "?subject=H%20SDF%20UI%20Outline"));
            GUILayout.Space(14);
            DrawLinkButton(s_discordIcon, "Join the H Works Interactive Discord", () => Application.OpenURL(DiscordUrl));
            GUILayout.FlexibleSpace();
        }
        GUI.changed = changed;

        EditorGUILayout.Space(6);
    }

    static void DrawLinkButton(Texture2D icon, string tooltip, System.Action onClick)
    {
        if (icon == null)
        {
            if (GUILayout.Button(new GUIContent("?", tooltip), GUILayout.Width(SupportIconSize), GUILayout.Height(SupportIconSize)))
                onClick();
            return;
        }

        Rect rect = GUILayoutUtility.GetRect(SupportIconSize, SupportIconSize, GUILayout.Width(SupportIconSize), GUILayout.Height(SupportIconSize));
        EditorGUIUtility.AddCursorRect(rect, MouseCursor.Link);

        Color tint = GUI.color;
        if (rect.Contains(Event.current.mousePosition))
            GUI.color = new Color(tint.r, tint.g, tint.b, tint.a * 0.75f);

        if (GUI.Button(rect, new GUIContent(icon, tooltip), s_iconButton))
            onClick();

        GUI.color = tint;
    }

    static void DrawSeparator()
    {
        Rect rect = GUILayoutUtility.GetRect(0f, 1f, GUILayout.ExpandWidth(true));
        EditorGUI.DrawRect(rect, EditorGUIUtility.isProSkin ? new Color(1f, 1f, 1f, 0.09f) : new Color(0f, 0f, 0f, 0.16f));
    }
}
#endif
