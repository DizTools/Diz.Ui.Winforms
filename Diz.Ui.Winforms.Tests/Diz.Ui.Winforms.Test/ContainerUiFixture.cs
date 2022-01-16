using Diz.Test.Utils;
using DiztinGUIsh;
using LightInject;

namespace Diz.Ui.Winforms.Test;

public class ContainerUiFixture : ContainerFixture
{
    protected override void Configure(IServiceRegistry serviceRegistry)
    {
        // not going to call the base, we're going to start even higher level than this.
        // our function call should eventually register the same services as what the base does
        // base.Configure(serviceRegistry);

        DizAppServices.RegisterDizUiServices(serviceRegistry);
    }
}