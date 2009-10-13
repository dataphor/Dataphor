using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Text;
using System.Windows.Forms;

namespace Alphora.Dataphor.Frontend.Client.Windows
{
    public partial class ResolutionsComboBox : ComboBox
    {
        public ResolutionsComboBox()
        {
            InitializeComponent();
        }

        public int SelectedResolution 
        {
            get
            {
                if (SelectedItem == null)
                    return 200;
                else
                    return (int)SelectedItem;
            }
            set
            {
                SelectedItem = (int)value;
            }
        }

        int[] resolutions;

        public int[] Resolutions
        {
            get 
            { 
                return resolutions; 
            }
            set 
            { 
                resolutions = value;
                DataSource = value;
                SelectedResolution = 200; // try 200, leave selected index 0 if no 200 in list
            }
        }

        protected override void OnFormat(ListControlConvertEventArgs e)
        {
            // Add " DPI"
            e.Value += " DPI";
            // Add user friendly blah-blah-blah to well-known values
            switch ((int)e.ListItem)
            {
                case 75:
                    e.Value += Localizer.GetStr(" (poor quality, not reccomended)");
                    break;
                case 100:
                    e.Value += Localizer.GetStr(" (poor quality, not reccomended)");
                    break;
                case 200:
                    e.Value += Localizer.GetStr(" (fax quality, quick scan, reccomended)");
                    break;
                case 300:
                    e.Value += Localizer.GetStr(" (low resolution printer quality, acceptable scan time)");
                    break;
                case 600:
                    e.Value += Localizer.GetStr(" (good printer quality, slow scan)");
                    break;
                case 1200:
                    e.Value += Localizer.GetStr(" (very good printer quality, very slow scan, not receomended)");
                    break;
                case 2400:
                    e.Value += Localizer.GetStr(" (exceptional quality, extremely slow scan, not receomended)");
                    break;
            }
        }
    }
}
