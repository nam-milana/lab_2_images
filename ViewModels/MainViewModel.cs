using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Media.Imaging;
using ImageProcessor.Models;
using ImageProcessor.Services;
using Microsoft.Win32;

namespace ImageProcessor.ViewModels;

/// <summary>
/// ViewModel главного окна.
/// Управляет загрузкой изображений, запуском обработки и хранением результатов.
/// </summary>
public class MainViewModel : BaseViewModel
{
    private readonly ImageProcessingService _processingService = new();

    // --- Состояние ---

    private bool _isBusy;

    /// <summary>true, когда идёт обработка. Используется для блокировки кнопок и показа прогресса.</summary>
    public bool IsBusy
    {
        get => _isBusy;
        set => SetField(ref _isBusy, value);
    }

    private int _progressValue;

    /// <summary>Текущий прогресс обработки (количество готовых изображений).</summary>
    public int ProgressValue
    {
        get => _progressValue;
        set => SetField(ref _progressValue, value);
    }

    private int _progressMax = 1;

    /// <summary>Максимальное значение прогресса (общее количество изображений).</summary>
    public int ProgressMax
    {
        get => _progressMax;
        set => SetField(ref _progressMax, value);
    }

    private string _statusText = "Загрузите изображения для начала работы";

    /// <summary>Строка статуса в нижней части окна.</summary>
    public string StatusText
    {
        get => _statusText;
        set => SetField(ref _statusText, value);
    }

    // --- Результаты ---

    private ProcessingResult? _singleResult;

    /// <summary>Результат однопоточного прогона.</summary>
    public ProcessingResult? SingleResult
    {
        get => _singleResult;
        set
        {
            SetField(ref _singleResult, value);
            OnPropertyChanged(nameof(SingleResultText));
            OnPropertyChanged(nameof(HasResults));
            OnPropertyChanged(nameof(SpeedupText));
        }
    }

    private ProcessingResult? _multiResult;

    /// <summary>Результат многопоточного прогона.</summary>
    public ProcessingResult? MultiResult
    {
        get => _multiResult;
        set
        {
            SetField(ref _multiResult, value);
            OnPropertyChanged(nameof(MultiResultText));
            OnPropertyChanged(nameof(HasResults));
            OnPropertyChanged(nameof(SpeedupText));
        }
    }

    /// <summary>Отформатированный текст результата для однопоточного режима.</summary>
    public string SingleResultText =>
        SingleResult is null
            ? "—"
            : $"{SingleResult.ElapsedMs} мс  (avg {SingleResult.AverageMs:F1} мс/img)";

    /// <summary>Отформатированный текст результата для многопоточного режима.</summary>
    public string MultiResultText =>
        MultiResult is null
            ? "—"
            : $"{MultiResult.ElapsedMs} мс  (avg {MultiResult.AverageMs:F1} мс/img)";

    /// <summary>true, когда оба результата получены — нужно для отображения итоговой панели.</summary>
    public bool HasResults => SingleResult is not null && MultiResult is not null;

    /// <summary>Текст с коэффициентом ускорения многопоточного режима над однопоточным.</summary>
    public string SpeedupText
    {
        get
        {
            if (SingleResult is null || MultiResult is null)
                return string.Empty;
            if (MultiResult.ElapsedMs == 0)
                return "Многопоточный режим отработал мгновенно";

            double speedup = (double)SingleResult.ElapsedMs / MultiResult.ElapsedMs;
            return speedup >= 1
                ? $"Многопоточный быстрее в {speedup:F2}×"
                : $"Однопоточный быстрее в {1 / speedup:F2}×  (накладные расходы потоков)";
        }
    }

    // --- Коллекции изображений ---

    /// <summary>Исходные загруженные изображения.</summary>
    public ObservableCollection<BitmapSource> SourceImages { get; } = [];

    /// <summary>Результаты после однопоточной обработки.</summary>
    public ObservableCollection<BitmapSource> SingleProcessedImages { get; } = [];

    /// <summary>Результаты после многопоточной обработки.</summary>
    public ObservableCollection<BitmapSource> MultiProcessedImages { get; } = [];

    // --- Команды ---

    /// <summary>Открыть диалог выбора изображений.</summary>
    public RelayCommand LoadImagesCommand { get; }

    /// <summary>Запустить однопоточную обработку.</summary>
    public RelayCommand RunSingleCommand { get; }

    /// <summary>Запустить многопоточную обработку.</summary>
    public RelayCommand RunMultiCommand { get; }

    /// <summary>Очистить всё и начать сначала.</summary>
    public RelayCommand ClearCommand { get; }

