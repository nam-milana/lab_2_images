namespace ImageProcessor.Models;

/// <summary>
/// Хранит итоговые показатели одного запуска обработки изображений.
/// </summary>
public class ProcessingResult
{
    /// <summary>Название режима, например "Однопоточный" или "Многопоточный".</summary>
    public string ModeName { get; init; } = string.Empty;

    /// <summary>Общее время обработки всех изображений в миллисекундах.</summary>
    public long ElapsedMs { get; init; }

    /// <summary>Количество обработанных изображений.</summary>
    public int ImageCount { get; init; }

    /// <summary>Среднее время на одно изображение в миллисекундах.</summary>
    public double AverageMs => ImageCount > 0 ? (double)ElapsedMs / ImageCount : 0;
}
