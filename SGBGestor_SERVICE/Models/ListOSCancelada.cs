using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace SGBGestor_SERVICE.Models
{
    public class ListOSCancelada
    {
        public List<CancelOSIntegration> lista;

        public ListOSCancelada()
        {
            lista = new List<CancelOSIntegration>();
        }
    }
}