using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Data.SqlTypes;
using SGBGestor_SERVICE.Utils;

namespace SGBGestor_SERVICE.Models
{
    public class Repository
    {
        private ShvGasEntities db;
        private log4net.ILog logger = log4net.LogManager.GetLogger("LogInFile");
        private static int STATUS_TRANSFERIR = 4000;
        private static int STATUS_INICIO_CANCELAMENTO = 4001; 
        

        public Repository() {
            db = new ShvGasEntities();        
        }

        public void SalvarStatus(StatusIntegration status)
        {
            DateTime data = JavadateToNetdate(status.data);
            logger.Info("SalvarStatus : [ telefone: " + status.telefone + " / codmensagem : " + status.codmensagem + " / idstatus: " + status.idstatus + " / data: " + data.ToString("yyyy-MM-dd HH:mm:ss") + " / ofr: " + status.ofr + " / concorrente: " + status.concorrente + "]");

            Solicitacao os;

            if (String.IsNullOrEmpty(status.telefone) || status.telefone.Equals("0"))
                os = db.Solicitacao.Where(s => s.CodMensagem == status.codmensagem).OrderByDescending(i => i.DataOrigem).FirstOrDefault();
            else
                os = db.Solicitacao.Where(s => s.CodMensagem == status.codmensagem && s.Celular == status.telefone).OrderByDescending(i => i.DataOrigem).FirstOrDefault();

            if (os != null)
            {
                bool recuperar_web = (os.RECUPERAR_WEB.HasValue ? os.RECUPERAR_WEB.Value : false);
                
                if (recuperar_web)
                {
                    StatusDescartadosMobile descartar = new StatusDescartadosMobile();
                    descartar.codmensagem = status.codmensagem;
                    descartar.dataStatus = data;
                    descartar.status = status.idstatus;
                    descartar.telefone = status.telefone;
                    descartar.createdAt = DateTime.Now;
                    db.AddToStatusDescartadosMobile(descartar);
                }
                else
                {
                    // atualiza o OFR  se houver
                    if (!String.IsNullOrEmpty(status.ofr))
                        os.wapOfrNr = Convert.ToInt32(status.ofr);

                    // atualiza o QRCODE  se houver
                    if (!String.IsNullOrEmpty(status.qrcode))
                        os.QRCODE = status.qrcode;

                    // atualiza o RESULTADO_TESTE  se houver
                    if (!String.IsNullOrEmpty(status.resultado_teste))
                        os.RESULTADO_TESTE = status.resultado_teste;

                    // atualiza o PERIODICIDADE  se houver
                    if (!String.IsNullOrEmpty(status.periodicidade))
                        os.PERIODICIDADE = status.periodicidade;

                    // atualiza o ASSINOU_COMODATO  se houver
                    if (!String.IsNullOrEmpty(status.assinou_comodato))
                        os.ASSINOU_TERMO = status.assinou_comodato.ToLower().Contains("true");

                    // atualiza o MOTIVO_NAO_MOVIMENTACAO  se houver
                    if (!String.IsNullOrEmpty(status.motivo_nao_movimentacao))
                        os.MOTIVO_NAO_MOVIMENTACAO = status.motivo_nao_movimentacao;

                    // atualiza o MOTIVO_SUPERBOTAO  se houver
                    if (!String.IsNullOrEmpty(status.motivo_superbotao))
                        os.MOTIVO_SUPERBOTAO = status.motivo_superbotao;

                    // atualiza o CONFIRMA_RETIRADA  se houver
                    if (!String.IsNullOrEmpty(status.confirma_retirada))
                        os.CONFIRMA_RETIRADA = status.confirma_retirada.ToLower().Equals("true") ? true : false;

                    HistSolicitacao historico_existente = db.HistSolicitacao.FirstOrDefault(h => h.IDStatus == status.idstatus &&
                        (h.Data.Year == data.Year && h.Data.Month == data.Month && h.Data.Day == data.Day &&
                         h.Data.Hour == data.Hour && h.Data.Minute == data.Minute && h.Data.Second == data.Second)); //Confere a data sem os milliseconds

                    if (historico_existente == null)
                    {
                        HistSolicitacao hist = new HistSolicitacao();
                        hist.IDStatus = status.idstatus;
                        hist.CodMensagem = status.codmensagem;
                        hist.Data = data;
                        hist.DataOrigem = os.DataOrigem;
                        hist.Celular = os.Celular;
                        hist.CodSolicitacao = status.codmensagem;
                        hist.Mensagem = os.Mensagem;
                        hist.Responsavel = os.Responsavel;
                        hist.Tipo = os.Tipo;
                        hist.Observacao = "";
                        hist.Tipo = "Pedido";
                        hist.StatusDispatch = 0;
                        hist.created_by = "android";

                        db.AddToHistSolicitacao(hist);

                        //So altera os dados da OS caso o status seja mais novo que a data do status atual da Os
                        DateTime dataDoStatus = new DateTime(data.Year, data.Month, data.Day, data.Hour, data.Minute, 0, 0);
                        DateTime dataDaOs = new DateTime(os.Data.Year, os.Data.Month, os.Data.Day, os.Data.Hour, os.Data.Minute, 0, 0);

                        //So altera os dados da OS caso o status seja mais novo que a data do status atual da Os
                        if (dataDoStatus >= dataDaOs)
                        {
                            //Altera o status da Solicitação
                            os.IDStatus = status.idstatus;
                            os.Data = data;
                        }

                        //Se for transferencia
                        if (status.idstatus == STATUS_TRANSFERIR)
                        {
                            os.transferedTo = status.transferir_para;
                            os.transferido = false;
                        }

                        if (!String.IsNullOrEmpty(status.concorrente))
                        {
                            Concorrentes concorrente = db.Concorrentes.FirstOrDefault(c => c.descricao == status.concorrente);

                            if(concorrente != null) {
                                Concorrente_X_Produto cxp = new Concorrente_X_Produto();
                                cxp.idConcorrente = concorrente.id;
                                cxp.idSolicitacao = os.IDSolicitacao.ToString();
                                db.Concorrente_X_Produto.AddObject(cxp);
                            }
                        }
                    }
                }

                db.SaveChanges();
            }
        }

