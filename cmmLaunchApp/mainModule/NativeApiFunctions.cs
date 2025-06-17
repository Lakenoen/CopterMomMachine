using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace mainModule;
public class NativeApiFunctions : IEnumerable
{
    protected virtual Dictionary<string, Delegate> functions { get; } = new Dictionary<string, Delegate>()
    {
        
    };

    virtual public Delegate? this[string funcName]
    {
        get
        {
            if (!functions.ContainsKey(funcName))
                return null;
            return functions[funcName];
        }
    }
    public NativeApiFunctions() { }

    virtual public IEnumerator GetEnumerator()
    {
        return functions.GetEnumerator();
    }

    public static int HookCreateFileA(string path, uint access, uint mode, IntPtr secAttr, uint dispos, uint flag, int temp)
    {
        return 0;
    }

    public static int HookCreateFileW(string path, uint access, uint mode, IntPtr secAttr, uint dispos, uint flag, int temp)
    {
        return 0;
    }

    public static int HookOpenFile(string path, IntPtr ofstruct, uint style)
    {
        return -1;
    }

    public static bool HookDeleteFileA(string path)
    {
        return false;
    }
    public static bool HookDeleteFileW(string path)
    {
        return false;
    }

    public static bool HookCopyFileA(string oldPath, string newPath, bool ifExist)
    {
        return false;
    }

    public static bool HookCopyFileW(string oldPath, string newPath, bool ifExist)
    {
        return false;
    }

    public static IntPtr HookFindFirstFileA(string fileName, IntPtr pStruct)
    {
        return IntPtr.Zero;
    }

    public static IntPtr HookFindFirstFileW(string fileName, IntPtr pStruct)
    {
        return IntPtr.Zero;
    }

    public static bool HookCreateDirectoryA(string path, IntPtr secure)
    {
        return false;
    }

    public static bool HookCreateDirectoryW(string path, IntPtr secure)
    {
        return false;
    }

    public static bool HookRemoveDirectoryA(string path)
    {
        return false;
    }

    public static bool HookRemoveDirectoryW(string path)
    {
        return false;
    }


    public delegate int HookCreateFileDelegate(string path, uint access, uint mode, IntPtr secAttr, uint dispos, uint flag, int temp);
    public delegate int HookOpenFileDelegate(string path, IntPtr ofstruct, uint style);
    public delegate bool HookDeleteFileDelegate(string path);
    public delegate bool HookCopyFileDelegate(string oldPath, string newPath, bool ifExist);
    public delegate IntPtr HookFindFirstFileDelegate(string fileName, IntPtr pStruct);
    public delegate bool HookCreateDirectoryDelegate(string path, IntPtr secure);
    public delegate bool HookRemoveDirectoryDelegate(string path);
}
