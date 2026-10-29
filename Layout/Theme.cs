using MudBlazor;

namespace Split.Layout;

public static class Theme
{
    public static readonly MudTheme FirstTheme = new()
    {
    PaletteLight = new PaletteLight()
    {
        Primary = "#4f46e5",
        PrimaryContrastText = "#ffffff",

        Secondary = "#0ea5e9",
        SecondaryContrastText = "#ffffff",

        Success = "#22c55e",
        Info = "#0284c7",
        Warning = "#f59e0b",
        Error = "#ef4444",

        Dark = "#111827",

        TextPrimary = "#0f172a",
        TextSecondary = "#64748b",

        Background = "#f8fafc",    // sehr clean
        Surface = "#ffffff",

        DrawerBackground = "#ffffff",
        DrawerText = "#334155",

        AppbarBackground = "#ffffff",
        AppbarText = "#0f172a"
    },

    PaletteDark = new PaletteDark()
    {
        Primary = "#818cf8",
        PrimaryContrastText = "#0b1020",

        Secondary = "#38bdf8",
        SecondaryContrastText = "#0b1020",

        Success = "#4ade80",
        Info = "#38bdf8",
        Warning = "#fbbf24",
        Error = "#f87171",

        Dark = "#020617",

        TextPrimary = "#e2e8f0",
        TextSecondary = "#94a3b8",

        Background = "#0f172a",
        Surface = "#111827",

        DrawerBackground = "#111827",
        DrawerText = "#cbd5f5",

        AppbarBackground = "#111827",
        AppbarText = "#e2e8f0"
    },

    Typography = new Typography()
    {
        Default = new DefaultTypography()
        {
            FontFamily = new[] { "Inter", "Roboto", "Helvetica", "Arial", "sans-serif" },
            FontSize = "0.95rem"
        },

        H1 = new H1Typography() { FontWeight = "700" },
        H2 = new H2Typography() { FontWeight = "700" },
        H3 = new H3Typography() { FontWeight = "600" },
        H4 = new H4Typography() { FontWeight = "600" },
        H5 = new H5Typography() { FontWeight = "500" },
        H6 = new H6Typography() { FontWeight = "500" },

        Button = new ButtonTypography()
        {
            TextTransform = "none", // kein ALL CAPS → moderner
            FontWeight = "500"
        }
    },

    Shadows = new Shadow()
    {
        Elevation = new[]
        {
            "none",
            "0px 1px 2px rgba(0,0,0,0.05)",
            "0px 1px 3px rgba(0,0,0,0.08)",
            "0px 4px 6px rgba(0,0,0,0.10)",
            "0px 10px 15px rgba(0,0,0,0.12)",
            "0px 20px 25px rgba(0,0,0,0.15)"
        }
    },

    ZIndex = new ZIndex()
    {
        Drawer = 1100,
        AppBar = 1200,
        Dialog = 1300,
        Snackbar = 1400,
        Tooltip = 1500
    },

    LayoutProperties = new LayoutProperties()
    {
        DefaultBorderRadius = "10px"
    }
};

    /// <summary>Standard-Theme der App (Orange).</summary>
    public static readonly MudTheme Split = new()
    {
        PaletteLight = new PaletteLight()
        {
            Primary = "#F97316",
            PrimaryContrastText = "#ffffff",
            Secondary = "#EA580C",
            SecondaryContrastText = "#ffffff",
            Success = "#16A34A",
            Info = "#0EA5E9",
            Warning = "#F59E0B",
            Error = "#DC2626",
            Dark = "#1F2937",
            TextPrimary = "#1F2937",
            TextSecondary = "#6B7280",
            Background = "#F8FAFC",
            Surface = "#FFFFFF",
            DrawerBackground = "#FFFFFF",
            DrawerText = "#374151",
            DrawerIcon = "#6B7280",
            AppbarBackground = "#FFFFFF",
            AppbarText = "#1F2937",
            LinesDefault = "#E5E7EB"
        },

        PaletteDark = new PaletteDark()
        {
            Primary = "#FB923C",
            PrimaryContrastText = "#1C1917",
            Secondary = "#FDBA74",
            SecondaryContrastText = "#1C1917",
            Success = "#4ADE80",
            Info = "#38BDF8",
            Warning = "#FBBF24",
            Error = "#F87171",
            Dark = "#030712",
            TextPrimary = "#F3F4F6",
            TextSecondary = "#D1D5DB",
            Background = "#111827",
            Surface = "#1F2937",
            DrawerBackground = "#1F2937",
            DrawerText = "#F3F4F6",
            DrawerIcon = "#D1D5DB",
            AppbarBackground = "#111827",
            AppbarText = "#F3F4F6",
            LinesDefault = "#374151"
        },

        Typography = new Typography()
        {
            Default = new DefaultTypography()
            {
                FontFamily = ["Inter", "Roboto", "Helvetica", "Arial", "sans-serif"]
            },
            H4 = new H4Typography() { FontWeight = "700" },
            H5 = new H5Typography() { FontWeight = "600" },
            H6 = new H6Typography() { FontWeight = "600" },
            Button = new ButtonTypography()
            {
                TextTransform = "none",
                FontWeight = "600"
            }
        },

        LayoutProperties = new LayoutProperties()
        {
            DefaultBorderRadius = "12px"
        }
    };
}