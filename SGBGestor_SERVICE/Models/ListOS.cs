using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace SGBGestor_SERVICE.Models
{
    public class ListOS
    {
        public List<OrdemServicoIntegration> listOS;

        public ListOS()
        {
            listOS = new List<OrdemServicoIntegration>();
        }
    }
}