using System.Windows.Forms;
using Diz.Controllers.interfaces;
using Diz.Ui.Winforms.dialogs;
using Diz.Ui.Winforms.usercontrols;
using FluentAssertions;
using LightInject;
using Xunit;

namespace Diz.Ui.Winforms.Test.Tests;

/// <summary>
/// The region editor is resolved by name through IViewFactory and never constructed by anyone.
/// Three things have to hold for that to work, none of which the compiler checks: the WinForms
/// root has to register something; the registration NAME has to match the factory method name
/// minus "Get", because the auto-factory resolves by that string; and what comes back has to be
/// the CONTROL already sitting inside its window, because Show() and BringFormToTop() work by
/// finding the form the control lives in. A control handed out unparented would compile, resolve,
/// and then silently do nothing when the user picked Tools -> Region List.
/// </summary>
public class RegionListViewRegistrationTests
{
    [Fact]
    public void TheWinformsRootRegistersTheRegionEditorView()
    {
        using var container = new ServiceContainer();
        container.RegisterFrom<DizUiWinformsCompositionRoot>();

        container.GetInstance<IRegionListView>("RegionEditorView")
            .Should().BeOfType<RegionListViewControl>();
    }

    [Fact]
    public void TheRegionEditorArrivesAlreadyHostedInItsOwnWindow()
    {
        using var container = new ServiceContainer();
        container.RegisterFrom<DizUiWinformsCompositionRoot>();

        var view = container.GetInstance<IRegionListView>("RegionEditorView");

        ((Control)view).FindForm().Should().BeOfType<RegionListForm>();
    }

    [Fact]
    public void TheViewFactoryHandsBackTheRegionEditorByMethodName()
    {
        // this is the path MainWindow actually takes. IViewFactory has no implementation
        // anywhere: LightInject generates one, mapping GetRegionEditorView() to the registration
        // named "RegionEditorView". A typo in either string would compile fine and fail only here.
        using var container = new ServiceContainer();
        container.EnableAutoFactories();
        container.RegisterAutoFactory<IViewFactory>();
        container.RegisterFrom<DizUiWinformsCompositionRoot>();

        var viewFactory = container.GetInstance<IViewFactory>();

        viewFactory.GetRegionEditorView().Should().BeOfType<RegionListViewControl>();
    }

    [Fact]
    public void EveryResolveGetsItsOwnWindow()
    {
        // MainWindow keeps one for the application's lifetime, but the registration itself must
        // not hand the same control to two owners -- each would rebind it to its own project.
        using var container = new ServiceContainer();
        container.RegisterFrom<DizUiWinformsCompositionRoot>();

        var first = container.GetInstance<IRegionListView>("RegionEditorView");
        var second = container.GetInstance<IRegionListView>("RegionEditorView");

        second.Should().NotBeSameAs(first);
        ((Control)second).FindForm().Should().NotBeSameAs(((Control)first).FindForm());
    }
}
