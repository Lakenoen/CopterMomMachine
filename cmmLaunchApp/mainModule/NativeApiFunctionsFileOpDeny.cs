using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace mainModule;
public class NativeApiFunctionsFileOpDeny : NativeApiFunctions
{
    protected override Dictionary<string, Delegate> functions { get; } = new Dictionary<string, Delegate>()
    {
        { "CreateFileA",new HookCreateFileDelegate(HookCreateFileA) },
        { "CreateFileW", new HookCreateFileDelegate(HookCreateFileW) },
        { "OpenFile", new HookOpenFileDelegate(HookOpenFile) },
        { "DeleteFileA", new HookDeleteFileDelegate(HookDeleteFileA) },
        { "DeleteFileW", new HookDeleteFileDelegate(HookDeleteFileW)},
        { "CopyFileA", new HookCopyFileDelegate(HookCopyFileA) },
        { "CopyFileW", new HookCopyFileDelegate(HookCopyFileW) },
        { "FindFirstFileA", new HookFindFirstFileDelegate(HookFindFirstFileA) },
        { "FindFirstFileW", new HookFindFirstFileDelegate(HookFindFirstFileW) },
        { "CreateDirectoryA", new HookCreateDirectoryDelegate(HookCreateDirectoryA) },
        { "CreateDirectoryW", new HookCreateDirectoryDelegate(HookCreateDirectoryW) },
        { "RemoveDirectoryA", new HookRemoveDirectoryDelegate(HookRemoveDirectoryA) },
        { "RemoveDirectoryW", new HookRemoveDirectoryDelegate(HookRemoveDirectoryW) },
    };

    public NativeApiFunctionsFileOpDeny() : base()
    {

    }

}
