using System.ComponentModel.Design;
using System.Runtime.InteropServices;
using System.Text;
using mainModule;

static class Programm
{

    //TestAppCpp.exe
    //encrypt access_token TestAppCpp.exe TestAppCpp.cmm\

    public static string encryptKey { get; private set; } = "";

    public static void encryptForTest(string[] args)
    {
        ApiProvider.Instance.register();
        CryptoProvider.makeCryptoFile(args[2], Encoding.ASCII.GetBytes(encryptKey), NativeLauncher.ReadPEFile(args[1]).ToArray());
    }

    public static void run(string[] args)
    {
        Task listenTask = ApiProvider.Instance.Listen();
        ApiProvider.Instance.fillID();
        ApiProvider.Instance.auth();
        listenTask.Wait();
        NativeLauncher launcher = new NativeLauncher(args[1]);
        NativeApiFunctions allows = new NativeApiFunctionsAllAccess();
        launcher.run();
    }
    public static void Main(string[] args)
    {

        if (args.Length == 0)
        {
            Console.WriteLine("Error: args length = 0");
            return;
        }

        try
        {
            Console.WriteLine("ATTENTION!!! All the launcher functionality associated with encryption of the PE file is tested, and should not begin in the release,\r\nThe launcher is intended only for decoding and starting, encryption of the file of the file should be produced by the developer of the source application.\r\nThe developer must, together with the encrypted application, supply the launcher and a file with ID for identification on the authorization server");
            if (args[0] == "help")
            {
                Console.Write("dbgEncrypt <file path> <encrypt file name> - make encrypted PE file (for test)\nrun <encrypt file path> - run encrypt file");
                return;
            }

            /*ВНИМАНИЕ!!! Весь функционал лаунчера связанный с шифрованием PE файла явялется тестовым, и в релизе пристутсвовать не должен,
            лаунчер предназначен только для расшифровки и запуска, шифрование PE файла должно производится разработчиком исходного приложения.
            Разработчик должен вместе с зашифрованым приложением поставлять лаунчер и файл с ID для идентификации на сервере авторизации*/

            if (args[0] == "dbgEncrypt")
            {
                encryptForTest(args);
            }
            else if(args[0] == "run")
            {
                run(args);
            }
        } catch (Exception ex){
            Console.WriteLine(ex.ToString());
            return;
        }
    }
}
