using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Serialization;
using Kuznyechik;

namespace mainModule;
public static class CryptoProvider
{

    public static byte[] encrypt(byte[] key, byte[] msg)
    {
        byte[] res = new byte[msg.Length];
        msg.CopyTo(res, 0);
        Scrambler scrambler = new Scrambler(key);
        scrambler.Encrypt(ref res);
        return res;
    }

    public static byte[] decrypt(byte[] key, byte[] msg)
    {
        if (key.Length < 32)
        {
            var temp = new byte[32];
            key.CopyTo(temp, 0);
            key = temp;
        }
        byte[] res = new byte[msg.Length];
        msg.CopyTo(res,0);
        Scrambler scrambler = new Scrambler(key);
        scrambler.Decrypt(ref res);
        return res;
    }

    public static void makeCryptoFile(string name,byte[] key, byte[] data)
    {
        if(key.Length < 32)
        {
            var temp = new byte[32];
            key.CopyTo(temp, 0);
            key = temp;
        }
        byte[] encryptData = encrypt(key, data);
        using (FileStream stream = File.Open(name, FileMode.OpenOrCreate, FileAccess.Write, FileShare.Read))
        {
            using (BufferedStream buffer = new BufferedStream(stream))
            {
                using (BinaryWriter writer = new BinaryWriter(buffer))
                {
                    writer.Write(encryptData);
                }
            }
        }
    }

}
