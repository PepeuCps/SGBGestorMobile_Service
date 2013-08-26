using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using SGBGestor_SERVICE.Models;

namespace SGBGestor_SERVICE.Utils
{
    public class ParserHelper
    {
        public List<ProdutoIntegration> ParseProducts(String codmensagem, List<Produtos> produtos)
        {
            List<ProdutoIntegration> lista_produtos = new List<ProdutoIntegration>();

            foreach (var p in produtos)
            {
                ProdutoIntegration produto = new ProdutoIntegration();
                produto.descricao = p.descricao;
                produto.quantidade = p.qtde;
                produto.codmensagem = codmensagem;
                produto.codproduto = p.codProduto;
                lista_produtos.Add(produto);
            }
                        
            return lista_produtos;
        }

        /// <summary>
        /// Count occurrences of strings.
        /// </summary>
        private static int CountStringOccurrences(string text, string pattern)
        {
            // Loop through all instances of the string 'text'.
            int count = 0;
            int i = 0;
            while ((i = text.IndexOf(pattern, i)) != -1)
            {
                i += pattern.Length;
                count++;
            }
            return count;
        }

    }

}