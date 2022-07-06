#pragma warning disable CS1591,CS1573,CS0465,CS0649,CS8019,CS1570,CS1584,CS1658,CS0436,CS8981

namespace Windows.Win32
{
    using global::System;
    using global::System.Diagnostics;
    using global::System.Runtime.CompilerServices;
    using global::System.Runtime.InteropServices;
    using global::System.Runtime.Versioning;
    using winmdroot = global::Windows.Win32;

    internal unsafe partial class PInvoke
    {
        public static unsafe winmdroot.Foundation.PCWSTR MAKEINTRESOURCE(int value) => MAKEINTRESOURCE(&value);

        public static unsafe winmdroot.Foundation.PCWSTR MAKEINTRESOURCE(short value) => MAKEINTRESOURCE(&value);

        public static unsafe winmdroot.Foundation.PCWSTR MAKEINTRESOURCE(ushort value) => MAKEINTRESOURCE(&value);

        public static unsafe winmdroot.Foundation.PCWSTR MAKEINTRESOURCE(void* ptr) => (char*)*(ushort*)ptr;
    }
}