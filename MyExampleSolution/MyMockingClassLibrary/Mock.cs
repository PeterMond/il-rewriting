using Castle.DynamicProxy;
using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.NetworkInformation;
using System.Reflection;
using System.Reflection.Emit;
using System.Runtime.InteropServices;
using System.Threading;
using System.Windows.Forms;

namespace MyMockingClassLibrary
{
    public interface IMyDynamicInterface
    {
        int GetValue();
    }

    public static class SomeClass
    {
        public static IPStatus DoStuff(object @object, string methodName, ParameterInfo[] parameters)
        {
            MessageBox.Show($"{string.Join(Environment.NewLine, @object.GetType().Name, methodName, string.Join(", ", parameters.Select(parameterInfo => parameterInfo.Name)))}");

            return IPStatus.DestinationUnreachable;
        }
    }

    public static class Mock
    {
        #region Unmanaged Stuff

        public enum Status
        {
            Ready = 1,
            Uninitialized = 0,
            ErrorHookCompileMethodFailed = -1,
            ErrorLoadedMethodDescIteratorInitializationFailed = -2,
            ErrorMethodDescInitializationFailed = -3,
            ErrorDbgHelpNotFound = -4,
            ErrorJitNotFound = -5,
            ErrorDownloadPdbFailed = -6,
            ErrorClrNotFound = -7,
        }

        private struct InjectorMethodName
        {
            public const string GetStatusName = "GetStatus";
            public const string UpdateIlCodesName = "UpdateILCodes";
            public const string WaitForIntializationCompletionName = "WaitForIntializationCompletion";
        }

        [DllImport("kernel32.dll", CharSet = CharSet.Unicode)]
        private static extern IntPtr LoadLibraryW(string lpFileName);

        [DllImport("kernel32.dll", CharSet = CharSet.Ansi)]
        private static extern IntPtr GetProcAddress(IntPtr hModule, string procName);

        [UnmanagedFunctionPointer(CallingConvention.StdCall)]
        private delegate bool UpdateIlCodesDelegate(IntPtr pMethodTable, IntPtr pMethodHandle, int md, IntPtr pBuffer, int dwSize);

        [UnmanagedFunctionPointer(CallingConvention.StdCall)]
        private delegate Status GetStatusDelegate();

        [UnmanagedFunctionPointer(CallingConvention.StdCall)]
        private delegate Status WaitForIntializationCompletionDelegate();

        #endregion Unmanaged Stuff

