using System.Windows;
using System.IO;
using System.Text.Json;
using System.Windows.Media.Imaging;
using System.Windows.Input;


namespace ContadorCofres;

public partial class DropInfoWindow : Window
{
    private List<DropItem> _dropItems = new();

    public DropInfoWindow()
    {
        InitializeComponent();
        LoadDrops();
    }

    private void LoadDrops()
    {
        try
        {
            string dataDirectory = Path.GetFullPath(
                Path.Combine(AppContext.BaseDirectory, "Data"));
            string jsonPath = Path.Combine(dataDirectory, "drops.json");
            string json = File.ReadAllText(jsonPath);

            DropCatalog? catalog = JsonSerializer.Deserialize<DropCatalog>(json);

            if (catalog?.Items is null || catalog.Items.Count != 6)
                throw new InvalidDataException("El archivo debe contener exactamente seis objetos.");

            string dataDirectoryPrefix = dataDirectory + Path.DirectorySeparatorChar;

            foreach (DropItem item in catalog.Items)
            {
                if (string.IsNullOrWhiteSpace(item.Name))
                    throw new InvalidDataException("Cada objeto debe tener un nombre.");

                if (!IsValidDropRate(item.NormalDropChance) ||
                    (item.WeeklyDropChance is not null &&
                     !IsValidDropRate(item.WeeklyDropChance)))
                {
                    throw new InvalidDataException(
                        $"Las chances de «{item.Name}» no son válidas.");
                }

                item.ActiveDropChance = item.NormalDropChance;

                if (string.IsNullOrWhiteSpace(item.Image))
                    continue;

                if (Path.IsPathRooted(item.Image))
                    throw new InvalidDataException("Las rutas de imagen deben ser relativas a Data.");

                string imagePath = Path.GetFullPath(
                    Path.Combine(dataDirectory, item.Image.Replace('/', Path.DirectorySeparatorChar)));

                if (!imagePath.StartsWith(dataDirectoryPrefix, StringComparison.OrdinalIgnoreCase))
                    throw new InvalidDataException("La ruta de una imagen sale de la carpeta Data.");

                if (!File.Exists(imagePath))
                    throw new FileNotFoundException($"No se encontró la imagen de «{item.Name}».", imagePath);

                var picture = new BitmapImage();
                picture.BeginInit();
                picture.CacheOption = BitmapCacheOption.OnLoad;
                picture.UriSource = new Uri(imagePath, UriKind.Absolute);
                picture.EndInit();
                picture.Freeze();

                item.Picture = picture;
            }

            _dropItems = catalog.Items;
            DropItemsControl.ItemsSource = _dropItems;
        }
        catch (IOException ex)
        {
            ShowLoadError(ex.Message);
        }
        catch (UnauthorizedAccessException ex)
        {
            ShowLoadError(ex.Message);
        }
        catch (JsonException ex)
        {
            ShowLoadError(ex.Message);
        }
        catch (InvalidDataException ex)
        {
            ShowLoadError(ex.Message);
        }
        catch (ArgumentException ex)
        {
            ShowLoadError(ex.Message);
        }
        catch (NotSupportedException ex)
        {
            ShowLoadError(ex.Message);
        }
        catch (FormatException ex)
        {
            ShowLoadError(ex.Message);
        }
    }

    private void ShowLoadError(string details)
    {
        MessageBox.Show(
            this,
            $"No se pudo cargar la información de los objetos.\n{details}",
            "Error de datos",
            MessageBoxButton.OK,
            MessageBoxImage.Error);
    }

    private void WindowTitle_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        if (e.LeftButton == MouseButtonState.Pressed)
            DragMove();
    }

    private void CloseWindow_Click(object sender, RoutedEventArgs e)
    {
        Close();
    }

    private static bool IsValidDropRate(DropRate? rate)
    {
        return rate is not null &&
               rate.Numerator > 0 &&
               rate.Denominator > 0 &&
               rate.Numerator <= rate.Denominator;
    }

    private void WeeklyChancesCheckBox_Click(object sender, RoutedEventArgs e)
    {
        bool useWeekly = WeeklyChancesCheckBox.IsChecked == true;

        foreach (DropItem item in _dropItems)
        {
            item.ActiveDropChance =
                useWeekly && item.WeeklyDropChance is not null
                    ? item.WeeklyDropChance
                    : item.NormalDropChance;
        }
    }

}