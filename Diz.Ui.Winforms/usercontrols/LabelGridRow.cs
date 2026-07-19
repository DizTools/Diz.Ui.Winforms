using System.ComponentModel;
using Diz.Ui.ViewModels.Labels;

namespace Diz.Ui.Winforms.usercontrols;

/// <summary>
/// WinForms display wrapper around one ILabelRowViewModel, the item type of the grid's
/// bound list (see <see cref="ObservableBindingList{TSource,TItem}"/>).
///
/// Why it exists: DataGridView data-binding needs plain get/set string properties per
/// column. The setters here are DELIBERATE NO-OPS -- they exist only so the bound columns
/// stay editable (a setterless property makes its column read-only). Actual writes flow
/// through ILabelEditorViewModel.ValidateEdit/CommitEdit in LabelsViewControl; the grid's
/// own push-back after an edit lands here and is discarded (one-way binding, plan finding 4).
///
/// Relays the row VM's PropertyChanged so BindingList raises ItemChanged and the grid
/// repaints the row when a label changes elsewhere (e.g. the details panel).
/// </summary>
public sealed class LabelGridRow : INotifyPropertyChanged, IDisposable
{
    public LabelGridRow(ILabelRowViewModel row)
    {
        Row = row;
        row.PropertyChanged += OnRowPropertyChanged;
    }

    public ILabelRowViewModel Row { get; }

    // ReSharper disable ValueParameterNotUsed -- see class comment: setters are no-ops by design
    public string Address { get => Row.AddressText; set { } }
    public string Name { get => Row.Name; set { } }
    public string Comment { get => Row.Comment; set { } }
    // ReSharper restore ValueParameterNotUsed
    public string Context => Row.ContextSummary;

    public event PropertyChangedEventHandler? PropertyChanged;

    private void OnRowPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        var mapped = e.PropertyName switch
        {
            nameof(ILabelRowViewModel.Name) => nameof(Name),
            nameof(ILabelRowViewModel.Comment) => nameof(Comment),
            nameof(ILabelRowViewModel.ContextSummary) => nameof(Context),
            nameof(ILabelRowViewModel.AddressText) => nameof(Address),
            _ => null,
        };
        if (mapped != null)
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(mapped));
    }

    public void Dispose() => Row.PropertyChanged -= OnRowPropertyChanged;
}
