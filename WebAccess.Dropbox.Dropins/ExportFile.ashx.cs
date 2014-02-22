using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Web;
using NpoComputer.WebAccess;

namespace WebAccess.Extensions.Dropbox.Dropins {
  /// <summary>
  /// Summary description for ExportFile
  /// </summary>
  public class ExportFile : IHttpHandler {

    public void ProcessRequest(HttpContext context) {
      Guid keyGuid;
      string filePath = string.Empty;
      var key = context.Request["key"];

      if (!string.IsNullOrEmpty(key)) {
        keyGuid = new Guid(key);
        filePath = KeyManagement.GetFilePath(keyGuid);
      }

      if (File.Exists(filePath)) {
        try {
          context.Response.ClearContent();
          context.Response.ContentType = "text/octet-stream";
          context.Response.AddHeader("Content-Disposition", "attachment; filename=" + Path.GetFileName(filePath));
          context.Response.TransmitFile(filePath);
        } catch (Exception ex) {
          Log.LogException(ex);
          if (File.Exists(filePath)) File.Delete(filePath);
          context.Response.StatusCode = 404;
        }
      } else {
        context.Response.StatusCode = 404;
      }
    }

    public bool IsReusable {
      get {
        return false;
      }
    }
  }
}