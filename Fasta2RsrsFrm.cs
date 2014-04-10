using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.IO;
using System.Text.RegularExpressions;
using System.Diagnostics;
using System.Threading;
using System.Collections;

namespace FASTA_to_RSRS
{
    public partial class Fasta2RsrsFrm : Form
    {
        int OP_CHANGE = 0;
        int OP_INSERT = 1;
        int OP_DELETE = 2;

        Process p = null;
        string diff_work_dir = @"C:\Temp\";
        string fasta_file = "";

        List<string> markers_new = new List<string>();

        public Fasta2RsrsFrm()
        {
            InitializeComponent();
        }

        private string FastaSeq(string file)
        {
            StreamReader sr = new StreamReader(file);
            string line = null;
            StringBuilder sb = new StringBuilder();
            line = sr.ReadLine();// ignore header
            while ((line = sr.ReadLine()) != null)
            {
                sb.Append(line);
            }
            sr.Close();
            return sb.ToString();
        }

        private void Fasta2RsrsFrm_Load(object sender, EventArgs e)
        {
            diff_work_dir = Path.GetTempPath() + "Fasta2Rsrs\\";
            Directory.CreateDirectory(diff_work_dir);
            tbMarkers.SelectionStart = 0;
            tbMarkers.SelectionLength = 0;
        }

        private Process execute(string file1,string file2)
        {
            p = new Process();
            p.StartInfo.UseShellExecute = false;
            p.StartInfo.RedirectStandardOutput = true;
            p.StartInfo.CreateNoWindow = true;
            p.StartInfo.WorkingDirectory = diff_work_dir;
            p.StartInfo.FileName = diff_work_dir+"diff.exe";
            p.StartInfo.Arguments = file1+" "+file2 ;
            p.Start();
            return p;
        }

        public string getMarkers(string file)
        {
           string rsrs = FASTA_to_RSRS.Properties.Resources.RSRS.ToUpper();
           string user = FastaSeq(file).ToUpper();
           rsrs = Regex.Replace(rsrs, "(.)", "$1\r\n");
           user = Regex.Replace(user, "(.)", "$1\r\n");
           File.WriteAllText(diff_work_dir+"rsrs.txt",rsrs);
           File.WriteAllText(diff_work_dir + "user.txt", user);
           
           File.WriteAllBytes(diff_work_dir+"diff.exe", FASTA_to_RSRS.Properties.Resources.diff);
           p = execute(diff_work_dir + "rsrs.txt", diff_work_dir + "user.txt");
           StringBuilder sb = new StringBuilder();
           string line = null;
           int op = -1;
           string[] rsrs_pos = null;
           string[] user_pos = null;
           int count = 0;
           string[] rsrs_a = null;
           string[] user_a = null;
           while (!p.StandardOutput.EndOfStream)
           {
               line = p.StandardOutput.ReadLine();
               if (line.IndexOf("c") != -1)
               {
                   // change..
                   op = OP_CHANGE;
                   rsrs_pos = line.Split(new char[] { 'c' })[0].Split(new char[] { ',' });
                   user_pos = line.Split(new char[] { 'c' })[1].Split(new char[] { ',' });
                   count = rsrs_pos.Length;
                   rsrs_a= new string[count];
                   user_a = new string[count];

                   for (int i = 0; i < count; i++)
                   {
                       rsrs_a[i] = p.StandardOutput.ReadLine();
                       rsrs_a[i] = rsrs_a[i].Substring(rsrs_a[i].Length - 1);
                   }
                   p.StandardOutput.ReadLine();// middleline
                   for (int i = 0; i < count; i++)
                   {
                       user_a[i] = p.StandardOutput.ReadLine();
                       user_a[i] = user_a[i].Substring(user_a[i].Length - 1);

                       if((rsrs_a[i]=="A" && user_a[i]=="G")||
                           (rsrs_a[i]=="G" && user_a[i]=="A")||
                           (rsrs_a[i]=="T" && user_a[i]=="C")||
                           (rsrs_a[i]=="C" && user_a[i]=="T")||
                           (rsrs_a[i] == "N" || user_a[i] == "N"))
                            //transition AG TC
                            sb.Append(rsrs_a[i] + rsrs_pos[i] + user_a[i] + " ");
                       else
                           sb.Append(rsrs_a[i] + rsrs_pos[i] + user_a[i].ToLower() + " ");
                   }
               }
               else if (line.IndexOf("a") != -1)
               {
                   // insert..
                   op = OP_INSERT;
                   rsrs_pos = line.Split(new char[] { 'a' })[0].Split(new char[] { ',' });
                   user_pos = line.Split(new char[] { 'a' })[1].Split(new char[] { ',' });
                   count = rsrs_pos.Length;
                   user_a = new string[count];
                   string pos = rsrs_pos[0];     
                    for (int i = 0; i < count; i++)
                    {
                        user_a[i] = p.StandardOutput.ReadLine();
                        user_a[i] = user_a[i].Substring(user_a[i].Length - 1);
                        sb.Append(pos + "." + (i + 1) + user_a[i] + " ");
                    }
               }
               else if (line.IndexOf("d") != -1)
               {
                   // delete..
                   op = OP_DELETE;
                   rsrs_pos = line.Split(new char[] { 'd' })[0].Split(new char[] { ',' });
                   user_pos = line.Split(new char[] { 'd' })[1].Split(new char[] { ',' });
                   count = rsrs_pos.Length;
                   rsrs_a = new string[count];

                    for (int i = 0; i < count; i++)
                    {
                        rsrs_a[i] = p.StandardOutput.ReadLine();
                        rsrs_a[i] = rsrs_a[i].Substring(rsrs_a[i].Length - 1);
                        sb.Append(rsrs_a[i] + rsrs_pos[i] + "D ");
                    }
               }
           }
           return sb.ToString().Trim().Replace(" ", ", ");
        }

