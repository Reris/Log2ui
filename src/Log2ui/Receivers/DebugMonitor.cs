using System;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.InteropServices;
using System.Threading;

namespace Log2ui.Receivers;

/// <summary>
/// Delegate used when firing DebugMonitor.OnOutputDebug event
/// </summary>
public delegate void OnOutputDebugStringHandler(int pid, string text);

/// <summary>
/// This class captures all strings passed to <c>OutputDebugString</c> when
/// the application is not debugged.
/// </summary>
/// <remarks>
/// This class is a port of Microsofts Visual Studio's C++ example "dbmon", which
/// can be found at <c>http://msdn.microsoft.com/library/default.asp?url=/library/en-us/vcsample98/html/vcsmpdbmon.asp</c>.
/// </remarks>
/// <remarks>
///     <code>
/// 			public static void Main(string[] args) {
/// 				DebugMonitor.Start();
/// 				DebugMonitor.OnOutputDebugString += new OnOutputDebugStringHandler(OnOutputDebugString);
/// 				Console.WriteLine("Press 'Enter' to exit.");
/// 				Console.ReadLine();
/// 				DebugMonitor.Stop();
/// 			}
/// 			
/// 			private static void OnOutputDebugString(int pid, string text) {
/// 				Console.WriteLine(DateTime.Now + ": " + text);
/// 			}
/// 		</code>
/// </remarks>
public static class DebugMonitor
{
    /// <summary>
    /// Event handle for slot 'DBWIN_BUFFER_READY'
    /// </summary>
    private static IntPtr _ackEvent = IntPtr.Zero;

    /// <summary>
    /// Event handle for slot 'DBWIN_DATA_READY'
    /// </summary>
    private static IntPtr _readyEvent = IntPtr.Zero;

    /// <summary>
    /// Handle for our shared file
    /// </summary>
    private static IntPtr _sharedFile = IntPtr.Zero;

    /// <summary>
    /// Handle for our shared memory
    /// </summary>
    private static IntPtr _sharedMem = IntPtr.Zero;

    /// <summary>
    /// Our capturing thread
    /// </summary>
    private static Thread? _capturer;

    /// <summary>
    /// Our synchronization root
    /// </summary>
    private static readonly object SyncRoot = new();

    /// <summary>
    /// Mutex for singleton check
    /// </summary>
    private static Mutex? _mutex;

    /// <summary>
    /// Fired if an application calls <c>OutputDebugString</c>
    /// </summary>
    public static event OnOutputDebugStringHandler? OnOutputDebugString;

    /// <summary>
    /// Starts this debug monitor
    /// </summary>
    public static void Start()
    {
        lock (DebugMonitor.SyncRoot)
        {
            if (DebugMonitor._capturer != null)
            {
                throw new ApplicationException("This DebugMonitor is already started.");
            }

            // Check for supported operating system. Mono (at least with *nix) won't support
            // our P/Invoke calls.
            if (Environment.OSVersion.ToString().IndexOf("Microsoft") == -1)
            {
                throw new NotSupportedException("This DebugMonitor is only supported on Microsoft operating systems.");
            }

            // Check for multiple instances. As the README.TXT of the msdn 
            // example notes it is possible to have multiple debug monitors
            // listen on OutputDebugString, but the message will be randomly
            // distributed among all running instances so this won't be
            // such a good idea.				
            DebugMonitor._mutex = new Mutex(false, typeof(DebugMonitor).Namespace, out var createdNew);
            if (!createdNew)
            {
                throw new ApplicationException("There is already an instance of 'DbMon.NET' running.");
            }

            var sd = new SECURITY_DESCRIPTOR();

            // Initialize the security descriptor.
            if (!DebugMonitor.InitializeSecurityDescriptor(ref sd, SystemConsts.SECURITY_DESCRIPTOR_REVISION))
            {
                throw DebugMonitor.CreateApplicationException("Failed to initializes the security descriptor.");
            }

            // Set information in a discretionary access control list
            if (!DebugMonitor.SetSecurityDescriptorDacl(ref sd, true, IntPtr.Zero, false))
            {
                throw DebugMonitor.CreateApplicationException("Failed to initializes the security descriptor");
            }

            var sa = new SECURITY_ATTRIBUTES();

            // Create the event for slot 'DBWIN_BUFFER_READY'
            DebugMonitor._ackEvent = DebugMonitor.CreateEvent(ref sa, false, false, "DBWIN_BUFFER_READY");
            if (DebugMonitor._ackEvent == IntPtr.Zero)
            {
                throw DebugMonitor.CreateApplicationException("Failed to create event 'DBWIN_BUFFER_READY'");
            }

            // Create the event for slot 'DBWIN_DATA_READY'
            DebugMonitor._readyEvent = DebugMonitor.CreateEvent(ref sa, false, false, "DBWIN_DATA_READY");
            if (DebugMonitor._readyEvent == IntPtr.Zero)
            {
                throw DebugMonitor.CreateApplicationException("Failed to create event 'DBWIN_DATA_READY'");
            }

            // Get a handle to the readable shared memory at slot 'DBWIN_BUFFER'.
            DebugMonitor._sharedFile = DebugMonitor.CreateFileMapping(new IntPtr(-1), ref sa, PageProtection.ReadWrite, 0, 4096, "DBWIN_BUFFER");
            if (DebugMonitor._sharedFile == IntPtr.Zero)
            {
                throw DebugMonitor.CreateApplicationException("Failed to create a file mapping to slot 'DBWIN_BUFFER'");
            }

            // Create a view for this file mapping so we can access it
            DebugMonitor._sharedMem = DebugMonitor.MapViewOfFile(DebugMonitor._sharedFile, SystemConsts.SECTION_MAP_READ, 0, 0, 512);
            if (DebugMonitor._sharedMem == IntPtr.Zero)
            {
                throw DebugMonitor.CreateApplicationException("Failed to create a mapping view for slot 'DBWIN_BUFFER'");
            }

            // Start a new thread where we can capture the output
            // of OutputDebugString calls so we don't block here.
            DebugMonitor._capturer = new Thread(DebugMonitor.Capture);
            DebugMonitor._capturer.Start();
        }
    }

