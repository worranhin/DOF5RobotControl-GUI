using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace UnitTest
{
    public class UtilTests
    {
        [Fact]
        public void TestBitConverter()
        {
            byte[] bytes = [0x00, 0x01];
            var value = BitConverter.ToInt16(bytes);
            Assert.Equal((short)0x0100, value);

            Int16 value2 = 0x0200;
            byte[] bytes2 = BitConverter.GetBytes(value2);
            Assert.Equal([0x00, 0x02], bytes2);
        }
    }
}