        public void AtualizarProduto(ProdutoIntegration produto)
        {
            logger.Info("AtualizarProduto : [ codmensagem : " + produto.codmensagem + " / codproduto: " + produto.codproduto + " / qtd: " + produto.quantidade + "]");

            var solicitacao = db.Solicitacao.Where(s => s.CodMensagem == produto.codmensagem).OrderByDescending(i => i.DataOrigem).FirstOrDefault();

            if (solicitacao != null)
            {
                var produto_encontrado = db.Produtos.FirstOrDefault(p => p.idSolicitacao == solicitacao.IDSolicitacao && p.codProduto == produto.codproduto);
                if (produto_encontrado != null)
                {
                    produto_encontrado.wapQtdeEntregue = produto.quantidade;
                    db.SaveChanges();
                }
            }
        }

        public void InformarAtraso(AtrasoIntegration atraso)
        {
            logger.Info("InformarAtraso : [ codmensagem : " + atraso.codmensagem + " / horario: " + atraso.horario + " ]");

            var solicitacao = db.Solicitacao.Where(s => s.CodMensagem == atraso.codmensagem).OrderByDescending(i => i.DataOrigem).FirstOrDefault();

            if (solicitacao != null)
            {
                solicitacao.ATRASO = atraso.horario;
                db.SaveChanges();
            }
        }

        public void InformarAtrasos(List<AtrasoIntegration> lista_atraso)
        {
            logger.Info("InformarAtrasos");

            foreach (var atraso in lista_atraso)
            {
                logger.Info("Atraso : [ codmensagem : " + atraso.codmensagem + " / horario: " + atraso.horario + " ]");
                var solicitacao = db.Solicitacao.Where(s => s.CodMensagem == atraso.codmensagem).OrderByDescending(i => i.DataOrigem).FirstOrDefault();
                if (solicitacao != null)
                {
                    solicitacao.ATRASO = atraso.horario;                    
                }
            }

            db.SaveChanges();
        }

        public List<OrdemServicoIntegration> ListarOrdensPendentes(String phone, long lastpackage, long newpackage, String securitykey)
        {
            var listaRetorno = new List<OrdemServicoIntegration>();
            ParserHelper parser = new ParserHelper();
            
            DateTime data_atual = new DateTime(DateTime.Now.Year, DateTime.Now.Month, DateTime.Now.Day, 23, 59, 59);
            DateTime data_corte = data_atual.AddDays(-1);
            
            //Retona as OSs pendentes de sincronização com o device
            var listaOS = db.Solicitacao.Where(s =>
                    s.Celular == phone &&
                    (s.IDStatus == 1001 || s.IDStatus == 1002 || s.IDStatus == 1004) &&
                    (s.LastPackageSent < lastpackage || s.LastPackageSent == null || s.LastPackageSent == 0) &&
                    (s.DataOrigem > data_corte) &&
                    (s.RECUPERAR_WEB == false || !s.RECUPERAR_WEB.HasValue)).ToList();
            
            //Atualiza o lastpackagesent
            foreach (var os in listaOS)
            {
                try
                {
                    List<Produtos> listaProdutos = db.Produtos.Where(p => p.idSolicitacao == os.IDSolicitacao).OrderBy(o => o.indice).ToList();

                    //Convertendo pro objeto usado na integracao com o device
                    OrdemServicoIntegration order = ConvertSolicitacao(os);
                    order.produtos = new List<ProdutoIntegration>();
                    order.produtos = parser.ParseProducts(os.CodMensagem, listaProdutos);

                    //Verifica se o pedido tem algum produto do superbotão
                    order.superbotao = 0;

                    Entregador entregador = db.Entregador.FirstOrDefault(e => e.codigo == phone);

                    if (entregador != null)
                    {
                        order.concorrencia = (entregador.concorrencia.HasValue ? entregador.concorrencia.Value : false);
                        order.is_vip = entregador.isVip.HasValue ? entregador.isVip.Value : false;
                    }

                    listaRetorno.Add(order);
                }
                catch (Exception ex)
                {
                    logger.Error("Repository.ListarOrdensPendentes : *** ERR [ codmensagem : " + os.CodMensagem + " / erro: " + ex.Message + " / " + ex.StackTrace + " ]");
                }

                os.SecurityKeyMobile = securitykey;
                os.LastPackageSent = newpackage;
            }

            //Se houver atualização efetua as alterações na base
            if(listaOS.Count > 0)
                db.SaveChanges();

            return listaRetorno;
        }

