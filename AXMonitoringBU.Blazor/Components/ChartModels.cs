namespace AXMonitoringBU.Blazor.Components;

public class ChartData
{
    public List<string> Labels { get; set; } = new();
    public List<ChartDataset> Datasets { get; set; } = new();
}

public class ChartDataset
{
    public string Label { get; set; } = string.Empty;
    public List<double> Data { get; set; } = new();
    public string? BackgroundColor { get; set; }
    public string? BorderColor { get; set; }
    public int BorderWidth { get; set; } = 1;
}

public class ChartOptions
{
    public bool Responsive { get; set; } = true;
    public ChartPlugins? Plugins { get; set; }
    public ChartScales? Scales { get; set; }
}

public class ChartPlugins
{
    public ChartLegend? Legend { get; set; }
    public ChartTitle? Title { get; set; }
}

public class ChartLegend
{
    public bool Display { get; set; } = true;
    public string Position { get; set; } = "top";
}

public class ChartTitle
{
    public bool Display { get; set; } = true;
    public string Text { get; set; } = string.Empty;
}

public class ChartScales
{
    public ChartAxis? Y { get; set; }
}

public class ChartAxis
{
    public bool BeginAtZero { get; set; } = true;
    public string? Max { get; set; }
}

