using System;
using System.Collections.Generic;
using System.Text;
using Xunit;

namespace RestTest.Banking.Tests
{
    public static class XUnitExtensions
    {
        public static void ShouldEqual<T>(this T a, T b)
        {
            Assert.Equal(b, a);
        }
        public static void ShouldNotEqual<T>(this T a, T b)
        {
            Assert.NotEqual(b, a);
        }
    }
}
