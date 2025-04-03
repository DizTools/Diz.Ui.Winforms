

// just shows one color and one label for Rom visualizer. like ("blue" = 8bit pointer)

namespace Diz.Ui.Winforms.usercontrols.visualizer.legend
{
    public partial class BankLegendItem : UserControl
    {
        public BankLegendItem(string labelText, Color color)
        {
            InitializeComponent();

            label1.Text = labelText;
            pictureBox1.BackColor = color;
        }
    }
}
