using Diz.Test.Utils;
using DiztinGUIsh;
using LightInject;

namespace Diz.Ui.Winforms.Test.Utils;

public class ContainerWinformsFixture : ContainerFixture
{
    protected override void Configure(IServiceRegistry serviceRegistry)
    {
        base.Configure(serviceRegistry);

        DizAppServices.RegisterDizUiServices(serviceRegistry);
    }
}