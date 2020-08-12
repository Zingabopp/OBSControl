using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OBSControl.UI.Formatters
{
    public class TimeFormatter : ICustomFormatter
    {
        public string Format(string format, object arg, IFormatProvider formatProvider)
        {

            if (arg is int seconds)
            {
                var timeSpan = new TimeSpan(0, 0, seconds);
                if (!string.IsNullOrEmpty(format))
                    return timeSpan.ToString(format);
                else
                    return timeSpan.ToString();
            }
            else
                return "<ERROR>";
        }
    }
}
