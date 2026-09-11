[CmdletBinding()]
param([string]$KeyPattern = '*')
$ErrorActionPreference = 'Stop'
$root = Split-Path -Parent $PSScriptRoot
$outputRoot = Join-Path $root 'native\IconFlow.WinUI\Strings'
$locales = @('zh-CN','zh-TW','en-US','es-ES','fr-FR','de-DE','pt-BR','ja-JP','ko-KR','ru-RU','ar-SA')

function T([string[]]$values) {
    if ($values.Count -ne $locales.Count) { throw "翻译列数量错误：$($values[0])" }
    return $values
}

$terms = @{
    IconManager = T @('图标管理器','圖示管理器','Icon manager','Administrador de iconos','Gestionnaire des icônes','Symbolverwaltung','Gerenciador de ícones','アイコン管理','아이콘 관리자','Менеджер значков','مدير الأيقونات')
    QuickChange = T @('快速更换','快速更換','Quick change','Cambio rápido','Changement rapide','Schnell ändern','Troca rápida','クイック変更','빠른 변경','Быстрая замена','تغيير سريع')
    Library = T @('图标库','圖示庫','Icon library','Biblioteca de iconos','Bibliothèque des icônes','Symbolbibliothek','Biblioteca de ícones','アイコンライブラリ','아이콘 라이브러리','Библиотека значков','مكتبة الأيقونات')
    History = T @('历史恢复','歷史還原','History & restore','Historial y restauración','Historique et restauration','Verlauf & Wiederherstellung','Histórico e restauração','履歴と復元','기록 및 복원','История и восстановление','السجل والاستعادة')
    Settings = T @('设置','設定','Settings','Configuración','Paramètres','Einstellungen','Configurações','設定','설정','Настройки','الإعدادات')
    SelectAndClick = T @('选择对象，再点一个图标','選擇物件，再點一個圖示','Select an item, then choose an icon','Selecciona un elemento y elige un icono','Sélectionnez un élément puis une icône','Element wählen, dann Symbol auswählen','Selecione um item e escolha um ícone','対象を選び、アイコンを選択','항목을 선택한 다음 아이콘 선택','Выберите объект, затем значок','حدد عنصراً ثم اختر أيقونة')
    Undo = T @('撤销','復原','Undo','Deshacer','Annuler','Rückgängig','Desfazer','元に戻す','실행 취소','Отменить','تراجع')
    DropFolderShortcut = T @('拖入文件夹或快捷方式','拖入資料夾或捷徑','Drop a folder or shortcut','Arrastra una carpeta o acceso directo','Déposez un dossier ou un raccourci','Ordner oder Verknüpfung ablegen','Solte uma pasta ou atalho','フォルダーまたはショートカットをドロップ','폴더 또는 바로 가기 놓기','Перетащите папку или ярлык','أفلت مجلداً أو اختصاراً')
    DropImage = T @('也可以拖入图片，自动生成 ICO','也可以拖入圖片，自動產生 ICO','You can also drop an image to create an ICO','También puedes soltar una imagen para crear un ICO','Vous pouvez aussi déposer une image pour créer un ICO','Bild ablegen und automatisch ICO erstellen','Você também pode soltar uma imagem para criar um ICO','画像をドロップして ICO を自動作成','이미지를 놓아 ICO 자동 생성','Можно перетащить изображение для создания ICO','يمكنك أيضاً إفلات صورة لإنشاء ICO')
    ChooseFolder = T @('选择文件夹','選擇資料夾','Choose folder','Elegir carpeta','Choisir un dossier','Ordner auswählen','Escolher pasta','フォルダーを選択','폴더 선택','Выбрать папку','اختيار مجلد')
    ChooseShortcut = T @('选择快捷方式','選擇捷徑','Choose shortcut','Elegir acceso directo','Choisir un raccourci','Verknüpfung auswählen','Escolher atalho','ショートカットを選択','바로 가기 선택','Выбрать ярлык','اختيار اختصار')
    NoTarget = T @('尚未选择对象','尚未選擇物件','No item selected','Ningún elemento seleccionado','Aucun élément sélectionné','Kein Element ausgewählt','Nenhum item selecionado','対象が選択されていません','선택한 항목 없음','Объект не выбран','لم يتم تحديد عنصر')
    RecentAction = T @('最近操作','最近操作','Recent activity','Actividad reciente','Activité récente','Letzte Aktivität','Atividade recente','最近の操作','최근 작업','Недавние действия','النشاط الأخير')
    NoHistory = T @('还没有修改记录','還沒有修改記錄','No changes yet','Aún no hay cambios','Aucune modification','Noch keine Änderungen','Nenhuma alteração ainda','変更履歴はありません','변경 기록 없음','Изменений пока нет','لا توجد تغييرات بعد')
    AutoBackup = T @('修改前会自动备份','修改前會自動備份','A backup is created before every change','Se crea una copia antes de cada cambio','Une sauvegarde est créée avant chaque modification','Vor jeder Änderung wird gesichert','Um backup é criado antes de cada alteração','変更前に自動バックアップ','변경 전 자동 백업','Перед каждым изменением создаётся копия','يتم إنشاء نسخة احتياطية قبل كل تغيير')
    UndoThis = T @('撤销这次修改','復原這次修改','Undo this change','Deshacer este cambio','Annuler cette modification','Diese Änderung rückgängig machen','Desfazer esta alteração','この変更を元に戻す','이 변경 실행 취소','Отменить это изменение','التراجع عن هذا التغيير')
    RecentUsed = T @('最近使用','最近使用','Recently used','Usados recientemente','Récemment utilisés','Zuletzt verwendet','Usados recentemente','最近使用','최근 사용','Недавно использованные','المستخدمة مؤخراً')
    ImportIcon = T @('导入图标','匯入圖示','Import icons','Importar iconos','Importer des icônes','Symbole importieren','Importar ícones','アイコンをインポート','아이콘 가져오기','Импорт значков','استيراد الأيقونات')
    Import = T @('导入','匯入','Import','Importar','Importer','Importieren','Importar','インポート','가져오기','Импорт','استيراد')
    OrganizeHint = T @('拖动图标即可整理到文件夹','拖動圖示即可整理到資料夾','Drag icons into folders to organize them','Arrastra iconos a carpetas para organizarlos','Faites glisser les icônes dans des dossiers','Symbole zum Sortieren in Ordner ziehen','Arraste ícones para pastas para organizar','アイコンをフォルダーへドラッグして整理','아이콘을 폴더로 끌어 정리','Перетаскивайте значки в папки','اسحب الأيقونات إلى المجلدات لتنظيمها')
    Search = T @('搜索名称、标签或来源','搜尋名稱、標籤或來源','Search name, tag, or source','Buscar nombre, etiqueta u origen','Rechercher un nom, une étiquette ou une source','Name, Tag oder Quelle suchen','Pesquisar nome, tag ou origem','名前、タグ、ソースを検索','이름, 태그 또는 출처 검색','Поиск по имени, тегу или источнику','البحث بالاسم أو الوسم أو المصدر')
    NewFolder = T @('新建文件夹','新增資料夾','New folder','Nueva carpeta','Nouveau dossier','Neuer Ordner','Nova pasta','新しいフォルダー','새 폴더','Новая папка','مجلد جديد')
    RenameFolder = T @('重命名文件夹','重新命名資料夾','Rename folder','Cambiar nombre de carpeta','Renommer le dossier','Ordner umbenennen','Renomear pasta','フォルダー名を変更','폴더 이름 바꾸기','Переименовать папку','إعادة تسمية المجلد')
    Rename = T @('重命名','重新命名','Rename','Cambiar nombre','Renommer','Umbenennen','Renomear','名前を変更','이름 바꾸기','Переименовать','إعادة تسمية')
    Edit = T @('编辑','編輯','Edit','Editar','Modifier','Bearbeiten','Editar','編集','편집','Изменить','تحرير')
    MoveFolder = T @('移动到文件夹…','移動到資料夾…','Move to folder…','Mover a carpeta…','Déplacer vers un dossier…','In Ordner verschieben…','Mover para pasta…','フォルダーへ移動…','폴더로 이동…','Переместить в папку…','نقل إلى مجلد…')
    Delete = T @('删除','刪除','Delete','Eliminar','Supprimer','Löschen','Excluir','削除','삭제','Удалить','حذف')
    HistoryHint = T @('同一对象的修改默认折叠；展开可查看图标前后对比','同一物件的修改預設收合；展開可查看圖示前後對比','Changes to the same item are grouped; expand to compare icons','Los cambios del mismo elemento se agrupan; expande para comparar','Les modifications du même élément sont groupées ; développez pour comparer','Änderungen desselben Elements sind gruppiert; zum Vergleichen erweitern','Alterações do mesmo item são agrupadas; expanda para comparar','同じ対象の変更はまとめられます。展開して比較できます','같은 항목의 변경은 그룹화됩니다. 펼쳐서 비교하세요','Изменения одного объекта сгруппированы; разверните для сравнения','يتم تجميع تغييرات العنصر نفسه؛ وسّع للمقارنة')
    OpenLocation = T @('打开位置','開啟位置','Open location','Abrir ubicación','Ouvrir emplacement','Speicherort öffnen','Abrir local','場所を開く','위치 열기','Открыть расположение','فتح الموقع')
    UndoRecent = T @('撤销最近','復原最近','Undo latest','Deshacer último','Annuler la dernière','Letzte rückgängig','Desfazer mais recente','最新を元に戻す','최근 항목 실행 취소','Отменить последнее','التراجع عن الأحدث')
    Before = T @('修改前','修改前','Before','Antes','Avant','Vorher','Antes','変更前','변경 전','До','قبل')
    After = T @('修改后','修改後','After','Después','Après','Nachher','Depois','変更後','변경 후','После','بعد')
    UndoChange = T @('撤销更改','復原變更','Undo change','Deshacer cambio','Annuler la modification','Änderung rückgängig','Desfazer alteração','変更を元に戻す','변경 실행 취소','Отменить изменение','التراجع عن التغيير')
    Appearance = T @('外观','外觀','Appearance','Apariencia','Apparence','Darstellung','Aparência','外観','모양','Внешний вид','المظهر')
    Theme = T @('主题','主題','Theme','Tema','Thème','Design','Tema','テーマ','테마','Тема','السمة')
    FollowSystem = T @('跟随系统','跟隨系統','Use system setting','Usar configuración del sistema','Suivre le système','Systemeinstellung verwenden','Usar configuração do sistema','システム設定を使用','시스템 설정 사용','Как в системе','استخدام إعداد النظام')
    Light = T @('浅色','淺色','Light','Claro','Clair','Hell','Claro','ライト','라이트','Светлая','فاتح')
    Dark = T @('深色','深色','Dark','Oscuro','Sombre','Dunkel','Escuro','ダーク','다크','Тёмная','داكن')
    Language = T @('语言','語言','Language','Idioma','Langue','Sprache','Idioma','言語','언어','Язык','اللغة')
    Integration = T @('系统集成','系統整合','System integration','Integración del sistema','Intégration système','Systemintegration','Integração do sistema','システム統合','시스템 통합','Интеграция с системой','تكامل النظام')
    CompatMenu = T @('资源管理器兼容右键菜单','檔案總管相容右鍵選單','Compatible Explorer context menu','Menú contextual compatible del Explorador','Menu contextuel compatible Explorateur','Kompatibles Explorer-Kontextmenü','Menu de contexto compatível do Explorer','互換エクスプローラーコンテキストメニュー','호환 탐색기 컨텍스트 메뉴','Совместимое контекстное меню Проводника','قائمة مستكشف متوافقة')
    Off = T @('关闭','關閉','Off','Desactivado','Désactivé','Aus','Desativado','オフ','끔','Выкл.','إيقاف')
    On = T @('开启','開啟','On','Activado','Activé','Ein','Ativado','オン','켬','Вкл.','تشغيل')
    CheckingModern = T @('正在检查 Windows 11 新版菜单组件…','正在檢查 Windows 11 新版選單元件…','Checking the Windows 11 modern menu…','Comprobando el menú moderno de Windows 11…','Vérification du menu moderne de Windows 11…','Windows-11-Menü wird geprüft…','Verificando o menu moderno do Windows 11…','Windows 11 の新メニューを確認中…','Windows 11 새 메뉴 확인 중…','Проверка нового меню Windows 11…','جارٍ فحص قائمة Windows 11 الحديثة…')
    InstallModern = T @('安装 / 修复 Win 11 新版菜单','安裝 / 修復 Win 11 新版選單','Install / repair Windows 11 menu','Instalar / reparar menú de Windows 11','Installer / réparer le menu Windows 11','Windows-11-Menü installieren / reparieren','Instalar / reparar menu do Windows 11','Windows 11 メニューをインストール / 修復','Windows 11 메뉴 설치 / 복구','Установить / восстановить меню Windows 11','تثبيت / إصلاح قائمة Windows 11')
    Startup = T @('开机启动','開機啟動','Start at sign-in','Iniciar al entrar','Démarrer à la connexion','Bei Anmeldung starten','Iniciar ao entrar','サインイン時に起動','로그인 시 시작','Запускать при входе','بدء التشغيل عند تسجيل الدخول')
    DefaultOff = T @('默认关闭','預設關閉','Off by default','Desactivado por defecto','Désactivé par défaut','Standardmäßig aus','Desativado por padrão','既定ではオフ','기본적으로 끔','По умолчанию выключено','متوقف افتراضياً')
    PermissionNote = T @('普通功能不需要管理员权限；关闭窗口即退出，不默认驻留后台。','一般功能不需要系統管理員權限；關閉視窗即退出，預設不常駐背景。','Normal features do not require administrator access. Closing the window exits the app.','Las funciones normales no requieren permisos de administrador. Cerrar la ventana sale de la aplicación.','Les fonctions normales ne nécessitent pas de droits administrateur. Fermer la fenêtre quitte application.','Normale Funktionen benötigen keine Administratorrechte. Beim Schließen wird die App beendet.','Recursos normais não exigem administrador. Fechar a janela encerra o aplicativo.','通常機能に管理者権限は不要です。ウィンドウを閉じると終了します。','일반 기능에는 관리자 권한이 필요하지 않습니다. 창을 닫으면 종료됩니다.','Обычные функции не требуют прав администратора. Закрытие окна завершает приложение.','لا تتطلب الميزات العادية صلاحيات المسؤول. يؤدي إغلاق النافذة إلى إنهاء التطبيق.')
    Storage = T @('图标库保存位置','圖示庫儲存位置','Icon library location','Ubicación de la biblioteca','Emplacement de la bibliothèque','Speicherort der Symbolbibliothek','Local da biblioteca de ícones','アイコンライブラリの場所','아이콘 라이브러리 위치','Расположение библиотеки','موقع مكتبة الأيقونات')
    ChangeMigrate = T @('更改并迁移','變更並移轉','Change and migrate','Cambiar y migrar','Modifier et migrer','Ändern und migrieren','Alterar e migrar','変更して移行','변경 및 마이그레이션','Изменить и перенести','تغيير ونقل')
    MigrateNote = T @('确认后会复制并校验全部图标，再切换到新位置；已经应用的图标不会失效。','確認後會複製並驗證全部圖示，再切換到新位置；已套用的圖示不會失效。','Icons are copied and verified before switching; applied icons remain valid.','Los iconos se copian y verifican antes de cambiar; los aplicados siguen funcionando.','Les icônes sont copiées et vérifiées avant le changement ; les icônes appliquées restent valides.','Symbole werden vor dem Wechsel kopiert und geprüft; angewendete Symbole bleiben gültig.','Os ícones são copiados e verificados antes da troca; ícones aplicados continuam válidos.','切り替え前にコピーと検証を行います。適用済みアイコンは失われません。','전환 전에 아이콘을 복사하고 확인합니다. 적용된 아이콘은 유지됩니다.','Перед переключением значки копируются и проверяются; применённые значки сохраняются.','يتم نسخ الأيقونات والتحقق منها قبل التبديل؛ وتظل الأيقونات المطبقة صالحة.')
    Data = T @('数据','資料','Data','Datos','Données','Daten','Dados','データ','데이터','Данные','البيانات')
    LocalMode = T @('WinUI 3 · 单进程 · 无 Chromium · 本地处理','WinUI 3 · 單一處理序 · 無 Chromium · 本機處理','WinUI 3 · Single process · No Chromium · Local only','WinUI 3 · Un proceso · Sin Chromium · Solo local','WinUI 3 · Processus unique · Sans Chromium · Local','WinUI 3 · Ein Prozess · Ohne Chromium · Lokal','WinUI 3 · Processo único · Sem Chromium · Local','WinUI 3 · 単一プロセス · Chromium なし · ローカル','WinUI 3 · 단일 프로세스 · Chromium 없음 · 로컬','WinUI 3 · Один процесс · Без Chromium · Локально','WinUI 3 · عملية واحدة · بدون Chromium · محلي')
    Reading = T @('读取对象…','讀取物件…','Reading item…','Leyendo elemento…','Lecture de élément…','Element wird gelesen…','Lendo item…','対象を読み込み中…','항목 읽는 중…','Чтение объекта…','جارٍ قراءة العنصر…')
    SearchIcon = T @('搜索图标','搜尋圖示','Search icons','Buscar iconos','Rechercher des icônes','Symbole suchen','Pesquisar ícones','アイコンを検索','아이콘 검색','Поиск значков','البحث عن الأيقونات')
    RenameCurrentFolder = T @('重命名当前文件夹','重新命名目前資料夾','Rename current folder','Cambiar nombre de carpeta actual','Renommer le dossier actuel','Aktuellen Ordner umbenennen','Renomear pasta atual','現在のフォルダー名を変更','현재 폴더 이름 바꾸기','Переименовать текущую папку','إعادة تسمية المجلد الحالي')
    EmptyDrop = T @('拖入图片或点击导入','拖入圖片或點擊匯入','Drop an image or click Import','Arrastra una imagen o pulsa Importar','Déposez une image ou cliquez sur Importer','Bild ablegen oder Importieren wählen','Solte uma imagem ou clique em Importar','画像をドロップするかインポート','이미지를 놓거나 가져오기 클릭','Перетащите изображение или нажмите «Импорт»','أفلت صورة أو انقر فوق استيراد')
    AutoApply = T @('导入后会自动选择并应用','匯入後會自動選擇並套用','Imported icons are selected and applied automatically','Los iconos importados se seleccionan y aplican automáticamente','Les icônes importées sont sélectionnées et appliquées automatiquement','Importierte Symbole werden automatisch ausgewählt und angewendet','Ícones importados são selecionados e aplicados automaticamente','インポート後に自動選択して適用','가져온 아이콘 자동 선택 및 적용','Импортированный значок будет выбран и применён автоматически','يتم تحديد الأيقونة المستوردة وتطبيقها تلقائياً')
    RestoreDefault = T @('恢复默认','還原預設值','Restore default','Restaurar predeterminado','Restaurer par défaut','Standard wiederherstellen','Restaurar padrão','既定に戻す','기본값 복원','Восстановить по умолчанию','استعادة الافتراضي')
    OpenMain = T @('打开主界面','開啟主介面','Open main window','Abrir ventana principal','Ouvrir la fenêtre principale','Hauptfenster öffnen','Abrir janela principal','メイン画面を開く','메인 창 열기','Открыть главное окно','فتح النافذة الرئيسية')
    CropStyle = T @('裁剪与样式','裁切與樣式','Crop & style','Recorte y estilo','Recadrage et style','Zuschnitt & Stil','Corte e estilo','切り抜きとスタイル','자르기 및 스타일','Кадрирование и стиль','القص والنمط')
    CropRatio = T @('裁剪比例','裁切比例','Crop ratio','Relación de recorte','Format de recadrage','Zuschnittverhältnis','Proporção de corte','切り抜き比率','자르기 비율','Соотношение кадра','نسبة القص')
    OriginalRatio = T @('自由 / 原比例','自由 / 原比例','Free / original','Libre / original','Libre / original','Frei / Original','Livre / original','自由 / 元の比率','자유 / 원본','Свободно / исходное','حر / أصلي')
    Zoom = T @('缩放','縮放','Zoom','Escala','Zoom','Zoom','Zoom','ズーム','확대/축소','Масштаб','تكبير')
    Horizontal = T @('水平位置','水平位置','Horizontal position','Posición horizontal','Position horizontale','Horizontale Position','Posição horizontal','水平位置','가로 위치','Горизонтальное положение','الموضع الأفقي')
    Vertical = T @('垂直位置','垂直位置','Vertical position','Posición vertical','Position verticale','Vertikale Position','Posição vertical','垂直位置','세로 위치','Вертикальное положение','الموضع الرأسي')
    Padding = T @('透明边距','透明邊距','Transparent padding','Margen transparente','Marge transparente','Transparenter Rand','Margem transparente','透明な余白','투명 여백','Прозрачные поля','هامش شفاف')
    Corner = T @('图像圆角','圖像圓角','Image corners','Esquinas de imagen','Coins de image','Bildecken','Cantos da imagem','画像の角丸','이미지 모서리','Скругление изображения','زوايا الصورة')
    RemoveBg = T @('去除背景色','移除背景色','Remove background color','Quitar color de fondo','Supprimer la couleur de fond','Hintergrundfarbe entfernen','Remover cor de fundo','背景色を削除','배경색 제거','Удалить цвет фона','إزالة لون الخلفية')
    KeepBg = T @('保留背景','保留背景','Keep background','Conservar fondo','Conserver le fond','Hintergrund behalten','Manter fundo','背景を保持','배경 유지','Сохранить фон','الاحتفاظ بالخلفية')
    MakeTransparent = T @('转为透明','轉為透明','Make transparent','Hacer transparente','Rendre transparent','Transparent machen','Tornar transparente','透明にする','투명하게','Сделать прозрачным','جعله شفافاً')
    RemoveColor = T @('要去除的颜色','要移除的顏色','Color to remove','Color a quitar','Couleur à supprimer','Zu entfernende Farbe','Cor a remover','削除する色','제거할 색상','Удаляемый цвет','اللون المراد إزالته')
    White = T @('白色','白色','White','Blanco','Blanc','Weiß','Branco','白','흰색','Белый','أبيض')
    Black = T @('黑色','黑色','Black','Negro','Noir','Schwarz','Preto','黒','검정','Чёрный','أسود')
    Green = T @('绿色','綠色','Green','Verde','Vert','Grün','Verde','緑','녹색','Зелёный','أخضر')
    Magenta = T @('洋红色','洋紅色','Magenta','Magenta','Magenta','Magenta','Magenta','マゼンタ','마젠타','Пурпурный','أرجواني')
    Tolerance = T @('颜色容差','顏色容差','Color tolerance','Tolerancia de color','Tolérance de couleur','Farbtoleranz','Tolerância de cor','色の許容範囲','색상 허용 오차','Допуск цвета','تفاوت اللون')
    BottomShape = T @('底层图形','底層圖形','Background shape','Forma de fondo','Forme arrière-plan','Hintergrundform','Forma de fundo','背景図形','배경 도형','Форма фона','شكل الخلفية')
    None = T @('无','無','None','Ninguna','Aucune','Keine','Nenhuma','なし','없음','Нет','بلا')
    Rounded = T @('圆角方形','圓角方形','Rounded square','Cuadrado redondeado','Carré arrondi','Abgerundetes Quadrat','Quadrado arredondado','角丸四角形','둥근 사각형','Скруглённый квадрат','مربع مستدير')
    Squircle = T @('Win 11 圆方形','Win 11 圓方形','Windows 11 squircle','Cuadrado redondeado de Windows 11','Carré arrondi Windows 11','Windows-11-Squircle','Quadrado arredondado do Windows 11','Windows 11 スクワークル','Windows 11 스쿼클','Сквиркл Windows 11','مربع دائري بنمط Windows 11')
    Circle = T @('圆形','圓形','Circle','Círculo','Cercle','Kreis','Círculo','円','원','Круг','دائرة')
    BottomColor = T @('底层颜色','底層顏色','Background color','Color de fondo','Couleur arrière-plan','Hintergrundfarbe','Cor de fundo','背景色','배경색','Цвет фона','لون الخلفية')
    Indigo = T @('靛蓝','靛藍','Indigo','Índigo','Indigo','Indigo','Índigo','インディゴ','인디고','Индиго','نيلي')
    Cyan = T @('青色','青色','Cyan','Cian','Cyan','Cyan','Ciano','シアン','청록','Циан','سماوي')
    Coral = T @('珊瑚红','珊瑚紅','Coral','Coral','Corail','Koralle','Coral','コーラル','코랄','Коралловый','مرجاني')
    DarkGray = T @('深灰','深灰','Dark gray','Gris oscuro','Gris foncé','Dunkelgrau','Cinza escuro','ダークグレー','진회색','Тёмно-серый','رمادي داكن')
    LightGray = T @('浅灰','淺灰','Light gray','Gris claro','Gris clair','Hellgrau','Cinza claro','ライトグレー','연회색','Светло-серый','رمادي فاتح')
    LocalOnly = T @('所有处理均在本地完成','所有處理均在本機完成','Everything is processed locally','Todo se procesa localmente','Tout est traité localement','Alles wird lokal verarbeitet','Tudo é processado localmente','すべてローカルで処理されます','모든 처리는 로컬에서 수행됩니다','Вся обработка выполняется локально','تتم جميع المعالجة محلياً')
    Cancel = T @('取消','取消','Cancel','Cancelar','Annuler','Abbrechen','Cancelar','キャンセル','취소','Отмена','إلغاء')
    SaveIcon = T @('保存图标','儲存圖示','Save icon','Guardar icono','Enregistrer icône','Symbol speichern','Salvar ícone','アイコンを保存','아이콘 저장','Сохранить значок','حفظ الأيقونة')
    LanguageSaved = T @('语言已保存','語言已儲存','Language saved','Idioma guardado','Langue enregistrée','Sprache gespeichert','Idioma salvo','言語を保存しました','언어 저장됨','Язык сохранён','تم حفظ اللغة')
    LanguageRestart = T @('重新打开 IconFlow 后应用新语言。','重新開啟 IconFlow 後套用新語言。','Reopen IconFlow to apply the new language.','Vuelve a abrir IconFlow para aplicar el nuevo idioma.','Rouvrez IconFlow pour appliquer la nouvelle langue.','IconFlow neu öffnen, um die Sprache anzuwenden.','Reabra o IconFlow para aplicar o novo idioma.','IconFlow を開き直すと言語が適用されます。','IconFlow를 다시 열어 새 언어를 적용하세요.','Перезапустите IconFlow для применения языка.','أعد فتح IconFlow لتطبيق اللغة الجديدة.')
    BuiltInSource = T @('IconFlow 内置','IconFlow 內建','Built into IconFlow','Incluido en IconFlow','Intégré à IconFlow','In IconFlow enthalten','Integrado ao IconFlow','IconFlow 内蔵','IconFlow 기본 제공','Встроено в IconFlow','مضمن في IconFlow')
    AllIcons = T @('全部图标','全部圖示','All icons','Todos los iconos','Toutes les icônes','Alle Symbole','Todos os ícones','すべてのアイコン','모든 아이콘','Все значки','كل الأيقونات')
    BuiltInFolder = T @('内置图标 · Fluent 文件夹','內建圖示 · Fluent 資料夾','Built-in icons · Fluent folders','Iconos incluidos · Carpetas Fluent','Icônes intégrées · Dossiers Fluent','Integrierte Symbole · Fluent-Ordner','Ícones integrados · Pastas Fluent','内蔵アイコン · Fluent フォルダー','기본 아이콘 · Fluent 폴더','Встроенные значки · Папки Fluent','أيقونات مضمنة · مجلدات Fluent')
    Literature = T @('文献资料','文獻資料','Literature','Literatura','Documentation','Literatur','Literatura','文献資料','문헌 자료','Литература','المراجع')
    Analysis = T @('数据分析','資料分析','Data analysis','Análisis de datos','Analyse de données','Datenanalyse','Análise de dados','データ分析','데이터 분석','Анализ данных','تحليل البيانات')
    Development = T @('代码开发','程式開發','Code development','Desarrollo de código','Développement','Codeentwicklung','Desenvolvimento','コード開発','코드 개발','Разработка','تطوير البرمجيات')
    Laboratory = T @('实验研究','實驗研究','Laboratory research','Investigación de laboratorio','Recherche en laboratoire','Laborforschung','Pesquisa de laboratório','実験研究','실험 연구','Лабораторные исследования','البحث المخبري')
    Pictures = T @('图片相册','圖片相簿','Pictures','Imágenes','Images','Bilder','Imagens','画像アルバム','사진 앨범','Изображения','الصور')
    Clinical = T @('临床医疗','臨床醫療','Clinical','Clínica','Clinique','Klinik','Clínico','臨床医療','임상 의료','Клиника','الطب السريري')
    Todo = T @('待办事项','待辦事項','To do','Pendiente','À faire','Zu erledigen','A fazer','未処理','할 일','К выполнению','المهام')
    Completed = T @('已完成','已完成','Completed','Completado','Terminé','Erledigt','Concluído','完了','완료','Выполнено','مكتمل')
    Archive = T @('归档资料','封存資料','Archive','Archivo','Archives','Archiv','Arquivo','アーカイブ','보관 자료','Архив','الأرشيف')
    Unused = T @('未使用','未使用','Unused','Sin usar','Non utilisée','Nicht verwendet','Não usado','未使用','사용 안 함','Не использован','غير مستخدم')
    UsedCount = T @('已使用 {0} 次','已使用 {0} 次','Used {0} times','Usado {0} veces','Utilisée {0} fois','{0}-mal verwendet','Usado {0} vezes','{0} 回使用','{0}회 사용','Использовано: {0}','استُخدمت {0} مرة')
    Favorite = T @('收藏','收藏','Favorite','Favorito','Favori','Favorit','Favorito','お気に入り','즐겨찾기','В избранное','مفضلة')
    Unfavorite = T @('取消收藏','取消收藏','Remove favorite','Quitar favorito','Retirer des favoris','Favorit entfernen','Remover favorito','お気に入り解除','즐겨찾기 해제','Удалить из избранного','إزالة من المفضلة')
    ChangeIcon = T @('更换图标','更換圖示','Change icon','Cambiar icono','Changer icône','Symbol ändern','Alterar ícone','アイコンを変更','아이콘 변경','Изменить значок','تغيير الأيقونة')
    Undone = T @('已撤销','已復原','Undone','Deshecho','Annulée','Rückgängig','Desfeito','取り消し済み','실행 취소됨','Отменено','تم التراجع')
    Undoable = T @('可撤销','可復原','Can undo','Se puede deshacer','Annulable','Rückgängig möglich','Pode desfazer','元に戻せます','실행 취소 가능','Можно отменить','يمكن التراجع')
    HistorySummary = T @('{0} 次修改 · 最近 {1}','{0} 次修改 · 最近 {1}','{0} changes · Latest {1}','{0} cambios · Último {1}','{0} modifications · Dernière {1}','{0} Änderungen · Zuletzt {1}','{0} alterações · Última {1}','{0} 件の変更 · 最新 {1}','변경 {0}회 · 최근 {1}','Изменений: {0} · Последнее {1}','{0} تغييرات · الأحدث {1}')
    ModernMenuEnabled = T @('Windows 11 新版一级菜单组件已启用。','Windows 11 新版第一層選單元件已啟用。','The Windows 11 primary context menu is enabled.','El menú contextual principal de Windows 11 está activado.','Le menu contextuel principal de Windows 11 est activé.','Das primäre Windows-11-Kontextmenü ist aktiviert.','O menu de contexto principal do Windows 11 está ativado.','Windows 11 の新しいコンテキストメニューが有効です。','Windows 11 새 컨텍스트 메뉴가 활성화되었습니다.','Новое контекстное меню Windows 11 включено.','تم تمكين قائمة السياق الرئيسية في Windows 11.')
    ModernMenuPortable = T @('当前为便携兼容模式；安装新版菜单组件后可进入 Windows 11 一级菜单。','目前為可攜相容模式；安裝新版選單元件後可進入 Windows 11 第一層選單。','Portable compatibility mode is active. Install the modern menu component to use the Windows 11 primary menu.','El modo portátil compatible está activo. Instala el componente moderno para usar el menú principal de Windows 11.','Le mode portable compatible est actif. Installez le composant moderne pour utiliser le menu principal de Windows 11.','Der portable Kompatibilitätsmodus ist aktiv. Installieren Sie die moderne Menükomponente für das primäre Windows-11-Menü.','O modo portátil compatível está ativo. Instale o componente moderno para usar o menu principal do Windows 11.','ポータブル互換モードです。Windows 11 の新しいメニューを使うにはメニューコンポーネントをインストールしてください。','포터블 호환 모드입니다. Windows 11 기본 메뉴를 사용하려면 새 메뉴 구성 요소를 설치하세요.','Включён переносной режим совместимости. Установите компонент нового меню для основного меню Windows 11.','وضع التوافق المحمول نشط. ثبّت مكوّن القائمة الحديثة لاستخدام قائمة Windows 11 الرئيسية.')
    GeneratingSizes = T @('正在生成 7 种尺寸…','正在產生 7 種尺寸…','Creating 7 icon sizes…','Creando 7 tamaños de icono…','Création en 7 tailles…','7 Symbolgrößen werden erstellt…','Criando 7 tamanhos de ícone…','7 種類のサイズを生成しています…','7개 아이콘 크기 생성 중…','Создание значка в 7 размерах…','جارٍ إنشاء 7 أحجام للأيقونة…')
}

