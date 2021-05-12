using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Gy001Tools
{
    public partial class Form2 : Form
    {
        public Form2()
        {
            InitializeComponent();
        }
        private readonly string comparePattern = @"(?<left>[^\=\<\>]+)(?<op>[\=\<\>]{1,2})(?<right>[^\=\<\>]+)[\,，]?";

        private void button1_Click(object sender, EventArgs e)
        {
            var matches = Regex.Matches(textBox1.Text, comparePattern);
            StringBuilder sb = new StringBuilder();
            foreach (var item in matches.OfType<Match>())
            {
                var ary = item.Groups["op"];
                //sb.AppendLine(string.Join(",", );
            }
            textBox2.Text = sb.ToString();
        }
    }
}
