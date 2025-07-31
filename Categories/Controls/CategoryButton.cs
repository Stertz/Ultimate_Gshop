using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Ultimate_Gshop.Categories.Controls
{
    public partial class CategoryButton : UserControl
    {
        public CategoryButton()
        {
            InitializeComponent();
        }

        public void SetTitle(string title)
        {
            LabelTitle.Text = title;
        }

        protected override void OnClick(EventArgs e)
        {
            base.OnClick(e);

 

        }

    }
}