        private void timer1_Tick(object sender, EventArgs e)
        {
            timer1.Stop();
            string txt = getMarkers(fasta_file);

            string[] txt2 = txt.Replace(" ", "").Split(new char[] { ',' });
            List<string> markers=new List<string>();
            foreach(string str in txt2)
            {
               // if (str != "3107.1C" && str != "C3106D" && str != "N3107D")

                //C3106D, 3107.1C
                if (str == "C3106D" || str == "3107.1C" || str == "N3107D" || str == "N523D" || str == "N524D")
                {
                    // don't add. 
                }
                else if (str.StartsWith("N523") && !str.EndsWith("D"))
                    markers.Add("522.1" + str.Substring(str.Length-1));
                else if (str.StartsWith("N524") && !str.EndsWith("D"))
                    markers.Add("522.2" + str.Substring(str.Length - 1));
                else
                    markers.Add(str);
            }
            tbMarkers.Text = convertInsDelToMod(markers, fasta_file);
            tbMarkers.SelectionStart = 0;
            tbMarkers.SelectionLength = 0;
        }

        private string convertInsDelToMod(List<string> markers,string file)
        {
            string rsrs = FASTA_to_RSRS.Properties.Resources.RSRS.ToUpper();
            string user = FastaSeq(file).ToUpper();
            int start = 0;
            int end = 0;
            int start_paired=0;
            Stack<int> stack = new Stack<int>();
            Hashtable ht = new Hashtable();
             foreach (string str in markers)
             {
                 if(str.EndsWith("D"))
                 {
                    start=int.Parse(str.Substring(1).Replace("D",""));
                    stack.Push(start);
                 }
                 else if(str.Contains("."))
                 {                     
                     end = int.Parse(str.Substring(0, str.IndexOf(".")));
                     if (stack.Count != 0)
                     {
                         start_paired = stack.Pop();
                         if (stack.Count == 0)
                         {
                             ht.Add(start_paired, end);
                         }
                     }
                 }
             }
             char f;
             char u;
             int offset = 0;
             List<string> new_mut = new List<string>();
             foreach (DictionaryEntry kvp in ht)
             {
                 start=int.Parse(kvp.Key.ToString());
                 end = int.Parse(kvp.Value.ToString());
                 char[] fasta_char = rsrs.ToCharArray();
                 char[] user_char = user.ToCharArray();
                 for(int i=start;i<=end;i++)
                 {
                     if (i > 3107)
                         offset = 1;
                     else
                         offset = 0;
                     f = fasta_char[i - offset];
                     u = user_char[i];
                     if (f != u)
                     {
                         if ((f == 'A' && u == 'G') ||
                           (f == 'G' && u == 'A') ||
                           (f == 'T' && u == 'C') ||
                           (f == 'C' && u == 'T'))
                            new_mut.Add(f.ToString() + i.ToString() + u.ToString());
                         else
                             new_mut.Add(f.ToString() + i.ToString() + u.ToString().ToLower());
                     }
                 }
             }


             markers_new.Clear();

             
             foreach (string str in markers)
             {
                 if (!markers_new.Contains(str))
                     markers_new.Add(str);
             }
             foreach (string str in new_mut)
             {
                 if (!markers_new.Contains(str))
                     markers_new.Add(str);
             }
            //

             foreach (string str in markers)
             {
                 foreach (DictionaryEntry kvp in ht)
                 {
                     start = int.Parse(kvp.Key.ToString());
                     end = int.Parse(kvp.Value.ToString());
                     for (int i = start; i <= end; i++)
                     {
                         if ((str.EndsWith(i + "D") && str.Length == (i + "D").Length + 1) || (str.IndexOf(i + ".") != -1 && str.Length == (i.ToString() + ".").Length + 2))
                         {
                             markers_new.Remove(str);                             
                         }
                     }
                 }
             }

            //
            StringBuilder sb = new StringBuilder();
            foreach (string str in markers_new)
                sb.Append(str + " ");
            return sb.ToString().Trim().Replace(" ", ",");
        }

