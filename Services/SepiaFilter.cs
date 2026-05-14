using System.Windows.Media;
using System.Windows.Media.Imaging;
using ImageProcessor.Models;

namespace ImageProcessor.Services;

/// <summary>
/// Применяет сепия-фильтр к изображению.
/// Формула: newR = 0.393R + 0.769G + 0.189B
///           newG = 0.349R + 0.686G + 0.168B
///           newB = 0.272R + 0.534G + 0.131B
/// </summary>
public static class SepiaFilter
{
    /// <summary>
    /// Применяет сепию к изображению и возвращает сырые байты пикселей.
    /// Не создаёт никаких WPF-объектов — безопасно вызывать из любого потока.
    /// </summary>
    /// <param name="source">Исходное изображение (должно быть Frozen).</param>
    /// <returns><see cref="ProcessedImage"/> с обработанными пикселями.</returns>
    public static ProcessedImage Apply(BitmapSource source)
    {
        // FormatConvertedBitmap можно использовать в фоновом потоке,
        // если source заморожен через Freeze() при загрузке
        var converted = new FormatConvertedBitmap(source, PixelFormats.Bgra32, null, 0);

        int width = converted.PixelWidth;
        int height = converted.PixelHeight;
        int stride = width * 4; // 4 байта: B G R A

        byte[] pixels = new byte[height * stride];
        converted.CopyPixels(pixels, stride, 0);

        ApplySepia(pixels);

        return new ProcessedImage
        {
            Pixels = pixels,
            Width = width,
            Height = height,
        };
    }

    /// <summary>
    /// Трансформирует массив байт пикселей (формат BGRA) по формуле сепии.
    /// Изменяет массив на месте.
    /// </summary>
    private static void ApplySepia(byte[] pixels)
    {
        for (int i = 0; i < pixels.Length; i += 4)
        {
            byte b = pixels[i];
            byte g = pixels[i + 1];
            byte r = pixels[i + 2];
            // pixels[i + 3] — alpha, не трогаем

            // Формула сепии; Clamp чтобы не выйти за [0, 255]
            pixels[i] = Clamp(0.272 * r + 0.534 * g + 0.131 * b); // newB
            pixels[i + 1] = Clamp(0.349 * r + 0.686 * g + 0.168 * b); // newG
            pixels[i + 2] = Clamp(0.393 * r + 0.769 * g + 0.189 * b); // newR
        }
    }

    /// <summary>
    /// Обрезает вещественное значение до диапазона байта [0, 255].
    /// </summary>
    private static byte Clamp(double value) => (byte)Math.Max(0, Math.Min(255, (int)value));
}
