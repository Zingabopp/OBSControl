using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Text;

namespace OBSControlTests
{
	[TestClass]
    public class ColorTests
    {
		[TestMethod]
		public void DefaultBackground()
        {
			Console.WriteLine(ToHtmlStringRGBA(0.7450981f, 0.7450981f, 0.7450981f, 1f));
        }

		public static string ToHtmlStringRGBA(float r, float g, float b, float a)
		{
			return string.Format("{0:X2}{1:X2}{2:X2}{3:X2}", new object[]
			{
				FloatColorToByte(r),
				FloatColorToByte(g),
				FloatColorToByte(b),
				FloatColorToByte(a)
			});
		}

		public static byte FloatColorToByte(float f)
        {
			return (byte)Math.Clamp((int)Math.Round(f * 255f, 0), 0, 255);
        }
	}
}
