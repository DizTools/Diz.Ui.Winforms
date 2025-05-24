using System.ComponentModel;
using System.Runtime.InteropServices;
using Diz.Core.model;
using Diz.Ui.Winforms.util;

// this usercontrol is JUST to show the raw image data (no markup, formatting, etc)

namespace Diz.Ui.Winforms.usercontrols.visualizer.graphics
{
    public partial class RomImage : UserControl
    {
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public RomVisual RomVisual { get; protected set; } = new();
        
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public IProject Project
        {
            get => RomVisual.Project;
            set =>
                RomVisual = new RomVisual
                {
                    Project = value
                };
            // if there's a reason to track ROM byte changes, hook in here
            // romVisual.MarkedDirty += RomVisual_MarkedDirty;
        }

        public RomImage()
        {
            InitializeComponent();
        }

        private void RomImage_Load(object sender, System.EventArgs e)
        {
            UpdateDimensions();
        }

        private void RomImage_Paint(object sender, PaintEventArgs e)
        {
            Redraw(e.Graphics);
        }

        private void Redraw(Graphics graphics = null)
        {
            if (RomVisual?.Bitmap == null)
                return;

            graphics ??= CreateGraphics();

            if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows)) 
                return;
            
            var width = RomVisual.Bitmap.Width;
            var height = RomVisual.Bitmap.Height;
            graphics.DrawImage(RomVisual.Bitmap, 0, 0, width, height);
        }

        private void RedrawIfNeeded()
        {
            if (!Visible || !RomVisual.IsDirty)
                return;

            RomVisual.Refresh();
            Redraw();
        }

        private void timer1_Tick(object sender, System.EventArgs e)
        {
            RedrawIfNeeded();
        }

        private void UpdateDimensions()
        {
            Width = RomVisual.Width;
            Height = RomVisual.Height;
        }
    }
}
