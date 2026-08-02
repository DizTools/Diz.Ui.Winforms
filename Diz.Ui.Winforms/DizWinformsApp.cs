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

        // Hand the command-line project (argv[0]) to the window rather than opening it here: unlike
        // Eto, the WinForms TaskHandler shows progress UI and needs a running message pump, so
        // opening before Application.Run would deadlock. MainWindow.Init() does the open after first
        // paint, and gives this precedence over the "open last project automatically" setting.
        if (mainWindow is window.MainWindow winformsMainWindow)
            winformsMainWindow.InitialProjectFileToOpen = initialProjectFileToOpen;

        Application.Run(mainWindow as Form);
    }
}