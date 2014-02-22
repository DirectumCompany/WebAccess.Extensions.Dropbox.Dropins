(function () {
  //Ключ приложения Dropbox
  var appKey = '0qcflj2scrg6p6o';

  // Нэймспейс клиентской логики расширения
  var DropIns = {};
  // Регистрация скрипта DROPBOX для работы импорта/экспорта
  DropIns.RegisterDropBoxScript = function () {
    var head = document.getElementsByTagName('head')[0];
    var script = document.createElement('script');
    script.type = 'text/javascript';
    script.src = 'https://www.dropbox.com/static/api/2/dropins.js';
    script.id = 'dropboxjs';
    script.setAttribute('data-app-key', appKey);
    head.appendChild(script);
  };

  // Действие экспорта файла
  DropIns.ExportFile = function () {
    // Формирование адреса загрузки файла и создание объекта Dropbox
    var sendFileKey = function (key) {      
      Dropbox.save(window.location.origin + '/ExportFile.ashx?key=' + key, WA.CR.ClientName + '.' + WA.CR.Editor.Extension)
    };
    
    if (WA.CR.Versions.length > 1) {
      // Создание новой формы
      var template = new WA.components.forms.FormBuilder("VERSIONS_DLG");
      var versionsSelect = {};
      $.each(WA.CR.Versions, function (i, item) {
        versionsSelect[item.Number] = item.Note;
      });
      // Добавление контрола типа: Select
      template.addSelect("VERSION", versionsSelect);
      // Создание конфигурации диалога
      var dialog = {};
      dialog.height = 170;
      dialog.width = 400;
      dialog.title = "Выберите версию для экспорта";
      dialog.okText = L("OK");
      dialog.cancelText = L("CANCEL");
      dialog.text = template.render();
      dialog.ok = function () {
        var version = template.getValue("VERSION");
        WA.services.call("Dropbox.asmx/ExportFileForDownload", { id: WA.CR.ID, version: version }).done(sendFileKey);
      }
      // Отображение диалога с выбором версий документа
      ConfirmDialog(dialog);
    } else {
      // Вызов сервиса с передачей ИД и версии документа
      WA.services.call("Dropbox.asmx/ExportFileForDownload", { id: WA.CR.ID, version: WA.CR.Versions[0].Number }).done(sendFileKey);
    }       
  };

  DropIns.ImportFile = function () {
    var downloadFile = function (id, url, name) {
      WA.services.call("Dropbox.asmx/ImportFileFromDropBox", { id: id, fileURL: url, fileName: name }).done(function () {
        var toast = new WA.CMP.NTF.Toast()
        toast.showMessage("Документ импортирован успешно.");
      });
    };

    var options = {
      success: function (files) {
        downloadFile(WA.CR.ID, files[0].link, files[0].name);
      },
      linkType: "direct"
    }
    if (!!WA.CR.Editor.Extension) options.extensions = ['.' + WA.CR.Editor.Extension]
    Dropbox.choose(options);
  };
  
  window.DropIns = DropIns;
})()

// Регистрация действий по окончанию загрузки ОМ
WebAccess.ready(function () {
  // Проверка на нахождение в карточке документа
  if (!!WA.CR.CardTypeID) {
    // Создание новой группы в тулбаре и добавление кнопки
    //var group = WA.CR.toolBar.groups.create("DIRDROP");
    // Или использование имеющейся
    var group = WA.CR.toolBar.groups["TOOLBAR_SEND_GROUP"];
    var button = group.createButton("DROPEXPORT");
    button.setLabel(L("DROPINS_EXPORT"))
          .setTooltip(L("DROPINS_EXPORT"))
          .setIcon("/App_Sprites/SidebarAndExtensions/drop_export.png");
    button.click(DropIns.ExportFile);

    var button = group.createButton("DROPIMPORT");
    button.setLabel(L("DROPINS_IMPORT"))
          .setTooltip(L("DROPINS_IMPORT"))
          .setIcon("/App_Sprites/SidebarAndExtensions/drop_export.png");
    button.click(DropIns.ImportFile);
    DropIns.RegisterDropBoxScript();
  }
});
