using System;
using System.Collections.Generic;
using System.Collections.Concurrent;
using System.Linq;
using System.Web;

namespace WebAccess.Extensions.Dropbox.Dropins {
  internal static class KeyManagement {
    /// <summary>Коллекция ключей</summary>
    private static ConcurrentDictionary<Guid, AccessKey> keys = new ConcurrentDictionary<Guid, AccessKey>();
    /// <summary>Время жизни ключа, в минутах</summary>
    private static Int16 keyLifeTime = 5;

    /// <summary></summary>
    /// <param name="filePath">Путь к файлу</param>
    /// <returns></returns>
    static internal string AddFilePath(string filePath) {
      var keyGuid = Guid.NewGuid();
      keys.TryAdd(keyGuid, new AccessKey { filePath = filePath, created = DateTime.Now });
      return keyGuid.ToString();
    }

    /// <summary>Получение пути к файлу по ИДентификатору</summary>
    /// <param name="keyGuid">идентификатор ключа</param>
    /// <returns></returns>
    static internal string GetFilePath(Guid keyGuid) {
      AccessKey ak;
      var filePath = string.Empty;
      if (keys.TryRemove(keyGuid, out ak) && (ak.created.AddMinutes(keyLifeTime) > DateTime.Now)) {
        filePath = ak.filePath;
      }
      return filePath;
    }

  }

  /// <summary>Информация</summary>
  internal class AccessKey {
    /// <summary>Путь к файлу</summary>
    public string filePath;
    /// <summary>Путь к файлу</summary> 
    public DateTime created;
  }

}