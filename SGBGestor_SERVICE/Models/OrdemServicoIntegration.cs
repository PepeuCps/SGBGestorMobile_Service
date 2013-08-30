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
        public bool editar_produtos;
        public bool is_vip;
        public bool concorrencia;
        public List<ProdutoIntegration> produtos;        
        public String cliente;    
        public String cod_cliente;    
        public String telefone;    
        public String logradouro;    
        public String numero;
        public String bairro;
        public String cidade;
        public String complemento;
        public String referencia;
        public String data_entrega;
        public String forma_pgto;
        public String troco;
        public String valor_total;
        public String obs;
    }
}