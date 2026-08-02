using Diz.Controllers.controllers;
using Diz.Controllers.interfaces;
using Diz.Ui.Winforms.dialogs;
using Diz.Ui.Winforms.util;
using Diz.Ui.Winforms.window;
using JetBrains.Annotations;
using LightInject;

namespace Diz.Ui.Winforms;

// top-level, doesn't need to be referenced anywhere else
[UsedImplicitly] public class DizUiWinformsCompositionRoot : ICompositionRoot
{
    public void Compose(IServiceRegistry serviceRegistry)
    {
        // note: service names (the strings) here must exactly match IViewFactory method names.
        //
        // These are the WinForms views that are NOT backend-selectable -- they are always
        // WinForms regardless of the label-editor backend. The backend-selectable seams
        // (LabelEditorView, RegionEditorView, ProgressBarView, IFileDialogService and the
        // per-invocation dialogs) live in DizUiWinformsBackendCompositionRoot /
        // DizUiAvaloniaCompositionRoot, registered by an explicit if/else branch in
        // DizWinformsRegisterServices (new-ui plan step 6: no more last-registration-wins
        // ordering trick).
        serviceRegistry.Register<IMainGridWindowView, MainWindow>("MainGridWindowView");
        serviceRegistry.Register<IFormViewer, About>("AboutView");
        serviceRegistry.Register<IImportRomDialogView, ImportRomDialog>("ImportRomView");
        serviceRegistry.Register<ILogCreatorSettingsEditorView, LogCreatorSettingsEditorForm>("ExportDisassemblyView");

        serviceRegistry.RegisterSingleton<IDizAppSettings, DizAppSettingsProvider>(); // TODO: probably move this out of this project into app.common
    }
}