using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using SGBGestor_SERVICE.Models;
using System.Web.Script.Serialization;
using System.Runtime.Serialization.Json;
using System.IO;
using System.Text;
using System.Json;
using Microsoft.CSharp.RuntimeBinder;

namespace SGBGestor_SERVICE.Controllers
{
    /// <summary>
    /// Classe reponsavel pela integração do mobile com o servidor Dispara
    /// </summary>
    public class MobileController : Controller
    {
        /// <summary>
        /// The logger
        /// </summary>
        private log4net.ILog logger = log4net.LogManager.GetLogger("LogInFile");

        /// <summary>
        /// Retorna as ordens de serviço/pedidos pendentes de sincronização com o mobile
        /// </summary>
        /// <param name="phone">telefone/identificação do mobile</param>
        /// <param name="lastpackage">ultimo pacote</param>
        /// <param name="newpackage">novo pacote</param>
        /// <returns></returns>
        [HttpGet]
        public JsonResult GetOS(String phone, String lastpackage, String newpackage, String version, String securitykey)
        {
            logger.Info("GetOS : phone  = " + phone + " | lastpackage : " + lastpackage + " | " + newpackage + " | " + version + " | " + securitykey);
            ListOS listOS = new ListOS();

            try
            {
                Repository repo = new Repository();

                /*if (double.Parse(version.Replace(".",",")) > 3.4)
                {
                    if (repo.CheckSecurityKey(phone, securitykey))
                    {
                        repo.AtualizarVersaoAPP(phone, version);
                        List<OrdemServicoIntegration> listaOs = repo.ListarOrdensPendentes(phone, long.Parse(lastpackage), long.Parse(newpackage), securitykey);
                        listOS.listOS.AddRange(listaOs);
                    }
                } else {*/
                    repo.AtualizarVersaoAPP(phone, version);
                    List<OrdemServicoIntegration> listaOs = repo.ListarOrdensPendentes(phone, long.Parse(lastpackage), long.Parse(newpackage), securitykey);
                    listOS.listOS.AddRange(listaOs);
                //}
            }
            catch (Exception ex)
            {
                logger.Error("GetOS ERROR : " + ex.Message + " [ " + ex.StackTrace + "]");
            }   

            return Json(listOS, JsonRequestBehavior.AllowGet);
        }