        public static T Create<T>() //where T : class, new()
        {
            const string methodName = "get_Status";
            //-----------------------------------------------------------------
            // Configuration
            //-----------------------------------------------------------------

            #region Configuration

            const string symSrvFilePath64 = @"C:\Program Files (x86)\Microsoft Visual Studio\2017\Professional\Common7\IDE\Remote Debugger\x64\symsrv.dll";
            const string profilerDllFilePath = @"C:\Users\tbowen\Desktop\MyExampleSolution\MyExampleSolution\MyCPlusPlusProject\x64\Release\MyCPlusPlusProject.dll";
            const string debugDirectoryPath = @"C:\Users\tbowen\Desktop\MyExampleSolution\MyExampleSolution\MyClassLibrary.Tests\bin\Debug";
            const string releaseDirectoryPath = @"C:\Users\tbowen\Desktop\MyExampleSolution\MyExampleSolution\MyClassLibrary.Tests\bin\Release";
            const string externallDllsNeededDirectoryPath = @"C:\Users\tbowen\Desktop\MyExampleSolution\MyExampleSolution\ExternalDLLsNeeded\x64 (Place in running test library)";
            const string myDependencyClassLibraryDllFilePath = @"C:\Users\tbowen\Desktop\MyExampleSolution\MyExampleSolution\ExternalDLLsNeeded\artifacts\MyDependencyClassLibrary.dll";
            const string myDependencyClassLibraryPdbFilePath = @"C:\Users\tbowen\Desktop\MyExampleSolution\MyExampleSolution\ExternalDLLsNeeded\artifacts\MyDependencyClassLibrary.pdb";

            var symchkFilePath = $@"{externallDllsNeededDirectoryPath}\symchk.exe";
            var dbg64FilePath = $@"{externallDllsNeededDirectoryPath}\dbg64.dll";
            var symbolCheckFilePath = $@"{externallDllsNeededDirectoryPath}\SymbolCheck.dll";

            try
            {
                File.Copy(dbg64FilePath, $@"{releaseDirectoryPath}\{Path.GetFileName(dbg64FilePath)}", overwrite: true);
                File.Copy(profilerDllFilePath, $@"{releaseDirectoryPath}\{Path.GetFileName(symSrvFilePath64)}", overwrite: true);
                File.Copy(symbolCheckFilePath, $@"{releaseDirectoryPath}\{Path.GetFileName(symbolCheckFilePath)}", overwrite: true);
                File.Copy(symchkFilePath, $@"{releaseDirectoryPath}\{Path.GetFileName(symchkFilePath)}", overwrite: true);
                File.Copy(symSrvFilePath64, $@"{releaseDirectoryPath}\{Path.GetFileName(symSrvFilePath64)}", overwrite: true);
                try { Directory.Delete($@"{releaseDirectoryPath}\cache", recursive: true); } catch { }
                try { Directory.Delete($@"{releaseDirectoryPath}\PDB_symbols", recursive: true); } catch { }
            }
            catch (Exception exception)
            {
                MessageBox.Show(exception.Message);
            }

            #endregion Configuration

            //-----------------------------------------------------------------
            // Get Method Source
            //-----------------------------------------------------------------
            //var typeSource = Assembly.LoadFile(@"C:\Users\tbowen\Desktop\MyExampleSolution\MyExampleSolution\MyClassLibrary.Tests\bin\Release\MyDependencyClassLibrary.dll").DefinedTypes.Single(typeInfo => typeInfo.Name == "MyDependency");
            var typeSource = typeof(T);
            var methodInfoSource = typeSource.GetMethod(methodName);
            //-----------------------------------------------------------------
            // Get Method Target
            //-----------------------------------------------------------------
            var definedTypes = Assembly.LoadFile(@"C:\Users\tbowen\Desktop\MyExampleSolution\MyExampleSolution\MyClassLibrary.Tests\bin\Release\MyDependencyClassLibrary.dll").DefinedTypes;
            var typeTarget = definedTypes.Single(typeInfo => typeInfo.Name == "MyDynamicDependency");
            var methodInfoTarget = typeTarget.GetMethod("GetValue");
            //-----------------------------------------------------------------
            // Assign Instructions
            //-----------------------------------------------------------------
            var ilAsByteArrayTarget = methodInfoTarget.GetMethodBody().GetILAsByteArray();
            //-----------------------------------------------------------------
            UpdateIlCodes(profilerDllFilePath, methodInfoSource, ilAsByteArrayTarget);
            //-----------------------------------------------------------------
            var mock = Activator.CreateInstance<T>();
            //-----------------------------------------------------------------

            return mock;
        }

