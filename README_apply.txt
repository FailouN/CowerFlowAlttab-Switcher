
Файлы для замены:
- PreviewGenerator.cs  -> гибрид PrintWindow(PW_RENDERFULLCONTENT) + BitBlt + DWM frame bounds
- WindowEnumerator.cs  -> заполняет WindowInfo.Preview через PreviewGenerator
- WindowInfo.cs        -> хранит Preview

Никаких изменений в XAML не требуется.
Если в отдельных окнах всё ещё пусто:
- проверь, что приложение запускается с достаточными правами (UAC может блокировать захват чужих окон);
- для UWP/Edge/игр с эксклюзивным рендерингом DWM-кадры недоступны — остаётся fallback BitBlt;
- можно отключить аппаратное ускорение у отдельных приложений для стабильного PrintWindow.
