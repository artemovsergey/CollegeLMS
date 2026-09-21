// Генерация QR-кодов постера (документация/воспроизводимость).
// Требуется модуль qrcode: npm i qrcode
// Запуск: node generate-qr.js <папка-вывода>
const QRCode = require("qrcode");
const path = require("path");

const outDir = process.argv[2];
if (!outDir) {
  console.error("usage: node generate-qr.js <outDir>");
  process.exit(1);
}

// Ссылки на конкурсные задания в публичном репозитории (raw GitHub).
// Документы с esim.firpo.ru требуют авторизации, поэтому файлы публикуются в репо.
const base = "https://raw.githubusercontent.com/artemovsergey/CollegeLMS/master/docs/professionals/tasks/";

const codes = [
  { file: "qr-programmnye-resheniya.png", url: base + "programmnye-resheniya-04.docx" },
  { file: "qr-veb-tehnologii.png", url: base + "veb-tehnologii-04.docx" },
  { file: "qr-mobilnye-prilozheniya.png", url: base + "mobilnye-prilozheniya-04.docx" },
];

(async () => {
  for (const c of codes) {
    await QRCode.toFile(path.join(outDir, c.file), c.url, {
      errorCorrectionLevel: "M",
      margin: 4,
      width: 1024,
      color: { dark: "#1D1D1B", light: "#FFFFFF" },
    });
    console.log(`${c.file}: ${c.url.length} chars`);
  }
})();
