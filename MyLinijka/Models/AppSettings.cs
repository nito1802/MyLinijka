using System.IO;
using System.Text.Json;
using System.Windows;
using System.Windows.Media;

namespace MyLinijka.Models;

public sealed class AppSettings
{
    public string LineFill { get; set; } = "#FF98BFF7";
    public string LineStroke { get; set; } = "#FF000000";
    public double LineThickness { get; set; } = 1;
    public string RectFill { get; set; } = "#FF618DCD";
    public string RectStroke { get; set; } = "#FF000000";
    public double RectThickness { get; set; } = 1;
    public double StartX { get; set; }
    public double StartY { get; set; }
    public double EndX { get; set; }
    public double EndY { get; set; }
    public double Length { get; set; }
    public double Angle { get; set; }
    public double Width { get; set; }
    public double Height { get; set; }
    public double PanelLeft { get; set; } = 200;
    public double PanelTop { get; set; }
    public bool RectangleSelected { get; set; }
    public bool ClickThrough { get; set; }

    public static string FilePath => Path.Combine(AppContext.BaseDirectory, "settings.json");

    public static AppSettings Load()
    {
        if (!File.Exists(FilePath)) return new();
        var settings = JsonSerializer.Deserialize<AppSettings>(File.ReadAllText(FilePath))
            ?? throw new JsonException("Plik ustawień jest pusty.");
        // Validate the complete snapshot before applying any part of it.
        ParseBrush(settings.LineFill);
        ParseBrush(settings.LineStroke);
        ParseBrush(settings.RectFill);
        ParseBrush(settings.RectStroke);
        double[] numbers = [settings.LineThickness, settings.RectThickness, settings.StartX,
            settings.StartY, settings.EndX, settings.EndY, settings.Length, settings.Angle,
            settings.Width, settings.Height, settings.PanelLeft, settings.PanelTop];
        if (numbers.Any(value => !double.IsFinite(value)) || settings.LineThickness < 0 ||
            settings.RectThickness < 0 || settings.Length < 0 || settings.Width < 0 || settings.Height < 0)
            throw new JsonException("Nieprawidłowe wartości ustawień.");
        return settings;
    }

    public void Apply(DrawOptionsModel options)
    {
        options.LineFill = ParseBrush(LineFill);
        options.LineStroke = ParseBrush(LineStroke);
        options.LineThickness = LineThickness;
        options.RectFill = ParseBrush(RectFill);
        options.RectStroke = ParseBrush(RectStroke);
        options.RectThickness = RectThickness;
        var stats = options.StatsDataContext;
        stats.StartPoint = new Point(StartX, StartY);
        stats.EndPoint = new Point(EndX, EndY);
        stats.LengthLine = Length;
        stats.Angle = Angle;
        stats.Width = Width;
        stats.Height = Height;
    }

    public static AppSettings Capture(DrawOptionsModel options)
    {
        var stats = options.StatsDataContext;
        return new()
        {
            LineFill = ((SolidColorBrush)options.LineFill).Color.ToString(),
            LineStroke = ((SolidColorBrush)options.LineStroke).Color.ToString(),
            LineThickness = options.LineThickness,
            RectFill = ((SolidColorBrush)options.RectFill).Color.ToString(),
            RectStroke = ((SolidColorBrush)options.RectStroke).Color.ToString(),
            RectThickness = options.RectThickness,
            StartX = stats.StartPoint.X, StartY = stats.StartPoint.Y,
            EndX = stats.EndPoint.X, EndY = stats.EndPoint.Y,
            Length = stats.LengthLine, Angle = stats.Angle,
            Width = stats.Width, Height = stats.Height
        };
    }

    public void Save()
    {
        string json = JsonSerializer.Serialize(this, new JsonSerializerOptions { WriteIndented = true });
        string temporaryPath = FilePath + ".tmp";
        File.WriteAllText(temporaryPath, json);
        File.Move(temporaryPath, FilePath, overwrite: true);
    }

    private static SolidColorBrush ParseBrush(string color)
    {
        if (string.IsNullOrWhiteSpace(color)) throw new JsonException("Brak koloru.");
        try { return new SolidColorBrush((Color)ColorConverter.ConvertFromString(color)); }
        catch (Exception error) when (error is FormatException or ArgumentException or NotSupportedException)
        { throw new JsonException("Nieprawidłowy kolor.", error); }
    }
}
