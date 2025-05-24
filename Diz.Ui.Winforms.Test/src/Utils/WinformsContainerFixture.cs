using Diz.Test.Utils;
using LightInject;

namespace Diz.Ui.Winforms.Test.Utils;

public class ContainerWinformsFixture : ContainerFixture
{
    protected override void Configure(IServiceRegistry serviceRegistry)
    {
        base.Configure(serviceRegistry);

        // TODO // DizAppServices.RegisterDizUiServices(serviceRegistry);
    }
}