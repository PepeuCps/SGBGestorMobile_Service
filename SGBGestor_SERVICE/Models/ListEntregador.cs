using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace SGBGestor_SERVICE.Models
{
    public class ListEntregador
    {
        public List<EntregadorIntegration> lista;

        public ListEntregador()
        {
            lista = new List<EntregadorIntegration>();
        }
    }
}