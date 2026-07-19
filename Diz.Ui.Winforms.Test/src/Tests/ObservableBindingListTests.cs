using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Windows.Forms;
using Diz.Ui.Winforms.usercontrols;
using FluentAssertions;
using Xunit;

namespace Diz.Ui.Winforms.Test.Tests;

/// <summary>
/// Proof of new-ui-plan.md design review finding 4: WinForms binding infrastructure
/// (BindingSource) listens to IBindingList.ListChanged and does NOT observe
/// INotifyCollectionChanged. An ObservableCollection bound through a BindingSource renders
/// once and then never reflects adds/removes. This is the failing bind that justifies the
/// ObservableBindingList adapter.
/// </summary>
public class BindingSourceObservableCollectionTests
{
    [Fact]
    public void BindingSource_OverObservableCollection_DoesNotRaiseListChanged_OnAdd()
    {
        var observable = new ObservableCollection<string> { "first" };
        using var bindingSource = new BindingSource { DataSource = observable };

        var listChangedEvents = 0;
        bindingSource.ListChanged += (_, _) => listChangedEvents++;

        observable.Add("second");
        observable.RemoveAt(0);

        // the underlying list DID change (BindingSource reads through to it) ...
        bindingSource.Count.Should().Be(1);
        // ... but no ListChanged ever fired, so a bound grid would never repaint. THIS is
        // the broken bind: if this assertion ever fails, WinForms learned INCC and the
        // adapter below is obsolete.
        listChangedEvents.Should().Be(0);
    }

    [Fact]
    public void BindingSource_OverBindingList_DoesRaiseListChanged_OnAdd()
    {
        // control experiment: identical wiring, IBindingList source -> events flow.
        var bindingList = new BindingList<string> { "first" };
        using var bindingSource = new BindingSource { DataSource = bindingList };

        var listChangedEvents = 0;
        bindingSource.ListChanged += (_, _) => listChangedEvents++;

        bindingList.Add("second");

        listChangedEvents.Should().BeGreaterThan(0);
    }
}

/// <summary>
/// The INCC-to-BindingList adapter itself: one-way, mapped, disposes mapped items when
/// they leave the list.
/// </summary>
public class ObservableBindingListTests
{
    private sealed class DisposableItem(string text) : IDisposable
    {
        public string Text { get; } = text;
        public bool Disposed { get; private set; }
        public void Dispose() => Disposed = true;
    }

    private static (ObservableCollection<string> source, ObservableBindingList<string, DisposableItem> adapter)
        NewAdapter(params string[] initial)
    {
        var source = new ObservableCollection<string>(initial);
        var adapter = new ObservableBindingList<string, DisposableItem>(
            new ReadOnlyObservableCollection<string>(source), s => new DisposableItem(s));
        return (source, adapter);
    }

    [Fact]
    public void InitialContents_AreMappedInOrder()
    {
        var (_, adapter) = NewAdapter("a", "b", "c");
        adapter.Select(x => x.Text).Should().Equal("a", "b", "c");
    }

    [Fact]
    public void Add_PropagatesAtCorrectIndex_AndRaisesListChanged()
    {
        var (source, adapter) = NewAdapter("a", "c");
        var events = new List<ListChangedType>();
        adapter.ListChanged += (_, e) => events.Add(e.ListChangedType);

        source.Insert(1, "b"); // sorted-insert shape: middle of the list, not the end

        adapter.Select(x => x.Text).Should().Equal("a", "b", "c");
        events.Should().Contain(ListChangedType.ItemAdded);
    }

    [Fact]
    public void Remove_Propagates_AndDisposesTheMappedItem()
    {
        var (source, adapter) = NewAdapter("a", "b");
        var removed = adapter[0];

        source.RemoveAt(0);

        adapter.Select(x => x.Text).Should().Equal("b");
        removed.Disposed.Should().BeTrue();
    }

    [Fact]
    public void Replace_Propagates_AndDisposesTheOldItem()
    {
        var (source, adapter) = NewAdapter("a", "b");
        var replaced = adapter[1];

        source[1] = "B";

        adapter.Select(x => x.Text).Should().Equal("a", "B");
        replaced.Disposed.Should().BeTrue();
    }

    [Fact]
    public void Reset_RebuildsFromSource_AndDisposesAllOldItems()
    {
        var (source, adapter) = NewAdapter("a", "b");
        var oldItems = adapter.ToList();
        var resets = 0;
        adapter.ListChanged += (_, e) =>
        {
            if (e.ListChangedType == ListChangedType.Reset) resets++;
        };

        source.Clear(); // ObservableCollection.Clear raises INCC Reset

        adapter.Should().BeEmpty();
        resets.Should().Be(1);
        oldItems.Should().OnlyContain(x => x.Disposed);
    }

    [Fact]
    public void Dispose_Unsubscribes_SoLaterSourceChangesAreIgnored()
    {
        var (source, adapter) = NewAdapter("a");
        adapter.Dispose();

        source.Add("b");

        adapter.Should().BeEmpty();
    }

    [Fact]
    public void ItemPropertyChanged_RaisesItemChanged_WhenItemsImplementInpc()
    {
        // BindingList's built-in INPC hookup is what lets the grid repaint a row when the
        // VM row changes (e.g. a rename from the details panel). Prove it survives mapping.
        var source = new ObservableCollection<InpcItem> { new() };
        var adapter = new ObservableBindingList<InpcItem, InpcItem>(
            new ReadOnlyObservableCollection<InpcItem>(source), x => x);

        var itemChangedEvents = 0;
        adapter.ListChanged += (_, e) =>
        {
            if (e.ListChangedType == ListChangedType.ItemChanged) itemChangedEvents++;
        };

        source[0].Text = "renamed";

        itemChangedEvents.Should().Be(1);
    }

    private sealed class InpcItem : INotifyPropertyChanged
    {
        private string text = "";
        public string Text
        {
            get => text;
            set
            {
                text = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Text)));
            }
        }
        public event PropertyChangedEventHandler? PropertyChanged;
    }
}
