using OpenTK;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Hydra
{
    public partial class MainForm : Form
    {

        GeoGebra ggb;

        ///The main window with UI and stuff
        public MainForm()
        {
            Toolkit.Init();
            InitializeComponent();
        }

        void MainForm_Load(object sender, EventArgs e)
        {
            ggb = new GeoGebra();
            ggb.Loaded += Ggb_Load;
        }

        private async void Ggb_Load(object sender, EventArgs e)
        {
            await ggb.CreatePoint(1, 2, 3);
        }
    }
}
