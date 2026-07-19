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
        // note: service names (the strings) here must exactly match IViewFactory method names
        serviceRegistry.Register<IMainGridWindowView, MainWindow>("MainGridWindowView");
        serviceRegistry.Register<IFormViewer, About>("AboutView");
        serviceRegistry.Register<IImportRomDialogView, ImportRomDialog>("ImportRomView");
        serviceRegistry.Register<IProgressView, ProgressDialog>("ProgressBarView");
        serviceRegistry.Register<ILogCreatorSettingsEditorView, LogCreatorSettingsEditorForm>("ExportDisassemblyView");
        // the interface implementation is the LabelsViewControl hosted inside an AliasList
        // window (the form itself is a plain host since step 3 of the new-ui plan). the
        // control's Show()/BringFormToTop() operate on its host form, so callers see the
        // same behavior as when AliasList implemented the interface directly.
        serviceRegistry.Register<ILabelEditorView>(_ => new AliasList().LabelEditor, "LabelEditorView");
        serviceRegistry.Register<IRegionListView, RegionList>("RegionEditorView");
        
        serviceRegistry.RegisterSingleton<IDizAppSettings, DizAppSettingsProvider>(); // TODO: probably move this out of this project into app.common
    }
}