    /// <summary>
    /// Captures
    /// </summary>
    private static void Capture()
    {
        try
        {
            // Everything after the first DWORD is our debugging text
            var pString = new IntPtr(
                DebugMonitor._sharedMem.ToInt32() + Marshal.SizeOf(typeof(int))
            );

            while (true)
            {
                DebugMonitor.SetEvent(DebugMonitor._ackEvent);

                var ret = DebugMonitor.WaitForSingleObject(DebugMonitor._readyEvent, SystemConsts.INFINITE);

                // if we have no capture set it means that someone
                // called 'Stop()' and is now waiting for us to exit
                // this endless loop.
                if (DebugMonitor._capturer == null)
                {
                    break;
                }

                if (ret == SystemConsts.WAIT_OBJECT_0)
                {
                    // The first DWORD of the shared memory buffer contains
                    // the process ID of the client that sent the debug string.
                    DebugMonitor.FireOnOutputDebugString(
                        Marshal.ReadInt32(DebugMonitor._sharedMem),
                        Marshal.PtrToStringAnsi(pString));
                }
            }
        }
        finally
        {
            DebugMonitor.Dispose();
        }
    }

    private static void FireOnOutputDebugString(int pid, string text)
    {
        // Raise event if we have any listeners
        if (DebugMonitor.OnOutputDebugString == null)
        {
            return;
        }

#if !DEBUG
			try {
#endif
        DebugMonitor.OnOutputDebugString(pid, text);
#if !DEBUG
			} catch (Exception ex) {
				Console.WriteLine("An 'OnOutputDebugString' handler failed to execute: " + ex.ToString());
			}
#endif
    }

    /// <summary>
    /// Dispose all resources
    /// </summary>
    private static void Dispose()
    {
        // Close AckEvent
        if (DebugMonitor._ackEvent != IntPtr.Zero)
        {
            if (!DebugMonitor.CloseHandle(DebugMonitor._ackEvent))
            {
                throw DebugMonitor.CreateApplicationException("Failed to close handle for 'AckEvent'");
            }

            DebugMonitor._ackEvent = IntPtr.Zero;
        }

        // Close ReadyEvent
        if (DebugMonitor._readyEvent != IntPtr.Zero)
        {
            if (!DebugMonitor.CloseHandle(DebugMonitor._readyEvent))
            {
                throw DebugMonitor.CreateApplicationException("Failed to close handle for 'ReadyEvent'");
            }

            DebugMonitor._readyEvent = IntPtr.Zero;
        }

        // Close SharedFile
        if (DebugMonitor._sharedFile != IntPtr.Zero)
        {
            if (!DebugMonitor.CloseHandle(DebugMonitor._sharedFile))
            {
                throw DebugMonitor.CreateApplicationException("Failed to close handle for 'SharedFile'");
            }

            DebugMonitor._sharedFile = IntPtr.Zero;
        }


        // Unmap SharedMem
        if (DebugMonitor._sharedMem != IntPtr.Zero)
        {
            if (!DebugMonitor.UnmapViewOfFile(DebugMonitor._sharedMem))
            {
                throw DebugMonitor.CreateApplicationException("Failed to unmap view for slot 'DBWIN_BUFFER'");
            }

            DebugMonitor._sharedMem = IntPtr.Zero;
        }

        // Close our mutex
        if (DebugMonitor._mutex != null)
        {
            DebugMonitor._mutex.Close();
            DebugMonitor._mutex = null;
        }
    }