        private void Fasta2RsrsFrm_FormClosing(object sender, FormClosingEventArgs e)
        {
            try
            {
                if (p != null)
                {
                    if (!p.HasExited)
                        p.Kill();
                    while (!p.HasExited)
                    {
                        Thread.Sleep(100);
                    }
                }
                Directory.Delete(diff_work_dir, true);
            }
            catch (Exception)
            { }
        }

        private void tbMarkers_DragEnter(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(DataFormats.FileDrop))
            {
                e.Effect = DragDropEffects.Copy;
            }
            else
            {
                e.Effect = DragDropEffects.None;
            }
        }

        private void tbMarkers_DragDrop(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(DataFormats.FileDrop))
            {
                string[] filePaths = (string[])(e.Data.GetData(DataFormats.FileDrop));
                fasta_file = filePaths[0];
                timer1.Start();
            }
        }

        private void Fasta2RsrsFrm_HelpButtonClicked(object sender, CancelEventArgs e)
        {
            Process.Start("http://www.y-str.org/tools/fasta-to-rsrs/");
            e.Cancel = true;
        }

        private void exitToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Application.Exit();
        }

        private void openFASTAToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if(openFileDialog1.ShowDialog(this)==DialogResult.OK)
            {
                fasta_file = openFileDialog1.FileName;
                timer1.Start();
            }
            
        }

        private void visualizeToolStripMenuItem_Click(object sender, EventArgs e)
        {
            VisualizerFrm frm = new VisualizerFrm(markers_new);
            frm.ShowDialog(this);
            frm.Dispose();
        }

        private void websiteToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Process.Start("http://www.y-str.org/tools/fasta-to-rsrs/");
        }

        private void aboutToolStripMenuItem_Click(object sender, EventArgs e)
        {
            MessageBox.Show("Developer: Felix Chandrakumar <i@fc.id.au>\r\nWebsite: www.y-str.org\r\nBuild Date: 4-Jan-2014", "About FASTA to RSRS", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }
    }
}
