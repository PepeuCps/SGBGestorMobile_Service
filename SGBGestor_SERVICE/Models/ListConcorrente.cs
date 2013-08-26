using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace SGBGestor_SERVICE.Models
{
    public class ListConcorrente
    {
        public List<ConcorrenteIntegration> lista;

        public ListConcorrente()
        {
            lista = new List<ConcorrenteIntegration>();
        }
    }
}