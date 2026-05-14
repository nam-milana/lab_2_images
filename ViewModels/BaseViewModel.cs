using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace ImageProcessor.ViewModels;

/// <summary>
/// Базовый класс для всех ViewModel.
/// Реализует INotifyPropertyChanged, чтобы WPF-биндинги обновлялись автоматически.
/// </summary>
public abstract class BaseViewModel : INotifyPropertyChanged
{
    public event PropertyChangedEventHandler? PropertyChanged;

    /// <summary>
    /// Устанавливает значение поля и уведомляет UI об изменении, если значение действительно изменилось.
    /// </summary>
    /// <typeparam name="T">Тип свойства.</typeparam>
    /// <param name="field">Ссылка на backing field.</param>
    /// <param name="value">Новое значение.</param>
    /// <param name="propertyName">Имя свойства (заполняется автоматически компилятором).</param>
    /// <returns>true, если значение изменилось.</returns>
    protected bool SetField<T>(ref T field, T value, [CallerMemberName] string propertyName = "")
    {
        if (EqualityComparer<T>.Default.Equals(field, value))
            return false;

        field = value;
        OnPropertyChanged(propertyName);
        return true;
    }

    /// <summary>
    /// Явно уведомляет UI об изменении свойства по имени.
    /// Используется для вычисляемых свойств, у которых нет backing field.
    /// </summary>
    protected void OnPropertyChanged(string propertyName) =>
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
}
