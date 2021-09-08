using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Text.RegularExpressions;
using System.Windows.Forms;
using static System.Linq.Expressions.Expression;

namespace Gy001Tools
{
    public partial class Form2 : Form
    {
        public Form2()
        {
            InitializeComponent();
        }
        //private readonly string comparePattern = @"(?<left>[^\+\-\*\/\=]+)(?<op>[\+\-\*\/\=]+)(?<right>\d+)[\,，]?";
        [ThreadStatic]
        private static Random _WorldRandom;
        public static Random WorldRandom => _WorldRandom ?? (_WorldRandom = new Random());

        private void button1_Click(object sender, EventArgs e)
        {
            //var matches = Regex.Matches(textBox1.Text, comparePattern);
            StringBuilder sb = new StringBuilder();
            //foreach (var item in matches.OfType<Match>())
            //{
            //    //sb.AppendLine(string.Join(",", );
            //}
            textBox2.Text = sb.ToString();
        }
        private void Form2_Load(object sender, EventArgs e)
        {
            //IEnumerable<string> coll = new string[] { string.Empty, "", "ds", null, " " };
            //var ary = coll.OfType<string>().ToArray();
            Test();
        }

        private void Test()
        {

        }

        private readonly string left = "k1";
        private readonly string right = "2";
        private readonly ExpressionType Operator = ExpressionType.GreaterThanOrEqual;

        private bool GetResult(IDictionary<string, object> dic)
        {
            decimal left, right;
            if (decimal.TryParse(this.left, out decimal dec))
                left = dec;
            else if (dic.TryGetValue(this.left, out object lObj) && lObj is decimal dec1)
                left = dec1;
            else
                left = 0;
            if (decimal.TryParse(this.right, out dec))
                right = dec;
            else if (dic.TryGetValue(this.right, out object rObj) && rObj is decimal dec1)
                right = dec1;
            else
                right = 0;
            var exp = Expression.MakeBinary(
                Operator,
                Constant(left),
                Constant(right));
            var func = Expression.Lambda<Func<bool>>(exp).Compile();
            return func();
        }
    }


}
