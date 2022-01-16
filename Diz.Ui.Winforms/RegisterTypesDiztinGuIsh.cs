using Diz.Controllers.interfaces;
using Diz.Ui.Winforms.dialogs;
using JetBrains.Annotations;
using LightInject;

namespace Diz.Ui.Winforms;

[UsedImplicitly] public class DizWinformsCompositionRoot : ICompositionRoot
{
    public void Compose(IServiceRegistry serviceRegistry)
    {
        // note: string names here must match IViewFactory method names
        serviceRegistry.Register<IFormViewer, About>("AboutView");
    }
}