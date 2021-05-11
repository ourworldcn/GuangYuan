using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Data.Common;
using System.Data.OleDb;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Gy001Tools
{
    public partial class Form1 : Form
    {
        public Form1()
        {
            InitializeComponent();
        }

        private void btGen_Click(object sender, EventArgs e)
        {
            if (!int.TryParse(tbCount.Text, out int count))
            {
                MessageBox.Show("请输入生成的数量。");
                tbCount.Focus();
                tbCount.SelectAll();
            }
            List<Guid> lst = new List<Guid>();
            for (int i = 0; i < count; i++)
            {
                lst.Add(Guid.NewGuid());
            }
            var guts = string.Join(Environment.NewLine, lst.Select(c => c.ToString("B")));

            tbGuts.Text = guts;

            tbClientGuts.Text = string.Join(Environment.NewLine, lst.Select(c => Convert.ToBase64String(c.ToByteArray())));
            tbGuts.Focus();
            tbGuts.SelectAll();
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            Stopwatch sw = new Stopwatch();
            sw.Start();
            sw.Stop();

            try
            {
                TestExcel();
            }
            catch (Exception err)
            {
                Debug.WriteLine(err.Message);
            }
        }

        private void TestExcel()
        {
            var fullPath = @"d:\Users\zhangchong\Documents\DOL.xls";
            // var connStr = $"Provider = Microsoft.ACE.OLEDB.12.0; Data Source = {fullPath}; Extended Properties = 'Excel 12.0;HDR=Yes;IMEX=1'";
            var connStr = $"provider=Microsoft.jet.oledb.4.0;data source={fullPath};Extended Properties=\"\";Excel 8.0;HDR=YES;";
            OleDbConnection dbConnection = new OleDbConnection(connStr);
            try
            {
                OleDbCommand dbCommand = new OleDbCommand("select * from [路克索神殿地下城$]", dbConnection);
                OleDbDataAdapter dbDataAdapter = new OleDbDataAdapter(dbCommand);
                var dt = new DataTable();
                dbDataAdapter.Fill(dt);
                foreach (var item in dt.Rows.Cast<DataRow>())
                {
                    var f1 = item["f1"];
                    var f2 = item["f2"];
                }
            }
            catch (Exception err)
            {
                Debug.WriteLine(err.Message);
            }
        }
    }
}
