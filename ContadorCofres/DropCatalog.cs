using System.Text.Json.Serialization;
using System.Windows.Media.Imaging;
using System.ComponentModel;

namespace ContadorCofres;

internal sealed class DropCatalog
{
    [JsonPropertyName("items")]
    public List<DropItem> Items { get; set; } = new();
}

internal sealed class DropItem : INotifyPropertyChanged
{
    private DropRate? _activeDropChance;

    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("normalDropChance")]
    public DropRate? NormalDropChance { get; set; }

    [JsonPropertyName("weeklyDropChance")]
    public DropRate? WeeklyDropChance { get; set; }

    [JsonPropertyName("image")]
    public string? Image { get; set; }

    [JsonIgnore]
    public BitmapImage? Picture { get; set; }

    [JsonIgnore]
    public DropRate? ActiveDropChance
    {
        get => _activeDropChance;
        set
        {
            if (ReferenceEquals(_activeDropChance, value))
                return;

            _activeDropChance = value;
            PropertyChanged?.Invoke(
                this,
                new PropertyChangedEventArgs(nameof(ProbabilityText)));
        }
    }

    [JsonIgnore]
    public string ProbabilityText
    {
        get
        {
            if (ActiveDropChance is not { Denominator: > 0 } chance)
                return "Probabilidad pendiente";

            double percent = (double)chance.Numerator / chance.Denominator * 100;
            return $"{chance.Numerator}/{chance.Denominator} ({percent:0.######}%)";
        }
    }

    public event PropertyChangedEventHandler? PropertyChanged;
}

internal sealed class DropRate
{
    [JsonPropertyName("numerator")]
    public int Numerator { get; set; }

    [JsonPropertyName("denominator")]
    public int Denominator { get; set; }
}