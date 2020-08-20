using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Indirect.Controls
{
    internal interface IImmersiveSupport
    {
        void OpenImmersiveView(object item);

        void CloseImmersiveView();
    }
}
