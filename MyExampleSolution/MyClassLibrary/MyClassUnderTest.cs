using System.Net.NetworkInformation;
using MyDependencyClassLibrary;
using System.Windows.Forms;

namespace MyClassLibrary
{
    public interface IMyClassUnderTest
    {
        IPStatus MyMethodUnderTest();
    }

    public class MyClassUnderTest : IMyClassUnderTest
    {
        private readonly PingReply _myDependency;

        public MyClassUnderTest(PingReply myDependency)
        {
            _myDependency = myDependency;
        }

        public IPStatus MyMethodUnderTest()
        {
            MessageBox.Show("About to make call in implementation...");

            var value = _myDependency.Status;

            return value;
        }
    }
}