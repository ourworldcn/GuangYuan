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
        private readonly string comparePattern = @"(?<left>[^\+\-\*\/\=]+)(?<op>[\+\-\*\/\=]+)(?<right>\d+)[\,，]?";

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
        private void Form2_Load(object sender, EventArgs e)
        {
            object obj1 = 3m;
            object obj2 = 2;
            var b1 = (obj1 as IComparable).CompareTo(obj2);
            Guid id = new Guid(System.Convert.FromBase64String("2A2KP+db60q1PLvse6Cczg=="));
            //var id = Guid.NewGuid();
            //“N”、“D”、“B”、“P”或“X”
            Debug.WriteLine($"N:{id:N}\nD:{id:D};B:{id:B};P:{id:P};\nX:{id:X}");

            Dictionary<string, object> dic = new Dictionary<string, object>()
            {
                {"k1",2m},
            };
            var b = GetResult(dic);
        }

        string left = "k1";
        string right = "2";
        ExpressionType Operator = ExpressionType.GreaterThanOrEqual;

        string key = "1";
        bool GetResult(IDictionary<string, object> dic)
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