        private static void UpdateIlCodes(string profilerDllFilePath, MethodInfo methodInfoSource, byte[] ilAsByteArrayTarget)
        {
            //-----------------------------------------------------------------
            // Initialization
            //-----------------------------------------------------------------
#if DEBUG
            throw new Exception($"{Path.GetFileName(profilerDllFilePath)} is only designed for .Net release mode process. it is not supposed to be used for debug mode.");
#endif
            //-----------------------------------------------------------------
            var intPtrModule = LoadLibraryW(profilerDllFilePath);

            if (intPtrModule == IntPtr.Zero)
            {
                throw new FileLoadException(profilerDllFilePath);
            }

            var intPtrUpdateIlCodes = GetProcAddress(intPtrModule, InjectorMethodName.UpdateIlCodesName);

            if (intPtrUpdateIlCodes == IntPtr.Zero)
            {
                throw new MethodAccessException(InjectorMethodName.GetStatusName);
            }

            var updateIlCodesMethod = (UpdateIlCodesDelegate)Marshal.GetDelegateForFunctionPointer(intPtrUpdateIlCodes, typeof(UpdateIlCodesDelegate));
            var intPtrGetStatus = GetProcAddress(intPtrModule, InjectorMethodName.GetStatusName);

            if (intPtrGetStatus == IntPtr.Zero)
            {
                throw new MethodAccessException(InjectorMethodName.GetStatusName);
            }

            var getStatusDelegate = (GetStatusDelegate)Marshal.GetDelegateForFunctionPointer(intPtrGetStatus, typeof(GetStatusDelegate));
            var intPtrWaitForIntializationCompletion = GetProcAddress(intPtrModule, InjectorMethodName.WaitForIntializationCompletionName);

            if (intPtrWaitForIntializationCompletion == IntPtr.Zero)
            {
                throw new MethodAccessException(InjectorMethodName.WaitForIntializationCompletionName);
            }

            var waitForIntializationCompletionDelegate = (WaitForIntializationCompletionDelegate)Marshal.GetDelegateForFunctionPointer(intPtrWaitForIntializationCompletion, typeof(WaitForIntializationCompletionDelegate));

            if (waitForIntializationCompletionDelegate.Invoke() == Status.Uninitialized)
            {
                throw new Exception(nameof(Status.Uninitialized));
            }
            //-----------------------------------------------------------------
            // Modification
            //-----------------------------------------------------------------
            var intPtrPeMethodTable = IntPtr.Zero;

            if (methodInfoSource.DeclaringType != null)
            {
                intPtrPeMethodTable = methodInfoSource.DeclaringType.TypeHandle.Value;
            }

            var intPtrMethod = methodInfoSource is DynamicMethod ? IntPtr.Zero : methodInfoSource.MethodHandle.Value;
            var pBuffer = Marshal.AllocHGlobal(cb: ilAsByteArrayTarget.Length);

            if (pBuffer == IntPtr.Zero)
            {
                throw new OutOfMemoryException();
            }

            Marshal.Copy(source: ilAsByteArrayTarget, startIndex: 0, destination: pBuffer, length: ilAsByteArrayTarget.Length);

            Trace.WriteLine(methodInfoSource.Name);

            updateIlCodesMethod(intPtrPeMethodTable, intPtrMethod, methodInfoSource.MetadataToken, pBuffer, ilAsByteArrayTarget.Length);
            //-----------------------------------------------------------------
            // Verification
            //-----------------------------------------------------------------
            Status status;

            var retryCount = 0;

            do
            {
                status = (Status)getStatusDelegate?.Invoke();

                const int threeSeconds = 3000;

                Thread.Sleep(threeSeconds);

                retryCount++;

                if (retryCount >= 7)
                {
                    throw new Exception($"{nameof(Status)}: {status}, {nameof(retryCount)}: {retryCount}");
                }
            }
            while (status != Status.Ready);
            //-----------------------------------------------------------------
        }
    }

    [Serializable]
    public class ProxyGenerationHook : IProxyGenerationHook
    {
        public bool ShouldInterceptMethod(Type type, MethodInfo memberInfo)
        {
            return true;
        }

        public void NonProxyableMemberNotification(Type type, MemberInfo memberInfo)
        {
        }

        public void MethodsInspected()
        {
            MessageBox.Show(nameof(MethodsInspected));
        }
    }

    [Serializable]
    public class InterceptorSelector : IInterceptorSelector
    {
        public IInterceptor[] SelectInterceptors(Type type, MethodInfo method, IInterceptor[] interceptors)
        {
            return interceptors;
        }
    }

    [Serializable]
    public class Interceptor : IInterceptor
    {
        public static IInvocation Invocation;

        public void Intercept(IInvocation invocation)
        {
            MessageBox.Show(invocation.Method.Name, nameof(Invocation));

            // this is just to by pass the null reference exception for now.
            invocation.ReturnValue = 7;
        }
    }
}