using System.IO;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Input;
using System.Windows.Interop;

namespace ContadorCofres;


public partial class MainWindow : Window{
    private static readonly string CounterDirectory = System.IO.Path.Combine(
    Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
    "ContadorCofres");
    private static readonly string CounterFilePath = System.IO.Path.Combine(CounterDirectory, "contador.txt");
    private static readonly string HotkeyFilePath =
    System.IO.Path.Combine(CounterDirectory, "atajos.txt");

    private HwndSource? _source;
    private bool _plusRegistered;
    private bool _minusRegistered;
    private const int WM_HOTKEY = 0x0312;
    private const int HOTKEY_PLUS_ID = 1;
    private const int HOTKEY_MINUS_ID = 2;

    private const uint MOD_NOREPEAT = 0x4000;

    private Key _plusKey = Key.Add;
    private Key _minusKey = Key.Subtract;

    [DllImport("user32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool RegisterHotKey(
        IntPtr hWnd, int id, uint fsModifiers, uint vk);

    [DllImport("user32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool UnregisterHotKey(IntPtr hWnd, int id);

    private int _chestCount = 0;


    private void IncrementCount()
    {
        AddToCounter(1);
    }

    private void DecrementCount()
    {
        AddToCounter(-1);
    }
        public MainWindow()
        {
            InitializeComponent();
            LoadCounter();
            LoadHotkeySettings();
            SourceInitialized += RegisterGlobalHotkeys;
            Closed += UnregisterGlobalHotkeys;  
        }

        private void RegisterGlobalHotkeys(object? sender, EventArgs e)
    {
        var hwnd = new WindowInteropHelper(this).Handle;
        _source = HwndSource.FromHwnd(hwnd);

        if (_source is null)
        {
            MessageBox.Show("No se pudo obtener el identificador de la ventana.");
            return;
        }

        _source.AddHook(HandleHotkeyMessage);

        TryRegisterHotkeys(_plusKey, _minusKey);
    }

    private IntPtr HandleHotkeyMessage(
        IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
    {
        if (msg == WM_HOTKEY)
        {
            switch (wParam.ToInt32())
            {
                case HOTKEY_PLUS_ID:
                    IncrementCount();
                    handled = true;
                    break;

                case HOTKEY_MINUS_ID:
                    DecrementCount();
                    handled = true;
                    break;
            }
        }

        return IntPtr.Zero;
    }

    private void UnregisterGlobalHotkeys(object? sender, EventArgs e)
    {
        UnregisterCurrentHotkeys();

        _source?.RemoveHook(HandleHotkeyMessage);
    }


        private void LoadCounter()
    {
        try
        {
            string savedValue = File.ReadAllText(CounterFilePath);

            if (int.TryParse(savedValue, out int count) && count >= 0)
            {
                _chestCount = count;
            }
            else
            {
                MessageBox.Show("El archivo guardado no contiene un contador válido. Se usará 0.");
            }
        }
        catch (FileNotFoundException)
        {
        }
        catch (DirectoryNotFoundException)
        {
        }
        catch (IOException ex)
        {
            MessageBox.Show($"No se pudo leer el contador guardado: {ex.Message}");
        }
        catch (UnauthorizedAccessException ex)
        {
            MessageBox.Show($"No se pudo acceder al contador guardado: {ex.Message}");
        }

        ChestCountText.Text = _chestCount.ToString();
    }

    private void SaveCounter()
    {
        try
        {
            Directory.CreateDirectory(CounterDirectory);
            File.WriteAllText(CounterFilePath, _chestCount.ToString());
        }
        catch (IOException ex)
        {
            MessageBox.Show($"No se pudo guardar el contador: {ex.Message}");
        }
        catch (UnauthorizedAccessException ex)
        {
            MessageBox.Show($"No se pudo acceder al archivo del contador: {ex.Message}");
        }
    }

    private SettingsWindow? _settingsWindow;

    private void OpenSettings_Click(object sender, RoutedEventArgs e)
    {
        UnregisterCurrentHotkeys();
        if (_settingsWindow is null || !_settingsWindow.IsVisible)
        {
            _settingsWindow = new SettingsWindow(
                ResetCounter,
                AddToCounter,
                ApplyHotkeys,
                _plusKey,
                _minusKey);

            _settingsWindow.Closed += (_, _) =>
            {
                if (!_plusRegistered && !_minusRegistered)
                    TryRegisterHotkeys(_plusKey, _minusKey);
            };

            _settingsWindow.Show();
        }
        else
        {
            _settingsWindow.Activate();
        }
    }

    private void ResetCounter()
    {
        _chestCount = 0;
        ChestCountText.Text = _chestCount.ToString();
        SaveCounter();
    }

    private bool TryRegisterHotkeys(Key plusKey, Key minusKey)
{
    if (_source is null)
        return false;

    if (plusKey == minusKey)
    {
        MessageBox.Show("Elegí teclas distintas para +1 y -1.");
        return false;
    }

    var hwnd = _source.Handle;
    uint plusVk = (uint)KeyInterop.VirtualKeyFromKey(plusKey);
    uint minusVk = (uint)KeyInterop.VirtualKeyFromKey(minusKey);

    if (plusVk == 0 || minusVk == 0)
    {
        MessageBox.Show("Una de las teclas seleccionadas no es válida.");
        return false;
    }

    _plusRegistered = RegisterHotKey(hwnd, HOTKEY_PLUS_ID, MOD_NOREPEAT, plusVk);
    if (!_plusRegistered)
    {
        MessageBox.Show(
            $"No se pudo registrar {plusKey}. Error de Windows: {Marshal.GetLastWin32Error()}");
        return false;
    }

    _minusRegistered = RegisterHotKey(hwnd, HOTKEY_MINUS_ID, MOD_NOREPEAT, minusVk);
    if (!_minusRegistered)
    {
        int error = Marshal.GetLastWin32Error();
        UnregisterHotKey(hwnd, HOTKEY_PLUS_ID);
        _plusRegistered = false;

        MessageBox.Show(
            $"No se pudo registrar {minusKey}. Error de Windows: {error}");
        return false;
    }

    _plusKey = plusKey;
    _minusKey = minusKey;
    return true;
}

private bool ApplyHotkeys(Key plusKey, Key minusKey)
{
    Key previousPlus = _plusKey;
    Key previousMinus = _minusKey;

    UnregisterCurrentHotkeys();

    if (TryRegisterHotkeys(plusKey, minusKey))
    {
        SaveHotkeySettings();
        return true;
    }

    TryRegisterHotkeys(previousPlus, previousMinus);
    return false;
}

private void UnregisterCurrentHotkeys()
{
    if (_source is null)
        return;

    if (_plusRegistered)
    {
        UnregisterHotKey(_source.Handle, HOTKEY_PLUS_ID);
        _plusRegistered = false;
    }

    if (_minusRegistered)
    {
        UnregisterHotKey(_source.Handle, HOTKEY_MINUS_ID);
        _minusRegistered = false;
    }
}

private void LoadHotkeySettings()
{
    try
    {
        string[] values = File.ReadAllLines(HotkeyFilePath);

        if (values.Length != 2 ||
            !Enum.TryParse<Key>(values[0], out Key plusKey) ||
            !Enum.TryParse<Key>(values[1], out Key minusKey) ||
            plusKey == minusKey ||
            !IsValidHotkeyKey(plusKey) ||
            !IsValidHotkeyKey(minusKey))
        {
            MessageBox.Show("La configuración de teclas no es válida. Se usarán las teclas predeterminadas.");
            return;
        }

        _plusKey = plusKey;
        _minusKey = minusKey;
    }
    catch (FileNotFoundException)
    {
    }
    catch (DirectoryNotFoundException)
    {
    }
    catch (IOException ex)
    {
        MessageBox.Show($"No se pudo leer la configuración de teclas: {ex.Message}");
    }
    catch (UnauthorizedAccessException ex)
    {
        MessageBox.Show($"No se pudo acceder a la configuración de teclas: {ex.Message}");
    }
}

private static bool IsValidHotkeyKey(Key key)
{
    return key != Key.None &&
           key != Key.System &&
           KeyInterop.VirtualKeyFromKey(key) != 0;
}

private void SaveHotkeySettings()
{
    try
    {
        Directory.CreateDirectory(CounterDirectory);
        File.WriteAllLines(HotkeyFilePath, new[]
        {
            _plusKey.ToString(),
            _minusKey.ToString()
        });
    }
    catch (IOException ex)
    {
        MessageBox.Show($"No se pudo guardar la configuración de teclas: {ex.Message}");
    }
    catch (UnauthorizedAccessException ex)
    {
        MessageBox.Show($"No se pudo acceder a la configuración de teclas: {ex.Message}");
    }
}
private void AddToCounter(int amount)
{
    long newCount = (long)_chestCount + amount;

    if (newCount > int.MaxValue)
    {
        MessageBox.Show("La cantidad excede el máximo permitido para el contador.");
        return;
    }

    if (newCount < 0)
        newCount = 0;

    if (newCount == _chestCount)
        return;

    _chestCount = (int)newCount;
    ChestCountText.Text = _chestCount.ToString();
    SaveCounter();
}
private void OverlayTitle_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
{
    if (e.LeftButton == MouseButtonState.Pressed)
        DragMove();
}
private void CloseOverlay_Click(object sender, RoutedEventArgs e)
{
    _settingsWindow?.Close();
    Close();
}
}