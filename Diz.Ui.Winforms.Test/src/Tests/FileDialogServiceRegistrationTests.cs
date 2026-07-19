using Diz.Controllers.interfaces;
using Diz.Ui.Winforms.usercontrols;
using Diz.Ui.Winforms.util;
using FluentAssertions;
using LightInject;
using Xunit;

namespace Diz.Ui.Winforms.Test.Tests;

// Step 4 of the new-ui plan: IFileDialogService is the per-toolkit file-dialog seam.
// These tests prove the WinForms composition root registers it and hands the container's
// instance to the label editor -- no real dialog is ever shown (registration/wiring only).
public class FileDialogServiceRegistrationTests
{
    [Fact]
    public void WinformsCompositionRoot_ResolvesFileDialogService()
    {
        using var container = new ServiceContainer();
        container.RegisterFrom<DizUiWinformsCompositionRoot>();

        container.GetInstance<IFileDialogService>()
            .Should().BeOfType<WinformsFileDialogService>();
    }

    [Fact]
    public void LabelEditorView_ReceivesTheContainerFileDialogServiceInstance()
    {
        using var container = new ServiceContainer();
        container.RegisterFrom<DizUiWinformsCompositionRoot>();

        // resolving the named registration constructs the real LabelEditorForm host +
        // LabelsViewControl (fine headless: no handle is created until Show).
        var view = container.GetInstance<ILabelEditorView>("LabelEditorView");

        var control = view.Should().BeOfType<LabelsViewControl>().Which;
        // the composition root must overwrite the control's designer-safe default with
        // the container's (singleton) instance -- instance identity proves the wiring ran.
        control.FileDialogService.Should().BeSameAs(container.GetInstance<IFileDialogService>());
    }
}
