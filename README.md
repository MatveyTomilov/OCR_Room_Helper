# Abyss Overlay (Windows)

Мини‑оверлей для EVE Abyss: кнопка поверх экрана + окно помощи с приоритетами целей и заметками по комнате.

## Что нужно
- Windows 10/11
- Интернет для первого запуска `publish.ps1` (установка Tesseract через winget)

## Быстрый запуск (dev)
```powershell
cd AbyssOverlay
 dotnet run
```

## Установка и запуск (релиз)
### 1) Собрать self‑contained exe + установить Tesseract автоматически
```powershell
cd AbyssOverlay
.\publish.ps1 -Runtime win-x64
```
Результат будет в `AbyssOverlay\publish\win-x64\`.

Скрипт автоматически:
- собирает self‑contained single‑file exe
- ставит Tesseract через **winget** (user scope, без админ‑прав)
- копирует Tesseract в `publish\win-x64\tesseract\`
- скачивает `rus` и `eng` traineddata

### 2) Запуск
Запусти:
```
AbyssOverlay\publish\win-x64\AbyssOverlay.exe
```

Приложение автоматически ищет Tesseract в:
- `./tesseract/tesseract.exe`
- `./tools/tesseract/tesseract.exe`
- PATH
- стандартные пути Program Files

## Управление
- Кнопка `Abyss` — оверлей, всегда поверх окон.
- Клик по кнопке открывает/скрывает настройки.

### В настройках
- `Select region` — выбрать область экрана для OCR.
- `Analyze (OCR)` — распознать текст и обновить окно помощи.
- `Reload Excel` — перечитать Excel.
- `Quit` — выход.

### Окно помощи (оверлей)
- `Show help overlay` — показать/скрыть окно помощи.
- `Lock overlay (click-through)` — закрепить и сделать окно прозрачным для кликов.
- `Opacity` — прозрачность окна.
- `Save overlay position` — сохранить позицию/размер окна.

### Горячие клавиши
- `Ctrl+Alt+R` — выбор области
- `Ctrl+Alt+S` — анализ OCR
- `Ctrl+Alt+Q` — выход

## Примечания
- Файл `config.json` создается рядом с exe и хранит настройки оверлея и области.
- Файл `T6_Exotic.xlsx` (или `12312.xlsx`) должен лежать рядом с exe или в корне проекта.
