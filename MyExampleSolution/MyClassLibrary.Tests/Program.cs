using MyMockingClassLibrary;
using System.Net.NetworkInformation;
using System.Windows.Forms;
using Xunit;

namespace MyClassLibrary.Tests
{
    public class Program
    {
        public static void Main()
        {
            var myDependency = Mock.Create<PingReply>();
            var testObject = new MyClassUnderTest(myDependency);

            var result = testObject.MyMethodUnderTest();

            MessageBox.Show(result.ToString(), nameof(result));

            Assert.Equal(IPStatus.DestinationUnreachable, result);
        }
    }
}