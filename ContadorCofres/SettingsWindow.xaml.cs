using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace ContadorCofres;

public partial class SettingsWindow : Window
{
    private readonly Action _resetCounter;
    
    private readonly Func<Key, Key, bool> _applyHotkeys;
    private readonly Action<int> _addCounter;

    public SettingsWindow(
        Action resetCounter,
        Action<int> addCounter,
        Func<Key, Key, bool> applyHotkeys,
        Key plusKey,
        Key minusKey)
    {
        InitializeComponent();

        _resetCounter = resetCounter;
        _addCounter = addCounter;
        _applyHotkeys = applyHotkeys;
        _plusKey = plusKey;
        _minusKey = minusKey;

        PlusKeyButton.Content = _plusKey.ToString();
        MinusKeyButton.Content = _minusKey.ToString();
    }

    private void ResetCounter_Click(object sender, RoutedEventArgs e)
{
    MessageBoxResult result = MessageBox.Show(
        this,
        "¿Seguro que querés reiniciar el contador? Esta acción no se puede deshacer.",
        "Confirmar reinicio",
        MessageBoxButton.YesNo,
        MessageBoxImage.Warning,
        MessageBoxResult.No);

    if (result == MessageBoxResult.Yes)
        _resetCounter();
}
    private Button? _captureTarget;
    private Key _plusKey = Key.Add;
    private Key _minusKey = Key.Subtract;

    private void CapturePlusKey_Click(object sender, RoutedEventArgs e)
    {
        _captureTarget = PlusKeyButton;
        PlusKeyButton.Content = "Presioná una tecla...";
    }

    private void CaptureMinusKey_Click(object sender, RoutedEventArgs e)
    {
        _captureTarget = MinusKeyButton;
        MinusKeyButton.Content = "Presioná una tecla...";
    }

    private void SettingsWindow_PreviewKeyDown(object sender, KeyEventArgs e)
    {
        if (_captureTarget is null)
            return;

        Key key = e.Key == Key.System ? e.SystemKey : e.Key;

        if (key == Key.Escape)
        {
            _captureTarget.Content =
                _captureTarget == PlusKeyButton ? "Numpad Add" : "Numpad Subtract";
            _captureTarget = null;
            e.Handled = true;
            return;
        }

        if (key is Key.LeftCtrl or Key.RightCtrl
            or Key.LeftAlt or Key.RightAlt
            or Key.LeftShift or Key.RightShift
            or Key.LWin or Key.RWin)
        {
            return;
        }

        if ((_captureTarget == PlusKeyButton && key == _minusKey)
            || (_captureTarget == MinusKeyButton && key == _plusKey))
        {
            MessageBox.Show("Elegí una tecla distinta para cada acción.");
            e.Handled = true;
            return;
        }

        if (_captureTarget == PlusKeyButton)
            _plusKey = key;
        else
            _minusKey = key;

        _captureTarget.Content = key.ToString();
        _captureTarget = null;
        e.Handled = true;
    }

    private void ApplyHotkeys_Click(object sender, RoutedEventArgs e)
    {
        if (_applyHotkeys(_plusKey, _minusKey))
            Close();
    }

    private void AddCustomAmount_Click(object sender, RoutedEventArgs e)
    {
        if (!int.TryParse(CustomAmountTextBox.Text, out int amount) || amount <= 0)
        {
            MessageBox.Show(
                this,
                "Ingresá un número entero mayor que cero.",
                "Cantidad inválida",
                MessageBoxButton.OK,
                MessageBoxImage.Warning);
            return;
        }

        _addCounter(amount);
        CustomAmountTextBox.Clear();
    }
}