$resources = @{
    'AppTitleBar.Subtitle'='IconManager'; 'NavHome.Content'='QuickChange'; 'NavLibrary.Content'='Library'; 'NavHistory.Content'='History'
    'HomeHeader.Text'='QuickChange'; 'HomeSubtitle.Text'='SelectAndClick'; 'UndoText.Text'='Undo'; 'DropTargetTitle.Text'='DropFolderShortcut'; 'DropTargetSubtitle.Text'='DropImage'
    'ChooseFolder.Content'='ChooseFolder'; 'ChooseShortcut.Content'='ChooseShortcut'; 'NoTarget.Text'='NoTarget'; 'RecentAction.Text'='RecentAction'; 'NoHistory.Text'='NoHistory'; 'AutoBackup.Text'='AutoBackup'; 'UndoThis.Content'='UndoThis'; 'RecentUsed.Text'='RecentUsed'; 'ImportIconText.Text'='ImportIcon'
    'LibraryHeader.Text'='Library'; 'LibrarySubtitle.Text'='OrganizeHint'; 'ImportText.Text'='Import'; 'LibrarySearch.PlaceholderText'='Search'; 'NewFolder.Content'='NewFolder'; 'FolderRename.Text'='RenameFolder'; 'IconRename.Text'='Rename'; 'IconEdit.Text'='Edit'; 'IconMove.Text'='MoveFolder'; 'IconDelete.Text'='Delete'
    'HistoryHeader.Text'='History'; 'HistorySubtitle.Text'='HistoryHint'; 'OpenLocationText.Text'='OpenLocation'; 'UndoRecentText.Text'='UndoRecent'; 'BeforeText.Text'='Before'; 'AfterText.Text'='After'; 'OpenLocationButton.Content'='OpenLocation'; 'UndoChangeButton.Content'='UndoChange'
    'SettingsHeader.Text'='Settings'; 'AppearanceHeader.Text'='Appearance'; 'ThemeCombo.Header'='Theme'; 'ThemeSystem.Content'='FollowSystem'; 'ThemeLight.Content'='Light'; 'ThemeDark.Content'='Dark'; 'LanguageCombo.Header'='Language'
    'IntegrationHeader.Text'='Integration'; 'CompatMenu.Header'='CompatMenu'; 'CompatMenu.OffContent'='Off'; 'CompatMenu.OnContent'='On'; 'ModernStatus.Text'='CheckingModern'; 'InstallModernMenu.Content'='InstallModern'; 'StartToggle.Header'='Startup'; 'StartToggle.OffContent'='DefaultOff'; 'StartToggle.OnContent'='On'; 'PermissionsNote.Text'='PermissionNote'
    'StorageHeader.Text'='Storage'; 'ChangeMigrate.Content'='ChangeMigrate'; 'MigrateNote.Text'='MigrateNote'; 'DataHeader.Text'='Data'; 'LocalMode.Text'='LocalMode'
    'QuickTitleBar.Title'='ChangeIcon'; 'QuickReading.Text'='Reading'; 'QuickUndo.Content'='Undo'; 'QuickSearch.PlaceholderText'='SearchIcon'; 'QuickNewFolder.Text'='NewFolder'; 'QuickRenameFolder.Text'='RenameCurrentFolder'; 'QuickIconRename.Text'='Rename'; 'QuickIconEdit.Text'='Edit'; 'QuickIconMove.Text'='MoveFolder'; 'QuickIconDelete.Text'='Delete'; 'QuickEmptyTitle.Text'='EmptyDrop'; 'QuickEmptySubtitle.Text'='AutoApply'; 'QuickImportIcon.Content'='ImportIcon'; 'QuickRestore.Content'='RestoreDefault'; 'QuickImport.Content'='Import'; 'QuickOpenMain.Content'='OpenMain'
    'EditorTitleBar.Title'='Edit'; 'EditorTitleBar.Subtitle'='CropStyle'; 'EditorAspect.Header'='CropRatio'; 'EditorOriginalRatio.Content'='OriginalRatio'; 'EditorZoom.Text'='Zoom'; 'EditorHorizontal.Text'='Horizontal'; 'EditorVertical.Text'='Vertical'; 'EditorPadding.Text'='Padding'; 'EditorCorner.Text'='Corner'; 'EditorRemoveBackground.Header'='RemoveBg'; 'EditorRemoveBackground.OffContent'='KeepBg'; 'EditorRemoveBackground.OnContent'='MakeTransparent'; 'EditorRemoveColor.Header'='RemoveColor'; 'EditorWhite.Content'='White'; 'EditorBlack.Content'='Black'; 'EditorGreen.Content'='Green'; 'EditorMagenta.Content'='Magenta'; 'EditorTolerance.Text'='Tolerance'; 'EditorShape.Header'='BottomShape'; 'EditorShapeNone.Content'='None'; 'EditorShapeRounded.Content'='Rounded'; 'EditorShapeSquircle.Content'='Squircle'; 'EditorShapeCircle.Content'='Circle'; 'EditorBottomColor.Header'='BottomColor'; 'EditorIndigo.Content'='Indigo'; 'EditorCyan.Content'='Cyan'; 'EditorColorGreen.Content'='Green'; 'EditorCoral.Content'='Coral'; 'EditorDarkGray.Content'='DarkGray'; 'EditorLightGray.Content'='LightGray'; 'EditorLocalOnly.Text'='LocalOnly'; 'EditorCancel.Content'='Cancel'; 'EditorSave.Content'='SaveIcon'
    'LanguageSavedTitle'='LanguageSaved'; 'LanguageRestartMessage'='LanguageRestart'; 'BuiltInSource'='BuiltInSource'; 'AllIcons'='AllIcons'; 'BuiltInFolder'='BuiltInFolder'; 'BuiltIn_literature'='Literature'; 'BuiltIn_data'='Analysis'; 'BuiltIn_code'='Development'; 'BuiltIn_laboratory'='Laboratory'; 'BuiltIn_pictures'='Pictures'; 'BuiltIn_clinical'='Clinical'; 'BuiltIn_todo'='Todo'; 'BuiltIn_completed'='Completed'; 'BuiltIn_archive'='Archive'; 'Unused'='Unused'; 'UsedCount'='UsedCount'; 'Favorite'='Favorite'; 'Unfavorite'='Unfavorite'; 'RestoreDefault'='RestoreDefault'; 'ChangeIcon'='ChangeIcon'; 'Undone'='Undone'; 'Undoable'='Undoable'; 'HistorySummary'='HistorySummary'; 'SettingsTitle'='Settings'; 'NoHistoryValue'='NoHistory'; 'AutoBackupValue'='AutoBackup'; 'Edit'='Edit'; 'ModernMenuEnabled'='ModernMenuEnabled'; 'ModernMenuPortable'='ModernMenuPortable'; 'GeneratingSizes'='GeneratingSizes'
}

Add-Type -AssemblyName System.Windows.Forms
foreach ($locale in $locales) {
    $index = [Array]::IndexOf($locales, $locale)
    $directory = Join-Path $outputRoot $locale
    New-Item -ItemType Directory -Path $directory -Force | Out-Null
    $path = Join-Path $directory 'Resources.resw'
    $writer = [System.Resources.ResXResourceWriter]::new($path)
    try {
        foreach ($entry in $resources.GetEnumerator() | Where-Object Key -Like $KeyPattern | Sort-Object Key) {
            $writer.AddResource($entry.Key, $terms[$entry.Value][$index])
        }
    }
    finally { $writer.Dispose() }
    Write-Host "PASS: $locale -> $path"
}