    /// <summary>
    /// Stops this debug monitor. This call we block the executing thread
    /// until this debug monitor is stopped.
    /// </summary>
    public static void Stop()
    {
        lock (DebugMonitor.SyncRoot)
        {
            if (DebugMonitor._capturer == null)
            {
                throw new ObjectDisposedException("DebugMonitor", "This DebugMonitor is not running.");
            }

            DebugMonitor._capturer = null;
            DebugMonitor.PulseEvent(DebugMonitor._readyEvent);
            while (DebugMonitor._ackEvent != IntPtr.Zero)
            {
                ;
            }
        }
    }

    /// <summary>
    /// Helper to create a new application exception, which has automaticly the
    /// last win 32 error code appended.
    /// </summary>
    /// <param name="text">text</param>
    private static ApplicationException CreateApplicationException(string text)
    {
        if (text == null || text.Length < 1)
        {
            throw new ArgumentNullException("text", "'text' may not be empty or null.");
        }

        return new ApplicationException(
            string.Format(
                "{0}. Last Win32 Error was {1}",
                text,
                Marshal.GetLastWin32Error()));
    }

    #region Win32 API Imports

    [StructLayout(LayoutKind.Sequential)]
    [SuppressMessage("ReSharper", "InconsistentNaming", Justification = "System Type")]
    private struct SECURITY_DESCRIPTOR
    {
        public byte revision;
        public byte size;
        public short control;
        public IntPtr owner;
        public IntPtr group;
        public IntPtr sacl;
        public IntPtr dacl;
    }

    [StructLayout(LayoutKind.Sequential)]
    [SuppressMessage("ReSharper", "InconsistentNaming", Justification = "System Type")]
    private struct SECURITY_ATTRIBUTES
    {
        public int nLength;
        public IntPtr lpSecurityDescriptor;
        public int bInheritHandle;
    }

    [Flags]
    [SuppressMessage("ReSharper", "UnusedMember.Local", Justification = "System Type")]
    private enum PageProtection : uint
    {
        NoAccess = 0x01,
        Readonly = 0x02,
        ReadWrite = 0x04,
        WriteCopy = 0x08,
        Execute = 0x10,
        ExecuteRead = 0x20,
        ExecuteReadWrite = 0x40,
        ExecuteWriteCopy = 0x80,
        Guard = 0x100,
        NoCache = 0x200,
        WriteCombine = 0x400
    }

    [SuppressMessage("ReSharper", "InconsistentNaming", Justification = "System Type")]
    private static class SystemConsts
    {
        public const int WAIT_OBJECT_0 = 0;
        public const uint INFINITE = 0xFFFFFFFF;
        public const int ERROR_ALREADY_EXISTS = 183;
        public const uint SECURITY_DESCRIPTOR_REVISION = 1;
        public const uint SECTION_MAP_READ = 0x0004;
    }

    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern IntPtr MapViewOfFile(
        IntPtr hFileMappingObject,
        uint
            dwDesiredAccess,
        uint dwFileOffsetHigh,
        uint dwFileOffsetLow,
        uint dwNumberOfBytesToMap);

    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern bool UnmapViewOfFile(IntPtr lpBaseAddress);

    [DllImport("advapi32.dll", SetLastError = true)]
    private static extern bool InitializeSecurityDescriptor(ref SECURITY_DESCRIPTOR sd, uint dwRevision);

    [DllImport("advapi32.dll", SetLastError = true)]
    private static extern bool SetSecurityDescriptorDacl(ref SECURITY_DESCRIPTOR sd, bool daclPresent, IntPtr dacl, bool daclDefaulted);

    [DllImport("kernel32.dll")]
    private static extern IntPtr CreateEvent(ref SECURITY_ATTRIBUTES sa, bool bManualReset, bool bInitialState, string lpName);

    [DllImport("kernel32.dll")]
    private static extern bool PulseEvent(IntPtr hEvent);

    [DllImport("kernel32.dll")]
    private static extern bool SetEvent(IntPtr hEvent);

    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern IntPtr CreateFileMapping(
        IntPtr hFile,
        ref SECURITY_ATTRIBUTES lpFileMappingAttributes,
        PageProtection flProtect,
        uint dwMaximumSizeHigh,
        uint dwMaximumSizeLow,
        string lpName);

    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern bool CloseHandle(IntPtr hHandle);

    [DllImport("kernel32", SetLastError = true, ExactSpelling = true)]
    private static extern int WaitForSingleObject(IntPtr handle, uint milliseconds);

    #endregion
}
