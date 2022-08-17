using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace AlertFormV2
{
    public partial class UpcomingEventsForm : Form
    {
        static int clickCounter = 0;
        public UpcomingEventsForm()
        {
            InitializeComponent();
        }

        private void UpcomingEventsForm_FormClosing(object sender, FormClosingEventArgs e)
        {
            e.Cancel = true;
        }

    }
}
