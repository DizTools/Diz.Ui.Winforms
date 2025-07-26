using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using Diz.Controllers.interfaces;

namespace Diz.Ui.Winforms.usercontrols;

public partial class RegionListUserControl : UserControl, IRegionListView
{
    private IProjectController? projectController;

    public RegionListUserControl()
    {
        InitializeComponent();
    }
    
    public void BringFormToTop()
    {
        Show();
    }

    public void SetProjectController(IProjectController? projectController)
    {
        this.projectController = projectController;
    }
    
    public event EventHandler? OnFormClosed;
}