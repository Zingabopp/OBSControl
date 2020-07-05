using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OBSControl.UI.Formatters
{
    public class BoolFormatter : ICustomFormatter
    {
        public string Format(string format, object arg, IFormatProvider formatProvider)
        {

            if (arg is bool boolVal && format.Contains('|'))
            {
                return boolVal switch
                {
                    true => format.Substring(0, format.IndexOf('|')),
                    false => format.Substring(format.IndexOf('|') + 1)
                };
            }
            else
                return "<ERROR>";
        }
    }
}
