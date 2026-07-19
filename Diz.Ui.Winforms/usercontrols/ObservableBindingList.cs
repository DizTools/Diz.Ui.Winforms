using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;

namespace Diz.Ui.Winforms.usercontrols;

/// <summary>
/// One-way INCC-to-IBindingList bridge (new-ui-plan.md, design review finding 4).
///
/// WinForms binding (BindingSource / DataGridView) listens to IBindingList.ListChanged and
/// does NOT observe INotifyCollectionChanged, so an ObservableCollection bound through a
/// BindingSource renders once and never reflects adds/removes. Proven by
/// Diz.Ui.Winforms.Test/src/Tests/ObservableBindingListTests.BindingSource_OverObservableCollection_DoesNotRaiseListChanged_OnAdd.
///
/// This adapter mirrors a ReadOnlyObservableCollection (the ViewModel's Rows) into a
/// BindingList the grid can bind, mapping each source item through <c>map</c> (toolkit glue,
/// e.g. ILabelRowViewModel -&gt; LabelGridRow).
///
/// Contract:
///  - ONE-WAY: source (VM) to grid only. Grid edits must flow back through explicit VM
///    commands (ValidateEdit/CommitEdit), never by mutating this list. Nothing here writes
///    to the source.
///  - UI-THREAD-ONLY: the source collection must raise CollectionChanged on the UI thread
///    (the VM's notification marshaller guarantees this). No locking is done here.
///  - Mapped items that implement IDisposable are disposed when they leave the list.
/// </summary>
public sealed class ObservableBindingList<TSource, TItem> : BindingList<TItem>, IDisposable
    where TItem : class
{
    private readonly ReadOnlyObservableCollection<TSource> source;
    private readonly Func<TSource, TItem> map;
    private bool disposed;

    public ObservableBindingList(ReadOnlyObservableCollection<TSource> source, Func<TSource, TItem> map)
    {
        this.source = source;
        this.map = map;
        ResetFromSource();
        ((INotifyCollectionChanged)source).CollectionChanged += OnSourceCollectionChanged;
    }

    private void OnSourceCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        switch (e.Action)
        {
            case NotifyCollectionChangedAction.Add:
                for (var i = 0; i < e.NewItems!.Count; i++)
                    Insert(e.NewStartingIndex + i, map((TSource)e.NewItems[i]!));
                break;

            case NotifyCollectionChangedAction.Remove:
                for (var i = 0; i < e.OldItems!.Count; i++)
                    RemoveAndDisposeAt(e.OldStartingIndex);
                break;

            case NotifyCollectionChangedAction.Replace:
                for (var i = 0; i < e.NewItems!.Count; i++)
                {
                    (this[e.NewStartingIndex + i] as IDisposable)?.Dispose();
                    this[e.NewStartingIndex + i] = map((TSource)e.NewItems[i]!);
                }
                break;

            case NotifyCollectionChangedAction.Move:
                var moved = this[e.OldStartingIndex];
                RemoveAt(e.OldStartingIndex); // keep the same mapped item; don't dispose it
                Insert(e.NewStartingIndex, moved);
                break;

            case NotifyCollectionChangedAction.Reset:
            default:
                ResetFromSource();
                break;
        }
    }

    private void RemoveAndDisposeAt(int index)
    {
        (this[index] as IDisposable)?.Dispose();
        RemoveAt(index);
    }

    private void ResetFromSource()
    {
        // batch: one ListChanged(Reset) instead of one event per row
        RaiseListChangedEvents = false;
        foreach (var item in this)
            (item as IDisposable)?.Dispose();
        Clear();
        foreach (var item in source)
            Add(map(item));
        RaiseListChangedEvents = true;
        ResetBindings();
    }

    public void Dispose()
    {
        if (disposed)
            return;
        disposed = true;

        ((INotifyCollectionChanged)source).CollectionChanged -= OnSourceCollectionChanged;
        RaiseListChangedEvents = false;
        foreach (var item in this)
            (item as IDisposable)?.Dispose();
        Clear();
    }
}