        /// <summary>
        /// Recebe os status enviados pelo mobile no formato JSON.
        /// EXEMPLO:
        /// {"statusList": [
        /// {
        /// "status": "2001",
        /// "codmensagem": "1233454565747",
        /// "data": "2012-07-19 08:55:90",
        /// "transferir_para": "",
        /// "concorrente": "",
        /// "ofr": "",
        /// "qrcode": "",
        /// "resultado_teste": "",
        /// "periodicidade": "",
        /// "nr_termo": ""
        /// },
        /// {
        /// "status": "2001",
        /// "codmensagem": "1233454565747",
        /// "data": "2012-07-19 08:55:90"
        /// "transferir_para": "",
        /// "concorrente": "",
        /// "ofr": "",
        /// "qrcode": "",
        /// "resultado_teste": "",
        /// "periodicidade": "",
        /// "nr_termo": ""
        /// }
        /// ]
        /// }
        /// </summary>
        /// <returns>
        /// retorna 0 pra fracasso ou 1 para sucesso
        /// </returns>
        [HttpPost]
        public JsonResult ReceiveStatus()
        {
            logger.Info("ReceiveStatus");

            Repository repo = new Repository();
            try
            {    
                var request = this.HttpContext.Request;
                // Get the JSON data that's been posted
                var jsonStringData = new StreamReader(request.InputStream).ReadToEnd();
                
                logger.Info("ReceiveStatus : jsonStringData = " + jsonStringData);

                dynamic lista_status = JsonValue.Parse(jsonStringData);

                foreach (var status_json in lista_status)
                {
                    StatusIntegration status = new StatusIntegration();

                    string str_idstatus = status_json.idstatus;
                    string str_data = status_json.data;
                    string str_qrcode = "";
                    string str_resultado_teste = "";
                    string str_periodicidade = "";
                    string str_assinou_comodato = "";
                    string str_motivo_nao_movimentacao = "";
                    string str_motivo_superbotao = "";
                    string str_confirma_retirada = "";
                    string str_telefone = "";

                    status.codmensagem = status_json.codmensagem;
                    status.idstatus = int.Parse(str_idstatus);
                    status.data = long.Parse(str_data);
                    status.transferir_para = status_json.transferir_para;
                    status.concorrente = status_json.concorrente;
                    status.ofr = status_json.ofr;

                    try {
                        str_qrcode = status_json.qrcode;
                        str_resultado_teste = status_json.resultado_teste;
                        str_periodicidade = status_json.periodo;
                        str_assinou_comodato = status_json.assinou_comodato;
                        str_motivo_nao_movimentacao = status_json.motivo_nao_movimentacao;
                        str_motivo_superbotao = status_json.motivo_superbotao;
                        str_confirma_retirada = status_json.confirma_retirada;
                        str_telefone = status_json.telefone;
                    }
                    catch (RuntimeBinderException) {/*field doesn't exist*/}

                    status.qrcode = str_qrcode;
                    status.resultado_teste = str_resultado_teste;
                    status.periodicidade = str_periodicidade;

                    status.assinou_comodato = str_assinou_comodato;
                    status.motivo_nao_movimentacao = str_motivo_nao_movimentacao;
                    status.motivo_superbotao = str_motivo_superbotao;
                    status.confirma_retirada = str_confirma_retirada;
                    status.telefone = str_telefone;
                    
                    repo.SalvarStatus(status);
                }

                return Json(1, JsonRequestBehavior.AllowGet);
            } catch(Exception ex) {
                logger.Error("ReceiveStatus ERROR : " + ex.Message + " [ " + ex.StackTrace + "]");
                return Json(0, JsonRequestBehavior.AllowGet);
            }            
        }

        /// <summary>
        /// Recebe os produtos de uma ordem de serviço/pedido.
        /// As quantidades podem ter sido alteradas no mobile.
        /// </summary>
        /// <returns>
        /// retona 0 para fracasso e 1 para sucesso
        /// </returns>
        [HttpPost]
        public JsonResult ReceiveProdutos()
        {
            logger.Info("ReceiveProdutos");

            Repository repo = new Repository();
            try
            {
                var request = this.HttpContext.Request;
                // Get the JSON data that's been posted
                var jsonStringData = new StreamReader(request.InputStream).ReadToEnd();

                logger.Info("ReceiveProdutos : jsonStringData = " + jsonStringData);

                dynamic lista = JsonValue.Parse(jsonStringData);

                foreach (var json_item in lista)
                {
                    ProdutoIntegration produto = new ProdutoIntegration();

                    produto.codmensagem = json_item.codmensagem;
                    produto.codproduto = json_item.codproduto;
                    produto.quantidade = json_item.quantidade;

                    repo.AtualizarProduto(produto);
                }

                return Json(1, JsonRequestBehavior.AllowGet);
            }
            catch (Exception ex)
            {
                logger.Error("ReceiveProdutos ERROR : " + ex.Message + " [ " + ex.InnerException.StackTrace + "]");
                return Json(0, JsonRequestBehavior.AllowGet);
            }
        }