    public MainViewModel()
    {
        LoadImagesCommand = new RelayCommand(_ => LoadImages(), _ => !IsBusy);
        RunSingleCommand = new RelayCommand(
            _ => RunSingleAsync(),
            _ => !IsBusy && SourceImages.Count > 0
        );
        RunMultiCommand = new RelayCommand(
            _ => RunMultiAsync(),
            _ => !IsBusy && SourceImages.Count > 0
        );
        ClearCommand = new RelayCommand(_ => Clear(), _ => !IsBusy);
    }

    // --- Реализация команд ---

    /// <summary>
    /// Открывает диалог выбора файлов и загружает выбранные изображения в коллекцию.
    /// </summary>
    private void LoadImages()
    {
        var dialog = new OpenFileDialog
        {
            Title = "Выберите изображения",
            Filter = "Изображения|*.jpg;*.jpeg;*.png;*.bmp;*.tiff",
            Multiselect = true,
        };

        if (dialog.ShowDialog() != true)
            return;

        SourceImages.Clear();
        SingleProcessedImages.Clear();
        MultiProcessedImages.Clear();
        SingleResult = null;
        MultiResult = null;

        foreach (string path in dialog.FileNames)
        {
            try
            {
                var bitmap = new BitmapImage(new Uri(path));
                bitmap.Freeze(); // Freeze позволяет передавать bitmap между потоками
                SourceImages.Add(bitmap);
            }
            catch
            {
                // Пропускаем файлы, которые не удалось открыть
            }
        }

        StatusText =
            $"Загружено {SourceImages.Count} изображений. Нажмите «Запустить» для обработки.";
    }

    /// <summary>
    /// Запускает однопоточную обработку асинхронно, чтобы не блокировать UI.
    /// </summary>
    private async void RunSingleAsync()
    {
        await RunProcessingAsync(isSingle: true);
    }

    /// <summary>
    /// Запускает многопоточную обработку асинхронно, чтобы не блокировать UI.
    /// </summary>
    private async void RunMultiAsync()
    {
        await RunProcessingAsync(isSingle: false);
    }

    /// <summary>
    /// Общая логика запуска обработки. Выносим в один метод, чтобы не дублировать код.
    /// </summary>
    /// <param name="isSingle">true — однопоточный режим, false — многопоточный.</param>
    private async Task RunProcessingAsync(bool isSingle)
    {
        IsBusy = true;
        ProgressValue = 0;
        ProgressMax = SourceImages.Count;

        string modeName = isSingle ? "однопоточном" : "многопоточном";
        StatusText = $"Обработка в {modeName} режиме...";

        var sources = SourceImages.ToList();

        var progress = new Progress<int>(count => ProgressValue = count);

        try
        {
            // Вся тяжёлая работа (пиксельная математика) — в фоновом потоке.
            // Сервис возвращает ProcessedImage (просто байты) — никаких WPF-объектов.
            var (result, processedImages) = await Task.Run(() =>
                isSingle
                    ? ImageProcessingService.RunSingleThread(sources, progress)
                    : _processingService.RunMultiThread(sources, progress)
            );

            // WriteableBitmap создаём здесь, в UI-потоке — так требует WPF.
            var targetCollection = isSingle ? SingleProcessedImages : MultiProcessedImages;
            targetCollection.Clear();

            foreach (var img in processedImages)
            {
                var bitmap = new System.Windows.Media.Imaging.WriteableBitmap(
                    img.Width,
                    img.Height,
                    96,
                    96,
                    System.Windows.Media.PixelFormats.Bgra32,
                    null
                );
                bitmap.WritePixels(
                    new System.Windows.Int32Rect(0, 0, img.Width, img.Height),
                    img.Pixels,
                    img.Stride,
                    0
                );

                targetCollection.Add(bitmap);
            }

            if (isSingle)
                SingleResult = result;
            else
                MultiResult = result;

            StatusText = $"{result.ModeName} режим завершён за {result.ElapsedMs} мс.";
        }
        catch (Exception ex)
        {
            StatusText = $"Ошибка: {ex.Message}";
        }
        finally
        {
            IsBusy = false;
        }
    }

    /// <summary>
    /// Сбрасывает всё состояние приложения.
    /// </summary>
    private void Clear()
    {
        SourceImages.Clear();
        SingleProcessedImages.Clear();
        MultiProcessedImages.Clear();
        SingleResult = null;
        MultiResult = null;
        ProgressValue = 0;
        StatusText = "Загрузите изображения для начала работы";
    }
}
