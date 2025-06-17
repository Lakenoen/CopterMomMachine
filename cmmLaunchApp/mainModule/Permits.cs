using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;


 //TODO
namespace mainModule;
public class Permits
{
    public List<NativeApiFunctions> permits { get; set; } = new List<NativeApiFunctions>();
    private const string pathToPermitFile = "";
    public Permits(List<NativeApiFunctions> permits)
    {
        this.permits = permits;
    }

    public Permits()
    {
        update();
    }

    public void update()
    {
        permits.Clear();

    }

}
