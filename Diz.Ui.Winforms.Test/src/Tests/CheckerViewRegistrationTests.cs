using Diz.Controllers.interfaces;
using Diz.Ui.Winforms.dialogs;
using FluentAssertions;
using LightInject;
using Xunit;

namespace Diz.Ui.Winforms.Test.Tests;

/// <summary>
/// The misaligned-flags window and the in/out-point rescan confirmation are per-toolkit view
/// services: the caller asks IViewFactory for one and never names a form. Two things have to
/// hold for that to work, and neither is checked by the compiler -- the WinForms backend has to
/// register an implementation, and the registration NAME has to match the factory method name
/// minus "Get", because the auto-factory resolves by that string.
/// </summary>
public class CheckerViewRegistrationTests
{
    [Fact]
    public void WinformsBackendRegistersTheMisalignmentCheckerView()
    {
        using var container = new ServiceContainer();
        container.RegisterFrom<DizUiWinformsBackendCompositionRoot>();

        container.GetInstance<IMisalignmentCheckerView>("MisalignmentCheckerView")
            .Should().BeOfType<WinformsMisalignmentCheckerView>();
    }

    [Fact]
    public void WinformsBackendRegistersTheInOutPointCheckerView()
    {
        using var container = new ServiceContainer();
        container.RegisterFrom<DizUiWinformsBackendCompositionRoot>();

        container.GetInstance<IInOutPointCheckerView>("InOutPointCheckerView")
            .Should().BeOfType<WinformsInOutPointCheckerView>();
    }

    [Fact]
    public void EachViewIsAFreshInstancePerResolve()
    {
        // resolve, run, discard: one window per invocation, so no state can leak from one
        // showing to the next.
        using var container = new ServiceContainer();
        container.RegisterFrom<DizUiWinformsBackendCompositionRoot>();

        container.GetInstance<IMisalignmentCheckerView>("MisalignmentCheckerView")
            .Should().NotBeSameAs(container.GetInstance<IMisalignmentCheckerView>("MisalignmentCheckerView"));

        container.GetInstance<IInOutPointCheckerView>("InOutPointCheckerView")
            .Should().NotBeSameAs(container.GetInstance<IInOutPointCheckerView>("InOutPointCheckerView"));
    }

    [Fact]
    public void TheViewFactoryHandsBackBothWindowsByMethodName()
    {
        // this is the path MainWindow actually takes. IViewFactory has no implementation
        // anywhere: LightInject generates one, mapping GetXView() to the registration named "X
        // View". A typo in either string would compile fine and fail only here.
        using var container = new ServiceContainer();
        container.EnableAutoFactories();
        container.RegisterAutoFactory<IViewFactory>();
        container.RegisterFrom<DizUiWinformsBackendCompositionRoot>();

        var viewFactory = container.GetInstance<IViewFactory>();

        viewFactory.GetMisalignmentCheckerView().Should().BeOfType<WinformsMisalignmentCheckerView>();
        viewFactory.GetInOutPointCheckerView().Should().BeOfType<WinformsInOutPointCheckerView>();
    }

    [Fact]
    public void TheViewFactoryStillBuildsWithMembersThisBackendDoesNotRegister()
    {
        // the generated factory carries a method for every member of IViewFactory, including
        // ones no WinForms backend registration answers. Building it must not depend on all of
        // them resolving -- only on the ones actually called.
        using var container = new ServiceContainer();
        container.EnableAutoFactories();
        container.RegisterAutoFactory<IViewFactory>();
        container.RegisterFrom<DizUiWinformsBackendCompositionRoot>();

        var resolve = () => container.GetInstance<IViewFactory>();

        resolve.Should().NotThrow();
    }
}
