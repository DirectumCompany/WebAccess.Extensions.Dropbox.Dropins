using NpoComputer.WebAccess;
using NpoComputer.WebAccess.API;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Web;
using System.Web.Hosting;
using System.Web.Services;

namespace WebAccess.Extensions.Dropbox.Dropins {
  /// <summary>Сервис для выдачи и загрузки файлов Dropbox</summary>
  /// http://club.directum.ru/post/Code-Snippets-razrabotka-veb-modulja-DIRECTUM-dlja-raboty-s-Dropbox.aspx
  [WebService(Namespace = "http://npo-comp.ru/services/dropins/")]
  [WebServiceBinding(ConformsTo = WsiProfiles.BasicProfile1_1)]
  [System.ComponentModel.ToolboxItem(false)]
  [System.Web.Script.Services.ScriptService()]
  // To allow this Web Service to be called from script, using ASP.NET AJAX, uncomment the following line. 
  // [System.Web.Script.Services.ScriptService]
  public class Service : System.Web.Services.WebService {

    /// <summary>Метод выгрузки файла для возможности загрузки сервисом Dropbox</summary>
    /// <param name="id">ИД документа</param>
    /// <param name="version">версия документа</param>
    /// <returns>ИД для загрузки файла</returns>
    [WebMethod]
    public WebServiceResponse<string> ExportFileForDownload(int id, int version) {
      // Версия документа на выгрузку
      NpoComputer.WebAccess.API.EDocumentVersion docVersion;

      // Если контекст пользователя не задан, не продолжать работу
      if (WAAPIEntry.Context == null) return WebServiceResponse<string>.Fail("Контекст пользователя не найден. Сначала нужно войти в веб-доступ.");
      
      // Получение пути ко временной папке текущего пользователя
      string userDirectory = WAAPIEntry.Context.TempDirectory;
      if (!Path.IsPathRooted(userDirectory)) userDirectory = HostingEnvironment.MapPath(userDirectory);

      try {
        var document = WAAPIEntry.Context.EDocuments.GetDocumentByID(id);
        if (document == null) throw new Exception(string.Format("Документ (ИД={0}) не найден или у Вас нет прав на него.", id));

        // Получение версии документа
        if (version > 0) docVersion = document.get_Versions(version);
        else docVersion = document.LastActualVersion;

        if (docVersion == null) throw new Exception(string.Format("Документ (ИД={0}) не содержит версии с номером {1}.", id, version));

        // Формирование имени для выгрузки
        var fileName = String.Format("{0}_v{2}.{1}", id, document.Editor.Extension, docVersion.Number);
        var filePath = Path.Combine(userDirectory, fileName);

        docVersion.ExportToFile(NpoComputer.WebAccess.API.EDocument.ExportMode.Read, filePath);
        return WebServiceResponse<string>.OK(KeyManagement.AddFilePath(filePath));
      } catch (Exception ex) {
        Log.LogException(ex);
        return WebServiceResponse<string>.Fail(ex.Message);
      }
    }

        
    [WebMethod]
    public WebServiceResponse ImportFileFromDropBox(int id, string fileURL, string fileName){
      // Если контекст пользователя не задан, не продолжать работу
      if (WAAPIEntry.Context == null) return WebServiceResponse.Fail("Контекст пользователя не найден. Сначала нужно войти в веб-доступ.");
      
      // Получение пути ко временной папке текущего пользователя
      string userDirectory = WAAPIEntry.Context.TempDirectory;
      if (!Path.IsPathRooted(userDirectory)) userDirectory = HostingEnvironment.MapPath(userDirectory);

      try {
        // Получение документа по ИД
        var document = WAAPIEntry.Context.EDocuments.GetDocumentByID(id);
        if (document == null) throw new Exception(string.Format("Документ (ИД={0}) не найден или у Вас нет прав на него.", id));
        var filePath = Path.Combine(userDirectory, Path.GetTempFileName());
              
        // Откатывам поток до выполнения от имени пула приложений (при включенной имперсонации)
        using (new NpoComputer.Foundation.Security.Deimpersonation()) {          
          var client = new System.Net.WebClient();

          var Settings = NpoComputer.WebAccess.API.Application.Settings;
          bool isProxyEnabled;
          bool.TryParse(Settings.get_CustomParam("DROP_PROXY_ENABLED"), out isProxyEnabled);
          if (isProxyEnabled) { 
            //При необходимости используем прокси-сервер
            WebProxy p = new WebProxy(Settings.get_CustomParam("DROP_PROXY_SERVER"), true);
            p.Credentials = new NetworkCredential(Settings.get_CustomParam("DROP_PROXY_USER"), Settings.get_CustomParam("DROP_PROXY_PASSWORD"));
            WebRequest.DefaultWebProxy = p;
            client.Proxy = p;
          }
          client.DownloadFile(fileURL, filePath);

          var editor = WAAPIEntry.Context.EDocuments.get_DocumentEditorByExt(Path.GetExtension(fileName).TrimStart('.').ToUpper()).Code;
          document.Import(-1, false, "Импортировано из Dropbox",filePath,editor, true);
        }

        return WebServiceResponse.OK();
      } catch (Exception ex) {
        Log.LogException(ex);
        return WebServiceResponse.Fail(ex.Message);
      }
    }

  }

}