        public Boolean MarcarComoRecebidaPeloMobile(String phone, String codmensagem, long newpackage)
        {
            Boolean marked = false;

            //Retona as OSs pendentes de sincronização com o device
            var listaOS = db.Solicitacao.Where(s =>
                    s.Celular == phone &&
                    s.CodMensagem == codmensagem).ToList();

            //Atualiza o lastpackagesent
            foreach (var os in listaOS)
            {
                os.LastPackageSent = newpackage;
            }

            //Se houver atualização efetua as alterações na base
            if (listaOS.Count > 0)
            {
                try
                {
                    db.SaveChanges();
                    marked = true;
                }
                catch (Exception ex)
                {
                    marked = false;
                }
            }
            return marked;
        }

        public List<CancelOSIntegration> ListarOrdensCanceladas(String phone)
        {
            var listaRetorno = new List<CancelOSIntegration>();

            var entregador = db.Entregador.FirstOrDefault(s => s.codigo == phone);

            if (entregador != null)
            {
                //Retona as OSs pendentes de sincronização com o device
                var listaOS = db.Solicitacao.Where(s =>
                        s.Celular == phone &&
                        s.IDStatus == STATUS_INICIO_CANCELAMENTO &&
                        s.SyncTransferredMobile == 0).ToList().Distinct();

                //Atualiza o lastpackagesent
                foreach (var os in listaOS)
                {
                    CancelOSIntegration cancel = new CancelOSIntegration();
                    cancel.codmensagem = os.CodMensagem;
                    //Convertendo pro objeto usado na integracao com o device
                    listaRetorno.Add(cancel);

                    //Atualiza como já sincronizado
                    os.SyncTransferredMobile = 1;
                }

                db.SaveChanges();
            }

            return listaRetorno;
        }

        public List<EntregadorIntegration> ListarEntregadores(String phone)
        {
            var listaRetorno = new List<EntregadorIntegration>();

            var entregador = db.Entregador.FirstOrDefault(s =>
                    s.codigo == phone);

            if (entregador != null)
            {
                //Retona as entregadores da revenda
                var lista = db.Entregador.Where(s =>
                        s.IDRevenda.Value == entregador.IDRevenda.Value).ToList();

                //Atualiza o lastpackagesent
                foreach (var e in lista)
                {
                    //Convertendo pro objeto usado na integracao com o device
                    listaRetorno.Add(ConvertEntregador(e));
                }
            }

            return listaRetorno;
        }

        public List<ConcorrenteIntegration> ListarConcorrentes()
        {
            var listaRetorno = new List<ConcorrenteIntegration>();

            //Retona as concorrentes
            var lista = db.Concorrentes.ToList();

            foreach (var c in lista)
            {
                //Convertendo pro objeto usado na integracao com o device
                listaRetorno.Add(ConvertConcorrente(c));
            }

            return listaRetorno;
        }

        public SlaIntegration GetSla()
        {
            SlaIntegration si = null;
            //Retona o SLA
            var sla = db.Sla_Atendimento.FirstOrDefault();
            if (sla != null)
            {
                si = new SlaIntegration();
                si.T1 = sla.T1;
                si.T2 = sla.T2;
            }

            return si;
        }

        public void AtualizarFotoContrato(String codmensagem)
        {
            //Retona as OSs pendentes de sincronização com o device
            var listaOS = db.Solicitacao.Where(s =>
                    s.CodMensagem == codmensagem).OrderByDescending(i => i.IDSolicitacao).ToList();

            //Atualiza o lastpackagesent
            foreach (var os in listaOS)
            {
                os.URL_FOTO_CONTRATO = codmensagem;
            }

            db.SaveChanges();
        }

