using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace SGBGestor_SERVICE.Models
{
    public class ProdutoIntegration
    {
        public string codproduto { get; set; }
        public string codmensagem { get; set; }
        public int quantidade { get; set; }
        public string descricao { get; set; }
        public double valor { get; set; }
    }
}