        /// <summary>
        /// Retorna as ordens de serviço/pedido canceladas para fazer o cancelamento no mobile
        /// </summary>
        /// <param name="phone">telefone/identicação do mobile</param>
        /// <returns></returns>
        [HttpGet]
        public JsonResult GetCanceledOS(String phone)
        {
            logger.Info("GetCanceledOS : " + phone);
            ListOSCancelada listOS = new ListOSCancelada();

            try {
                Repository repo = new Repository();
                var lista = repo.ListarOrdensCanceladas(phone);
                listOS.lista.AddRange(lista);
            }
            catch (Exception ex)
            {
                logger.Error("GetCanceledOS ERROR : " + ex.Message + " [ " + ex.InnerException.StackTrace + "]");
            }

            string json = new JavaScriptSerializer().Serialize(Json(listOS).Data);
            logger.Info("GetCanceledOS - retorno : " + json);

            return Json(listOS, JsonRequestBehavior.AllowGet);
        }

        /// <summary>
        /// Retorna a lista de concorrentes da SGB que será sinalizada no final do fluxo e entrega.
        /// </summary>
        /// <param name="phone">telefone/identificação do mobile</param>
        /// <returns></returns>
        [HttpGet]
        public JsonResult GetConcorrentes(String phone)
        {
            logger.Info("GetConcorrentes : " + phone);
            ListConcorrente listConcorrente = new ListConcorrente();

            try {
                Repository repo = new Repository();
                var lista = repo.ListarConcorrentes();                
                listConcorrente.lista.AddRange(lista);
            }
            catch (Exception ex)
            {
                logger.Error("GetConcorrentes ERROR : " + ex.Message + " [ " + ex.InnerException.StackTrace + "]");
            }

            return Json(listConcorrente, JsonRequestBehavior.AllowGet);
        }

        /// <summary>
        /// Retorna a lista de entregadores da mesma revenda do telefone informado no parametro.
        /// </summary>
        /// <param name="phone">telefone/identificação do mobile</param>
        /// <returns></returns>
        [HttpGet]
        public JsonResult GetEntregadores(String phone)
        {
            logger.Info("GetEntregadores : " + phone);

            ListEntregador listEntregador = new ListEntregador();

            try
            {
                Repository repo = new Repository();
                var lista = repo.ListarEntregadores(phone);
                listEntregador.lista.AddRange(lista);
            }
            catch (Exception ex)
            {
                logger.Error("GetEntregadores ERROR : " + ex.Message + " [ " + ex.InnerException.StackTrace + "]");
            }


            return Json(listEntregador, JsonRequestBehavior.AllowGet);
        }

        /// <summary>
        /// Recebe os atrasos informados no mobile.
        /// </summary>
        /// <returns>
        /// retorna 0 para fracasso ou 1 para sucesso
        /// </returns>
        [HttpPost]
        public JsonResult ReceiveAtrasos()
        {
            logger.Info("ReceiveAtrasos");

            Repository repo = new Repository();
            try
            {
                var request = this.HttpContext.Request;
                // Get the JSON data that's been posted
                var jsonStringData = new StreamReader(request.InputStream).ReadToEnd();

                logger.Info("ReceiveAtrasos : jsonStringData = " + jsonStringData);

                dynamic lista = JsonValue.Parse(jsonStringData);

                List<AtrasoIntegration> lista_atraso = new List<AtrasoIntegration>();

                foreach (var json_item in lista.atraso)
                {
                    AtrasoIntegration atraso = new AtrasoIntegration();
                    atraso.codmensagem = json_item.codmensagem;
                    atraso.horario = json_item.horario;
                    lista_atraso.Add(atraso);
                    
                }

                if (lista_atraso.Count > 0)
                {
                    repo.InformarAtrasos(lista_atraso);
                }

                return Json(1, JsonRequestBehavior.AllowGet);
            }
            catch (Exception ex)
            {
                logger.Error("ReceiveProdutos ERROR : " + ex.Message + " [ " + ex.InnerException.StackTrace + "]");
                return Json(0, JsonRequestBehavior.AllowGet);
            }
        }

