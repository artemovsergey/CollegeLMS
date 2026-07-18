# Редизайн публичной части — tpu.ru

**Дата:** 2026-07-17
**Статус:** Утверждён

## 1. Цель

Полный редизайн публичной части сайта CollegeLMS в стиле tpu.ru. Все визуальные решения, компоновка и стилистика повторяют ТПУ.

## 2. Архитектура

### 2.1. DesignProvider

Контекст, устанавливающий `data-design="tpu"` на `<html>`. Аналог `ThemePresetProvider` для полного дизайна.

- `lib/design-provider.tsx`
- Тип: `"default" | "tpu"`
- Default: `"default"` (текущий дизайн без изменений)
- Хранит в localStorage ключ `design-preset`

### 2.2. DesignTokens — globals.css

Блок `[data-design="tpu"]` с CSS-кастомными свойствами: шрифты, цвета, отступы, радиусы, тени.

### 2.3. Компоненты

Каждый публичный компонент проверяет дизайн через `useDesign()`:
- `"tpu"` → TPU-вариант (изменённая структура/стилизация)
- `"default"` → текущая реализация

## 3. Компоненты

### 3.1. HeaderTPU
- Прозрачный на главной (`bg-transparent`), белый при скролле
- Навигация по центру, лого слева, поиск + вход справа
- Mega-menu: 2-3 колонки подразделов при наведении на раздел

### 3.2. Hero (CarouselTPU)
- Full-width, 80vh высота
- Overlay-градиент, текст слева, CTA
- Автопрокрутка 5 сек, dots снизу

### 3.3. Секции главной
- SpecialtiesSectionTPU: grid 3×2, карточки с тенью
- StatisticsSectionTPU: 4 блока с числами
- AdmissionSectionTPU: CTA-блок
- EventsSectionTPU: горизонтальные карточки
- NewsSectionTPU: grid 3 колонки
- PartnersSectionTPU: логотипы grayscale
- FAQSectionTPU: accordion
- FeedbackFormTPU: компактная форма

### 3.4. FooterTPU
- Тёмный фон, 4 колонки
- Копирайт снизу

### 3.5. SectionPageTPU
- Сайдбар 240px + контент
- Хлебные крошки

## 4. Порядок реализации

1. CSS-токены
2. DesignProvider
3. Layout + страницы
4. HeaderTPU + MegaMenu
5. CarouselTPU
6. Секции главной
7. FooterTPU
8. SectionPageTPU
9. DesignSwitcher
10. Адаптивность
