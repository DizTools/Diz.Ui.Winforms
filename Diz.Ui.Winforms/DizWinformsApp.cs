using Diz.Controllers.interfaces;
using Diz.Ui.Winforms.util;

namespace Diz.Ui.Winforms;

// ReSharper disable once ClassNeverInstantiated.Global
public class DizWinformsApp(IViewFactory viewFactory) : IDizApp
{
    public void Run(string initialProjectFileToOpen = "")
    {
        // TODO: do less weird janky casting here.
        
        WinformsGuiUtil.SetupDpiStuff();
        var mainWindow = viewFactory.GetMainGridWindowView();

        // TODO: fix
        // if (!string.IsNullOrEmpty(initialProjectFileToOpen))
        //      mainWindow.ProjectController.OpenProject(initialProjectFileToOpen);

        Application.Run(mainWindow as Form);
    }
}