        /// <summary>
        /// Retorna o SLA informado na web para integração com o mobile.
        /// Caso não tenha os valores padrões serãp retornados (T1 = 15 e T2 = 30)
        /// </summary>
        /// <returns>
        /// JSON: {"T1":10,"T2":20}
        /// </returns>
        [HttpGet]
        public JsonResult GetSla()
        {
            logger.Info("GetSla");

            SlaIntegration sla = new SlaIntegration();
            sla.T1 = 15;
            sla.T1 = 30;
            
            try
            {
                Repository repo = new Repository();
                sla = repo.GetSla();
            }
            catch (Exception ex)
            {
                logger.Error("GetSla ERROR : " + ex.Message + " [ " + ex.InnerException.StackTrace + "]");
            }

            return Json(sla, JsonRequestBehavior.AllowGet);
        }

        /// <summary>
        /// Recebe via fileupload a imagem do termo de responsabilidade
        /// </summary>
        /// <param name="uploadedfile">arquivo de imagem no padrão JPG</param>
        /// <returns>
        /// retona 0 para fracasso e 1 para sucesso
        /// </returns>
        [HttpPost]
        public JsonResult fileUpload(HttpPostedFileBase uploadedfile)
        {
            logger.Info("fileUpload");

            try
            {
                Repository repo = new Repository();

                if (uploadedfile != null && uploadedfile.ContentLength > 0)
                {
                    logger.Info("file name : " + uploadedfile.FileName);
                    uploadedfile.SaveAs(Server.MapPath("~/Content/Uploads/foto/") + Path.GetFileName(uploadedfile.FileName).Replace("sgb_",""));
                    uploadedfile.SaveAs(Server.MapPath("~/Content/Backup/") + Path.GetFileName(uploadedfile.FileName).Replace("sgb_", ""));
                    repo.AtualizarFotoContrato(uploadedfile.FileName.Replace("sgb_", "").Replace(".jpg", ""));
                }

                return Json(1, JsonRequestBehavior.AllowGet);
            }
            catch (Exception ex)
            {
                logger.Error("fileUpload ERROR : " + ex.Message + " [ " + ex.InnerException.StackTrace + "]");
                return Json(0, JsonRequestBehavior.AllowGet);
            }
        }

        /// <summary>
        /// Recebe a confirmacao do mobile com relacao ao salvamento da OS
        /// </summary>
        /// <param name="phone">telefone</param>
        /// <param name="codmensagem">cod. da os</param>
        /// <param name="newpackage">pacote</param>
        /// <returns>
        /// retona 0 para fracasso e 1 para sucesso
        /// </returns>
        [HttpPost]
        public JsonResult OSReceived(String phone, String codmensagem, String newpackage)
        {
            logger.Info("OSReceived : " + phone + " / codmensagem: " + codmensagem + " /  newpackage" + newpackage);
            Boolean marked = false;
            try
            {
                Repository repo = new Repository();
                marked = repo.MarcarComoRecebidaPeloMobile(phone, codmensagem, long.Parse(newpackage));
            }
            catch (Exception ex)
            {
                logger.Error("OSReceived ERROR : " + ex.Message + " [ " + ex.InnerException.StackTrace + "]");
            }

            return Json((marked ? 1 : 0), JsonRequestBehavior.AllowGet);
        }


        /// <summary>
        /// Obter os comandos que serão executados no device
        /// </summary>
        /// <returns>
        /// retona o json com as informações que o device deverá executar/exibir
        /// </returns>
        [HttpGet]
        public JsonResult GetCommands(String phone)
        {
            logger.Info("GetCommands : " + phone);

            List<CommandIntegration> comandos = new List<CommandIntegration>();

            try
            {
                Repository repo = new Repository();
                comandos = repo.ObterComandosPendentes(phone);
                repo.AtualizarUltimoSinc(phone);

            }
            catch (Exception ex)
            {
                logger.Error("GetCommands ERROR : " + ex.Message + " [ " + ex.InnerException.StackTrace + "]");
            }

            return Json(comandos, JsonRequestBehavior.AllowGet);
        }


    }

}
