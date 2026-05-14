namespace ImageProcessor.Models;

/// <summary>
/// Промежуточный результат обработки одного изображения.
/// Хранит только сырые байты и размеры — без WPF-объектов,
/// поэтому безопасно создаётся в любом потоке.
/// </summary>
public class ProcessedImage
{
    /// <summary>Пиксели в формате BGRA32.</summary>
    public byte[] Pixels { get; init; } = [];

    public int Width { get; init; }
    public int Height { get; init; }

    /// <summary>Stride = Width * 4 (4 байта на пиксель).</summary>
    public int Stride => Width * 4;
}
