using System.Net.NetworkInformation;

namespace MyDependencyClassLibrary
{
    public class MyDependency
    {
        public IPStatus Status { get; }
    }
}

namespace System.Net.NetworkInformation
{
    public class MyDynamicDependency
    {
        public IPStatus Status
        {
            get
            {
                return MyMockingClassLibrary.SomeClass.DoStuff(this, nameof(Status), GetType().GetMethod(nameof(Status)).GetParameters());
            }
        }
    }
}