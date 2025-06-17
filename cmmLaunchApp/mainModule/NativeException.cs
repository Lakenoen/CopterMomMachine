using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace mainModule;
public class NativeException : Exception
{
    public NativeException(string message)
        : base(message)
    {
        
    }

}