        private OrdemServicoIntegration ConvertSolicitacao(Solicitacao solicitacao)
        {
            OrdemServicoIntegration os = new OrdemServicoIntegration();

            os.codmensagem = solicitacao.CodMensagem;
            os.mensagem = solicitacao.Mensagem;
            os.data = solicitacao.DataOrigem.HasValue ? NetdateToJavadate(solicitacao.DataOrigem.Value) : NetdateToJavadate(new DateTime());
            os.coletar_ofr = solicitacao.coletarOFR.HasValue ? solicitacao.coletarOFR.Value : false;
            os.editar_produtos = solicitacao.editarQtdes.HasValue ? solicitacao.editarQtdes.Value : false;
            os.origem = solicitacao.ORIGIN.HasValue ? solicitacao.ORIGIN.Value : 1;

            return os;
        }

        private EntregadorIntegration ConvertEntregador(Entregador entregador)
        {
            EntregadorIntegration entregadorIntegration = new EntregadorIntegration();
            entregadorIntegration.nome = entregador.nome;
            entregadorIntegration.telefone = entregador.codigo;

            return entregadorIntegration;
        }

        private ConcorrenteIntegration ConvertConcorrente(Concorrentes concorrente)
        {
            ConcorrenteIntegration concorrenteIntegration = new ConcorrenteIntegration();
            concorrenteIntegration.codconcorrente = concorrente.id;
            concorrenteIntegration.descricao = concorrente.descricao;

            return concorrenteIntegration;
        }

        private List<ProdutoIntegration> ConvertProdutos(List<Produtos> produtos, string codmensagem)
        {
            List<ProdutoIntegration> lista = new List<ProdutoIntegration>();

            foreach (var item in produtos)
            {
                ProdutoIntegration prod = new ProdutoIntegration();
                prod.codproduto = item.codProduto;
                prod.codmensagem = codmensagem;
                prod.quantidade = item.qtde;

                lista.Add(prod);
            }

            return lista;
        }

        private DateTime JavadateToNetdate(long javadate_inmillis)
        {
            DateTime origin = new DateTime(1970, 1, 1, 0, 0, 0, 0);

            DateTime today = DateTime.Now;
            if(today.IsDaylightSavingTime())
                return origin.AddMilliseconds(javadate_inmillis).AddHours(-2); // For -2 GMT Brazil format (Daylight Saving Time)
            else
                return origin.AddMilliseconds(javadate_inmillis).AddHours(-3); // For -3 GMT Brazil format
        }

        private long NetdateToJavadate(DateTime netdate)
        {
            DateTime baseDate = new DateTime(1970, 1, 1, 0, 0, 0, 0);
            long timeInMillis = (netdate.ToUniversalTime().Ticks - baseDate.Ticks) / 10000; // For -3 GMT Brazil format
            return timeInMillis;
        }

        //Obtem os comandos pendente para o celular
        public List<CommandIntegration> ObterComandosPendentes(String phone)
        {
            var commands = db.MobileCommands.Where(c => c.DeviceNumber == phone && c.IsPending == true).ToList();

            List<CommandIntegration> commandList = new List<CommandIntegration>();
            foreach (var command in commands)
            {
                commandList.Add(new CommandIntegration() { commandType = command.CommandType, message = command.Message, codmensagem = command.CodMensagem });
                command.IsPending = false;
                command.ExecutedAt = DateTime.Now;
            }

            if (commandList.Count > 0)
                db.SaveChanges();

            return commandList;
        }

        public void AtualizarUltimoSinc(string phone)
        {
            Provision provision = db.Provision.FirstOrDefault(p => p.celular == phone);

            if (provision != null)
            {
                provision.lastSync = DateTime.Now;
                db.SaveChanges();
            }
        }

        public void AtualizarVersaoAPP(string phone, string version)
        {
            Provision prov = db.Provision.FirstOrDefault(p => p.celular == phone);
            if (prov != null)
            {
                prov.version = version;
                db.SaveChanges();
            }
        }

        public bool CheckSecurityKey(string phone, String securitykey)
        {
            bool isValid = false;

            logger.Debug("Repository.CheckSecurityKey [ phone : " + phone + " / securitykey: " + securitykey + " ]");
            Provision prov = db.Provision.FirstOrDefault(p => p.celular == phone && p.keyValue == securitykey);
            if (prov != null)
            {
                isValid = true;
            }

            logger.Debug("Repository.CheckSecurityKey [ phone : " + phone + " / isValid? : " + isValid + " ]");
            return isValid;
        }
    }
}