using Diz.Controllers.controllers;
using Diz.Controllers.interfaces;
using Diz.Ui.Winforms.dialogs;
using Diz.Ui.Winforms.util;
using Diz.Ui.Winforms.window;
using JetBrains.Annotations;
using LightInject;

namespace Diz.Ui.Winforms;

/// <summary>
/// The WinForms LABEL-EDITOR BACKEND: exactly the backend-selectable registrations
/// (LabelEditorView, MarkManyView, GotoView, HarshAutoStepView, ProgressBarView,
/// IFileDialogService). The app registers EITHER this root
/// OR <c>DizUiAvaloniaCompositionRoot</c> via an explicit if/else branch in
/// DizWinformsRegisterServices -- never both (new-ui plan step 6, replacing the old
/// last-registration-wins ordering trick). Non-selectable WinForms views stay in
/// <see cref="DizUiWinformsCompositionRoot"/>, which is always registered.
/// </summary>
[UsedImplicitly] public class DizUiWinformsBackendCompositionRoot : ICompositionRoot
{
    public void Compose(IServiceRegistry serviceRegistry)
    {
        // note: service names (the strings) here must exactly match IViewFactory method names.
        serviceRegistry.Register<IProgressView, ProgressDialog>("ProgressBarView");

        // the interface implementation is the LabelsViewControl hosted inside a LabelEditorForm
        // window (the form itself is a plain host since step 3 of the new-ui plan). the
        // control's Show()/BringFormToTop() operate on its host form, so callers see the
        // same behavior as when LabelEditorForm implemented the interface directly.
        // step 4: the control prompts for import/export paths through IFileDialogService,
        // so hand it the container's instance (its default is the same WinForms impl).
        serviceRegistry.Register<ILabelEditorView>(factory =>
        {
            var labelEditor = new LabelEditorForm().LabelEditor;
            labelEditor.FileDialogService = factory.GetInstance<IFileDialogService>();
            return labelEditor;
        }, "LabelEditorView");

        // the mark-many window. A fresh instance per resolve: the view is created, used for one
        // edit, and discarded.
        serviceRegistry.Register<IMarkManyView, WinformsMarkManyView>("MarkManyView");

        // the goto window, same per-invocation lifetime.
        serviceRegistry.Register<IGotoView, WinformsGotoView>("GotoView");

        // the harsh-auto-step window, same per-invocation lifetime.
        serviceRegistry.Register<IHarshAutoStepView, WinformsHarshAutoStepView>("HarshAutoStepView");

        // the file-dialog seam (new-ui plan step 4): each UI toolkit registers its own.
        // singleton: the service is stateless (a fresh dialog per call).
        serviceRegistry.RegisterSingleton<IFileDialogService, WinformsFileDialogService>();
    }
}
