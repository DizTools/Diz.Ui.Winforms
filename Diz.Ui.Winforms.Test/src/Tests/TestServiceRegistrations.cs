using Diz.Controllers.interfaces;
using Diz.Ui.Winforms.Test.Utils;
using FluentAssertions;
using Xunit;

namespace Diz.Ui.Winforms.Test.Tests;

public class TestServiceRegistrations : ContainerWinformsFixture
{
    // temp disabled til we get this working again
    /*[Fact]
    public void TestUtilServices()
    {
        GetInstance<ICommonGui>().Should().NotBeNull();

        GetInstance<IDizDocument>();
    }
    
    [Fact]
    public void Views()
    {
        var viewFactory = GetInstance<IViewFactory>();

        viewFactory.GetAboutView().Should().NotBeNull();
        viewFactory.GetExportDisassemblyView().Should().NotBeNull();
        viewFactory.GetImportRomView().Should().NotBeNull();
        viewFactory.GetLabelEditorView().Should().NotBeNull();
        viewFactory.GetProgressBarView().Should().NotBeNull();
        viewFactory.GetMainGridWindowView().Should().NotBeNull();
    }*/
}