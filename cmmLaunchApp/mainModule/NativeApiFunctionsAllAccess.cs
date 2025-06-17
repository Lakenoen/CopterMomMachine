using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace mainModule;
public class NativeApiFunctionsAllAccess : NativeApiFunctions
{
    protected override Dictionary<string, Delegate> functions { get; } = new Dictionary<string, Delegate>()
    {
        
    };

    public NativeApiFunctionsAllAccess() : base()
    {

    }

}
