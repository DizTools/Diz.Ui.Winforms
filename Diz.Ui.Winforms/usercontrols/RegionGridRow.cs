using System.ComponentModel;
using Diz.Core.Interfaces;
using Diz.Ui.ViewModels.Regions;

namespace Diz.Ui.Winforms.usercontrols;

/// <summary>
/// WinForms display wrapper around one region row ViewModel, the item type of the grid's bound
/// list (see <see cref="ObservableBindingList{TSource,TItem}"/>).
///
/// Why it exists: DataGridView data-binding needs plain get/set properties, one per column, and
/// the property names are what the columns' DataPropertyName strings point at. The setters here
/// are DELIBERATE NO-OPS -- they exist only so the bound columns stay editable (a setterless
/// property makes its column read-only). Actual writes flow through the list ViewModel's
/// CommitField, which validates first; the grid's own push-back after an edit lands here and is
/// discarded (one-way binding).
///
/// Most columns show TEXT, and the text a column shows is what the row ViewModel says to show:
/// the value the model holds, or -- while an edit was refused -- the text the user typed, which
/// the model never accepted. The two non-text columns (the separate-file checkbox and the export
/// type combo) show the COMMITTED value instead: a checkbox or a combo can only hold a legal
/// value, so displaying an uncommitted one would claim the model had taken it. A refused edit on
/// those columns is reported by the row's error marker and the status line, and the widget snaps
/// back to what the model actually holds.
///
/// Relays the row ViewModel's PropertyChanged so BindingList raises ItemChanged and the grid
/// repaints the row when a region changes anywhere else -- another window, an import, a
/// migration.
/// </summary>
public sealed class RegionGridRow : INotifyPropertyChanged, IDisposable
{
    public RegionGridRow(IRegionRowViewModel row)
    {
        Row = row;
        row.PropertyChanged += OnRowPropertyChanged;
    }

    public IRegionRowViewModel Row { get; }

    // ReSharper disable ValueParameterNotUsed -- see class comment: setters are no-ops by design
    public string StartSnesAddress { get => Row.StartText; set { } }
    public string EndSnesAddress { get => Row.EndText; set { } }
    public string Length { get => Row.LengthText; set { } }
    public string RegionName { get => Row.RegionNameText; set { } }
    public string ContextToApply { get => Row.ContextToApplyText; set { } }
    public string Priority { get => Row.PriorityText; set { } }
    public bool ExportSeparateFile { get => Row.ExportSeparateFile; set { } }
    public RegionExportType ExportType { get => Row.ExportType; set { } }
    public string AssetType { get => Row.AssetTypeText; set { } }
    public string AssetVersion { get => Row.AssetVersionText; set { } }
    public string AssetName { get => Row.AssetNameText; set { } }
    public string AssetOptions { get => Row.AssetOptionsText; set { } }
    // ReSharper restore ValueParameterNotUsed

    /// <summary>Not bound to a column: it drives the row's error marker and background, and it is
    /// a property (rather than something the view reads off the row ViewModel directly) so that a
    /// change to it reaches BindingList and repaints the row.</summary>
    public bool HasError => Row.HasError;

    public string ErrorText => Row.ErrorText;

    /// <summary>Not bound to a column either: it decides whether the four asset cells are greyed
    /// out and read-only for this row.</summary>
    public bool AssetFieldsEnabled => Row.AssetFieldsEnabled;

    public event PropertyChangedEventHandler? PropertyChanged;

    private void OnRowPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        var mapped = e.PropertyName switch
        {
            nameof(IRegionRowViewModel.StartText) => nameof(StartSnesAddress),
            nameof(IRegionRowViewModel.EndText) => nameof(EndSnesAddress),
            nameof(IRegionRowViewModel.LengthText) => nameof(Length),
            nameof(IRegionRowViewModel.RegionNameText) => nameof(RegionName),
            nameof(IRegionRowViewModel.ContextToApplyText) => nameof(ContextToApply),
            nameof(IRegionRowViewModel.PriorityText) => nameof(Priority),
            nameof(IRegionRowViewModel.ExportSeparateFileText) => nameof(ExportSeparateFile),
            nameof(IRegionRowViewModel.ExportSeparateFile) => nameof(ExportSeparateFile),
            nameof(IRegionRowViewModel.ExportTypeText) => nameof(ExportType),
            nameof(IRegionRowViewModel.ExportType) => nameof(ExportType),
            nameof(IRegionRowViewModel.AssetTypeText) => nameof(AssetType),
            nameof(IRegionRowViewModel.AssetVersionText) => nameof(AssetVersion),
            nameof(IRegionRowViewModel.AssetNameText) => nameof(AssetName),
            nameof(IRegionRowViewModel.AssetOptionsText) => nameof(AssetOptions),
            nameof(IRegionRowViewModel.AssetFieldsEnabled) => nameof(AssetFieldsEnabled),
            nameof(IRegionRowViewModel.HasError) => nameof(HasError),
            nameof(IRegionRowViewModel.ErrorText) => nameof(ErrorText),
            _ => null,
        };
        if (mapped != null)
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(mapped));
    }

    public void Dispose() => Row.PropertyChanged -= OnRowPropertyChanged;
}
