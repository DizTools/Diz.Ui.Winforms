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
        serviceRegistry.Register<ILabelEditorView, AliasList>("LabelEditorView");
        serviceRegistry.Register<IRegionListView, RegionList>("RegionEditorView");
        
        serviceRegistry.RegisterSingleton<IDizAppSettings, DizAppSettingsProvider>(); // TODO: probably move this out of this project into app.common
    }
}