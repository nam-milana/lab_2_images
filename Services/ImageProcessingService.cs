using System.Diagnostics;
using System.Windows.Media.Imaging;
using ImageProcessor.Models;

namespace ImageProcessor.Services;

/// <summary>
/// Обрабатывает набор изображений в однопоточном или многопоточном режиме.
/// Возвращает сырые байты (<see cref="ProcessedImage"/>) — без WPF-объектов,
/// чтобы вся тяжёлая работа была безопасна в фоновых потоках.
/// </summary>
public class ImageProcessingService
{
    /// <summary>
    /// Обрабатывает список изображений последовательно в одном потоке.
    /// </summary>
    public static (ProcessingResult Result, List<ProcessedImage> Images) RunSingleThread(
        IReadOnlyList<BitmapSource> sources,
        IProgress<int>? progress = null
    )
    {
        var images = new List<ProcessedImage>(sources.Count);
        var sw = Stopwatch.StartNew();

        for (int i = 0; i < sources.Count; i++)
        {
            images.Add(SepiaFilter.Apply(sources[i]));
            progress?.Report(i + 1);
        }

        sw.Stop();

        var result = new ProcessingResult
        {
            ModeName = "Однопоточный",
            ElapsedMs = sw.ElapsedMilliseconds,
            ImageCount = sources.Count,
        };

        return (result, images);
    }

    /// <summary>
    /// Обрабатывает список изображений параллельно через <see cref="Parallel.For"/>.
    /// Количество потоков определяется автоматически по числу ядер процессора.
    /// </summary>
    public (ProcessingResult Result, List<ProcessedImage> Images) RunMultiThread(
        IReadOnlyList<BitmapSource> sources,
        IProgress<int>? progress = null
    )
    {
        var images = new ProcessedImage[sources.Count];
        int processed = 0;

        var sw = Stopwatch.StartNew();

        Parallel.For(
            0,
            sources.Count,
            i =>
            {
                images[i] = SepiaFilter.Apply(sources[i]);

                // Interlocked — атомарный инкремент, безопасен из нескольких потоков
                int count = Interlocked.Increment(ref processed);
                progress?.Report(count);
            }
        );

        sw.Stop();

        var result = new ProcessingResult
        {
            ModeName = "Многопоточный",
            ElapsedMs = sw.ElapsedMilliseconds,
            ImageCount = sources.Count,
        };

        return (result, images.ToList());
    }
}
