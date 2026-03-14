# Avalonia Colors

For using in xaml:

```xml
<Setter Property="Background" Value="{DynamicResource SystemAccentColor}"/>
```

## Основные группы ресурсов

### Акцентные цвета (динамические):

- `SystemAccentColor` — основной цвет системы
- `SystemAccentColorLight1, Light2, Light3` — светлые оттенки
- `SystemAccentColorDark1, Dark2, Dark3` — темные оттенки

### Кисти для контролов (реакция на состояния):

- `SystemControlHighlightListLowBrush` — мягкий фон при наведении
- `SystemControlHighlightAccentBrush` — акцентная заливка
- `SystemControlForegroundBaseHighBrush` — основной цвет текста

### Нейтральные тона:

- `SystemControlBackgroundAltHighBrush` — белый/черный в зависимости от темы
- `SystemRegionBrush` — фон окна

## Основные системные имена для текста

### Основные текстовые кисти (Стандартные)

- `SystemControlForegroundBaseHighBrush` — Самый контрастный текст (основной контент).
- `SystemControlForegroundBaseMediumBrush` — Чуть менее контрастный (подзаголовки, вторичный текст).
- `SystemControlForegroundBaseLowBrush` — Слабоконтрастный (текст-подсказки, неактивные элементы).

### Специальные состояния (Акценты)

- `SystemControlHighlightAccentBrush` — Текст основного акцентного цвета (обычно синий).
- `SystemControlErrorTextForegroundBrush` — Красный цвет для ошибок.
- `SystemControlForegroundBaseMediumLowBrush` — Часто используется для текста внутри заблокированных (Disabled) контролов.

### Инвертированные кисти (для темных фонов)

Если вы рисуете текст поверх акцентного цвета (например, белый текст на синей кнопке):

- `SystemControlForegroundAltHighBrush` — Максимально контрастный инвертированный текст.

## Основные ресурсы для границ

- `SystemControlHighlightBaseMediumLowBrush` — стандартный цвет границы для большинства текстовых полей и кнопок в обычном состоянии.
- `SystemControlHighlightAccentBrush` — цвет границы при фокусе или активном состоянии (акцентный цвет системы).
- `ControlElevationBorderBrush` — используется в теме Fluent для создания эффекта легкой тени или приподнятости границ.
- `Container1BorderBrush / Container2BorderBrush` — специфические ресурсы для контейнеров и панелей.
