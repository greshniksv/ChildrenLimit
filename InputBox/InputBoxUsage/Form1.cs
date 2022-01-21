using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Text;
using System.Windows.Forms;
using MsgBox;
namespace InputBoxUsage
{
    public partial class Form1 : Form
    {
        public Form1()
        {
            InitializeComponent();
        }
        private void button1_Click(object sender, EventArgs e)
        {
            //Set buttons language Czech/English/German/Slovakian/Spanish (default English)
            InputBox.SetLanguage(InputBox.Language.English);
            //Save the DialogResult as res
            DialogResult res = InputBox.ShowDialog("Select some item from ComboBox below:", "Combo InputBox",   //Text message (mandatory), Title (optional)
                InputBox.Icon.Question,                                                                         //Set icon type Error/Exclamation/Question/Warning (default info)
                InputBox.Buttons.OkCancel,                                                                      //Set buttons set OK/OKcancel/YesNo/YesNoCancel (default ok)
                InputBox.Type.TextBox,                                                                         //Set type ComboBox/TextBox/Nothing (default nothing)
                new string[] { "Item1","Item2","Item3"},                                                        //Set string field as ComboBox items (default null)
                true,                                                                                           //Set visible in taskbar (default false)
                new System.Drawing.Font("Calibri", 10F, System.Drawing.FontStyle.Bold));                        //Set font (default by system)
            //Check InputBox result
            if (res == System.Windows.Forms.DialogResult.OK || res == System.Windows.Forms.DialogResult.Yes)
                listView1.Items.Add(InputBox.ResultValue);                                                      //Get returned value
        }
        private void Form1_Load(object sender, EventArgs e)
        {
            listView1.Columns.Add("Result", 100);
            listView1.View = View.Details;
        }
    }
}
