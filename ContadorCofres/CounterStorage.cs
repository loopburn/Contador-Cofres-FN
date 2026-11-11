using System.IO;

namespace ContadorCofres;

internal static class CounterStorage
{
    private static readonly string DataDirectory = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "ContadorCofres");

    private static readonly string CounterFilePath =
        Path.Combine(DataDirectory, "contador.txt");

    private static readonly string HotkeyFilePath =
        Path.Combine(DataDirectory, "atajos.txt");

    public static string? LoadCounter()
    {
        try
        {
            return File.ReadAllText(CounterFilePath);
        }
        catch (FileNotFoundException)
        {
            return null;
        }
        catch (DirectoryNotFoundException)
        {
            return null;
        }
    }

    public static void SaveCounter(int count)
    {
        Directory.CreateDirectory(DataDirectory);
        File.WriteAllText(CounterFilePath, count.ToString());
    }

    public static string[]? LoadHotkeys()
    {
        try
        {
            return File.ReadAllLines(HotkeyFilePath);
        }
        catch (FileNotFoundException)
        {
            return null;
        }
        catch (DirectoryNotFoundException)
        {
            return null;
        }
    }

    public static void SaveHotkeys(string plusKey, string minusKey)
    {
        Directory.CreateDirectory(DataDirectory);
        File.WriteAllLines(HotkeyFilePath, new[] { plusKey, minusKey });
    }
}