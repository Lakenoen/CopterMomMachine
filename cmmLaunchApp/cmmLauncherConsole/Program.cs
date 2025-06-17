using System.ComponentModel.Design;
using System.Runtime.InteropServices;
using System.Text;
using mainModule;

static class Programm
{

    //TestAppCpp.exe
    //encrypt access_token TestAppCpp.exe TestAppCpp.cmm
    public static void Main(string[] args)
    {
        if (args.Length == 0)
        {
            Console.WriteLine("Error: args length = 0");
            return;
        }

        try
        {
            if (args[0] == "help")
            {
                Console.Write("encrypt <key> <file path> <encrypt file name> - make encrypted PE file\nrun <encrypt file path> - run encrypt file");
            }
            else if (args[0] == "encrypt")
            {
                byte[] key = Encoding.ASCII.GetBytes(args[1]);
                CryptoProvider.makeCryptoFile(args[3], key, NativeLauncher.ReadPEFile(args[2]).ToArray());
            }
            else if(args[0] == "run")
            {
                Task listenTask = ApiProvider.Instance.Listen();
                ApiProvider.Instance.auth();
                listenTask.Wait();
                NativeLauncher launcher = new NativeLauncher(args[1]);
                NativeApiFunctions allows = new NativeApiFunctionsAllAccess();
                launcher.run();
            }
        } catch (Exception ex){
            Console.WriteLine(ex.ToString());
            return;
        }
    }
}
