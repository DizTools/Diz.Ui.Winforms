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
/// (LabelEditorView, RegionEditorView, MarkManyView, GotoView, HarshAutoStepView,
/// MisalignmentCheckerView,
/// InOutPointCheckerView, ProgressBarView, IFileDialogService). The app registers EITHER this root
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

        RegisterRegionEditorView(serviceRegistry);

        // the mark-many window. A fresh instance per resolve: the view is created, used for one
        // edit, and discarded.
        serviceRegistry.Register<IMarkManyView, WinformsMarkManyView>("MarkManyView");

        // the goto window, same per-invocation lifetime.
        serviceRegistry.Register<IGotoView, WinformsGotoView>("GotoView");

        // the harsh-auto-step window, same per-invocation lifetime.
        serviceRegistry.Register<IHarshAutoStepView, WinformsHarshAutoStepView>("HarshAutoStepView");

        // the misaligned-flags window, same per-invocation lifetime.
        serviceRegistry.Register<IMisalignmentCheckerView, WinformsMisalignmentCheckerView>("MisalignmentCheckerView");

        // the in/out-point rescan confirmation, same per-invocation lifetime.
        serviceRegistry.Register<IInOutPointCheckerView, WinformsInOutPointCheckerView>("InOutPointCheckerView");

        // the file-dialog seam (new-ui plan step 4): each UI toolkit registers its own.
        // singleton: the service is stateless (a fresh dialog per call).
        serviceRegistry.RegisterSingleton<IFileDialogService, WinformsFileDialogService>();
    }

    /// <summary>
    /// Register the WinForms region editor under the name IViewFactory.GetRegionEditorView()
    /// resolves by. Exposed separately because the TUI backend has no region screen and falls
    /// back to this one, and the control the registration hands out is internal to this assembly
    /// -- so the app layer cannot write the expression itself.
    ///
    /// What comes back is the RegionListViewControl already sitting inside its RegionListForm
    /// window: the control's Show()/BringFormToTop() work by finding the form it lives in, so an
    /// unparented control would resolve fine and then silently do nothing. Long-lived like the
    /// label editor -- MainWindow resolves one and keeps it for the application's lifetime -- but
    /// still one per resolve, so two owners never end up sharing a window.
    /// </summary>
    public static void RegisterRegionEditorView(IServiceRegistry serviceRegistry) =>
        serviceRegistry.Register<IRegionListView>(
            _ => new RegionListForm().RegionEditor, "RegionEditorView");
}
