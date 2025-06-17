using System;
using System.Runtime.InteropServices;
using static System.Runtime.InteropServices.JavaScript.JSType;
using System.Text;
using static mainModule.NativeLauncher;

namespace mainModule;
public class NativeLauncher : IDisposable
{
    [DllImport("cmmLaunchDll.dll",EntryPoint = "init", CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Unicode)]
    private static extern EntryPoint init(uint id, byte[] data, ulong size, out string errorMsg);

    [DllImport("cmmLaunchDll.dll", EntryPoint = "run", CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Unicode)]
    private static extern unsafe void run(uint id, out string errorMsg);

    [DllImport("cmmLaunchDll.dll", EntryPoint = "close", CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Unicode)]
    private static extern void close(uint id, out string errorMsg);

    [DllImport("cmmLaunchDll.dll",EntryPoint = "setRedirect", CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Unicode)]
    private static extern void setRedirect(uint id, string funcName, IntPtr addrAddr, out string errorMsg);

    [DllImport("cmmLaunchDll.dll", EntryPoint = "removeRedirect", CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Unicode)]
    private static extern void removeRedirect(uint id, string funcName, out string errorMsg);

    [DllImport("cmmLaunchDll.dll", CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Unicode)]
    public static extern void test();

    private string pathToPEFile = "";
    public uint id { get; private set; } = 0;
    private static uint globalId = 0;
    private static object idLocker = new object();
    public List<byte> fileData { get; } = new List<byte>();

    private const int bufferSize = 0x100000;
    private const long maxFileSize = 0x80000000;

    public EntryPoint? Entry { get; private set; } = null;

    public string PathToPEFile
    {
        get
        {
            return pathToPEFile;
        }
        set
        {
            lock (idLocker)
            {
                this.id = ++globalId;
            }
            pathToPEFile = value;
            init();
        }
    }

    public NativeLauncher(string pathToPEFile)
    {
        this.PathToPEFile = pathToPEFile;
    }

    public NativeLauncher()
    {

    }

    ~NativeLauncher()
    {
        Dispose();
    }

    private void ReadPEFile()
    {
        using (FileStream fs = File.Open(PathToPEFile, FileMode.Open, FileAccess.Read, FileShare.Read))
        { 
            if(fs.Length >= maxFileSize)
            {
                throw new Exception("mmmm ur file is so big..."); //TODO
            }
            using(BufferedStream bs = new BufferedStream(fs))
            {
                using (BinaryReader br = new BinaryReader(bs))
                {
                    byte[] buffData = new byte[bufferSize];
                    while ((buffData = br.ReadBytes(bufferSize)).Length > 0)
                    {
                        this.fileData.AddRange(buffData);
                    }
                }
            }
        }
    }

    public static List<byte> ReadPEFile(string path)
    {
        List<byte> result = new List<byte>();
        using (FileStream fs = File.Open(path, FileMode.Open, FileAccess.Read, FileShare.Read))
        {
            if (fs.Length >= maxFileSize)
            {
                throw new Exception("mmmm ur file is so big..."); //TODO
            }
            using (BufferedStream bs = new BufferedStream(fs))
            {
                using (BinaryReader br = new BinaryReader(bs))
                {
                    byte[] buffData = new byte[bufferSize];
                    while ((buffData = br.ReadBytes(bufferSize)).Length > 0)
                    {
                        result.AddRange(buffData);
                    }
                }
            }
        }
        return result;
    }

    private void init()
    {
        try
        {
            Dispose();
        } catch (Exception)
        {

        }
        ReadPEFile();
        byte[] key = Encoding.ASCII.GetBytes(ApiProvider.Instance.accessResp.access_token);
        byte[] decryptData = CryptoProvider.decrypt(key, this.fileData.ToArray());
        string errorMsg = "";
        Entry = init(id, decryptData, (ulong)this.fileData.Count,out errorMsg);
        this.fileData.Clear();
        if (errorMsg != null && errorMsg != "")
            throw new NativeException(errorMsg);
    }

    public void run()
    {
        if (id == 0)
            return;
        string errorMsg = "";
        run(id, out errorMsg);
        if (errorMsg != null && errorMsg != "")
            throw new NativeException(errorMsg);
    }

    public void setRedirect<D>(string funcName, D func) where D : Delegate
    {
        if (func is null)
            return;
        string errorMsg = "";
        setRedirect(id, funcName, Marshal.GetFunctionPointerForDelegate(func), out errorMsg);
        if (errorMsg != null && errorMsg != "")
            throw new NativeException(errorMsg);
    }

    public void removeRedirect(string funcName)
    {
        if (funcName == "")
            return;
        string errorMsg = "";
        removeRedirect(id, funcName, out errorMsg);
        if (errorMsg != null && errorMsg != "")
            throw new NativeException(errorMsg);
    }

    public void Dispose() {
        string errorMsg = "";
        close(id, out errorMsg);
        if (errorMsg != null && errorMsg != "")
            throw new NativeException(errorMsg);
    }

    public delegate void EntryPoint();

}
