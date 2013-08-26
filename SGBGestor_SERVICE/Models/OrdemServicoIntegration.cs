using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace SGBGestor_SERVICE.Models
{
    public class OrdemServicoIntegration
    {
        public String codmensagem;
        public String mensagem;
        public long data;
        public int status;
        public bool coletar_ofr;
        public bool editar_produtos;
        public bool is_vip;
        public bool concorrencia;
        public List<ProdutoIntegration> produtos;
        public int superbotao;
        public int origem;
    }
}