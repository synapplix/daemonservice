using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;

namespace DeamonService.UI
{
    public partial class DebugConsole : Form
    {
        private DeamonService deamonService;
        public DebugConsole(DeamonService deamonService)
        {
            this.deamonService = deamonService;
            InitializeComponent();
        }

        private void actionButton_Click(object sender, EventArgs e)
        {
            if ("&Start" == actionButton.Text)
            {
                try
                {
                    deamonService.Run(new String[0]);
                    
                    if(outputBox.Text.Length>0)
                        outputBox.Text = outputBox.Text + "\r\n";
                    
                    outputBox.Text = outputBox.Text + "Started!";
                }
                catch (Exception ex){
                    outputBox.Text = outputBox.Text + "\r\n" + ex.Message;
                }
                actionButton.Text = "&Stop";
            }
            else {
                try
                {
                    deamonService.StopService();
                    outputBox.Text = outputBox.Text + "\r\n" + "Stopped!";
                }
                catch (Exception ex)
                {
                    outputBox.Text = outputBox.Text + "\r\n" + ex.Message;
                }
                actionButton.Text = "&Start";
            }
        }
    }
}
