using Diz.Controllers.interfaces;
using Diz.Ui.Winforms.dialogs;
using Diz.Ui.Winforms.util;
using JetBrains.Annotations;
using LightInject;

namespace Diz.Ui.Winforms;

[UsedImplicitly] public class DizWinformsCompositionRoot : ICompositionRoot
{
    public void Compose(IServiceRegistry serviceRegistry)
    {
        // note: service names (the strings) here must exactly match IViewFactory method names
        serviceRegistry.Register<IFormViewer, About>("AboutView");

        serviceRegistry.RegisterSingleton<IDizAppSettings, DizWinformsAppSettingsProvider>();
    }
}