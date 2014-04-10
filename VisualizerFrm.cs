using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Windows.Forms;

namespace FASTA_to_RSRS
{
    public partial class VisualizerFrm : Form
    {
        List<string> markerList = null;

        Hashtable mutationStatus = null;

        public VisualizerFrm(List<string> markers)
        {
            markerList = markers;
            //necessary before component is initialized.
            int location = 0;
            mutationStatus = new Hashtable();
            string tmp=null;
            foreach (string marker in markerList)
            {
                if (marker.IndexOf(".") != -1) // insertion
                {
                    location = int.Parse(Regex.Replace(marker.Substring(0,marker.IndexOf(".")), "[NATGCatgcD]", ""));
                    if (!mutationStatus.ContainsKey(location))
                        mutationStatus.Add(location, "+"+marker.Substring(marker.IndexOf(".")+2));
                    else
                    {
                        tmp = mutationStatus[location].ToString();
                        mutationStatus.Remove(location);
                        mutationStatus.Add(location, tmp+" +" + marker.Substring(marker.IndexOf(".") + 2));
                    }
                }
                else if (marker.IndexOf("D") != -1) //deletion
                {
                    location = int.Parse(Regex.Replace(marker, "[NATGCatgcD]", ""));
                    if (!mutationStatus.ContainsKey(location))
                        mutationStatus.Add(location, marker.Replace("D", "").Replace(location.ToString(), " ✗"));
                }
                else // modification
                {
                    location = int.Parse(Regex.Replace(marker, "[NATGCatgc]", ""));
                    if (!mutationStatus.ContainsKey(location))
                        mutationStatus.Add(location, marker.Replace(location.ToString(), " → "));
                }
            }
            InitializeComponent();
        }

        private void VisualizerFrm_Load(object sender, EventArgs e)
        {
            
            rtbMap.Text = FASTA_to_RSRS.Properties.Resources.RSRS;
            rtbMap.Select(16000, 569);//HVR1 (16001, 16569)
            rtbMap.SelectionColor = Color.LightGreen;
            rtbMap.Select(0, 574);//HVR2 (1, 574)
            rtbMap.SelectionColor = Color.LightBlue;
            rtbMap.Select(574, 15426);//CR (575, 16000)
            rtbMap.SelectionColor = Color.LightGray;

            
            int location = 0;
            string user_marker = "";
            foreach(string marker in markerList)
            {
                if(marker.IndexOf(".")!=-1)
                {
                    location = int.Parse(Regex.Replace(marker.Substring(0, marker.IndexOf(".")), "[NATGCatgcD]", ""));
                    rtbMap.SelectionStart = location - 1;
                    rtbMap.SelectionLength = 1;
                    rtbMap.SelectionColor = Color.Red;
                    rtbMap.SelectionFont = new Font(rtbMap.Font.FontFamily, rtbMap.Font.Size, FontStyle.Bold);
                }
                else if (marker.IndexOf("D") != -1) //deletion
                {
                    location = int.Parse(Regex.Replace(marker, "[NATGCatgcD]", ""));
                    rtbMap.SelectionStart = location - 1;
                    rtbMap.SelectionLength = 1;
                    rtbMap.SelectionColor = Color.Black;
                    rtbMap.SelectionFont = new Font(rtbMap.Font.FontFamily, rtbMap.Font.Size, FontStyle.Strikeout);                    
                }
                else // modification
                {
                    location = int.Parse(Regex.Replace(marker, "[NATGCatgc]", ""));
                    rtbMap.SelectionStart = location - 1;
                    rtbMap.SelectionLength = 1;
                    rtbMap.SelectionColor = Color.Blue;
                    user_marker = marker.Substring(marker.Length - 1);
                    rtbMap.SelectedText = user_marker;
                    rtbMap.SelectionFont = new Font(rtbMap.Font.FontFamily, rtbMap.Font.Size, FontStyle.Bold);
                }
            }

            rtbMap.SelectionStart = 0;
            rtbMap.SelectionLength = 0;
        }

        private void rtbMap_MouseClick(object sender, MouseEventArgs e)
        {
            if (rtbMap.SelectionLength == 0)
                rtbMap.Select(rtbMap.SelectionStart, 1);
        }

        private void rtbMap_SelectionChanged(object sender, EventArgs e)
        {
            int offset = 0;
            statusLbl.Text = "";
            string pos = "";
            if (rtbMap.SelectionLength > 1)
            {
                statusLbl.Text += "Position " + (rtbMap.SelectionStart - offset + 1).ToString() + "-" + (rtbMap.SelectionStart + rtbMap.SelectionLength).ToString();
            }
            else
            {
                pos = (rtbMap.SelectionStart - offset + 1).ToString();
                statusLbl.Text += "Position " + pos;
                if (mutationStatus[rtbMap.SelectionStart + 1] != null)
                    statusLbl.Text += ", " + mutationStatus[rtbMap.SelectionStart + 1].ToString();
            }
        }
    }
}
