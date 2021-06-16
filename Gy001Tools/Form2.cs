using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
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
        [ThreadStatic]
        static Random _WorldRandom;
        public static Random WorldRandom => _WorldRandom ?? (_WorldRandom = new Random());

        private void button1_Click(object sender, EventArgs e)
        {
            var id = new Guid("06AC47A4A0F046E0AE0DD7D8D55BEB2A");
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
            test();
        }

        private void test()
        {
            var str = "v3rate=mds(0.2,0,0.25,0.6,0.25,0.75,0.2,0.75,1),d=4+9,nemhp=lerp(nemhp,v2mhp,v3rate),nemhp=round(nemhp),neatk=lerp(neatk,v2atk,v3rate),neatk=round(neatk),neqlt=lerp(neqlt,v2qlt,v3rate),neqlt=round(neqlt)";
            string patt = @"(?<item>[^\,，]+|{func})[,，]?";
            string subPatt = @".*?\=.*?\(.*?\)";
            patt = patt.Replace("{func}", subPatt);
            var ma = Regex.Matches(str, patt);
            foreach (var item in ma.OfType<Match>())
            {
                var tmp = item.Groups["item"];
                var val = tmp.Value;
            }
        }

        string left = "k1";
        string right = "2";
        ExpressionType Operator = ExpressionType.GreaterThanOrEqual;

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
