# MultiLanguage

A [MelonLoader](https://melonwiki.xyz/) mod for **Slendytubbies 3 Multiplayer** that adds 16 extra languages to the game and replaces the tiny language list with a proper flag picker.

[Русская версия ниже](#русский)

## What it does

- Adds 16 new languages to the in-game language menu (see the table below).
- Clicking the flag in the bottom left corner opens a panel with round flags and the name of every language written in that language.
- Translations and flags are embedded into the DLL, so the mod doesn't create any files in `UserData`.
- Works with Cyrillic, Greek, Latin with diacritics, Japanese and Korean text. The game's own font doesn't have all of these letters, so the mod swaps in another font that the game already has loaded.

## Languages

| Code | Language | Code | Language |
|------|----------|------|----------|
| `ru` | Русский | `hu` | Magyar |
| `uk` | Українська | `fi` | Suomi |
| `bg` | Български | `hr` | Hrvatski |
| `sr` | Српски | `el` | Ελληνικά |
| `de` | Deutsch | `ja` | 日本語 |
| `it` | Italiano | `ko` | 한국어 |
| `pl` | Polski | `tr` | Türkçe |
| `cs` | Čeština | `sv` | Svenska |

The built-in languages (English, Spanish, Doge, Danish, Dutch, Portuguese, Macedonian, Chinese, French, Romanian) are left to the game.

## Requirements

- Slendytubbies 3 Multiplayer **2.62+**
- MelonLoader **0.7.x** (IL2CPP)

The game's translation method has an obfuscated name that changes between versions. After a game update the mod may not find it. In that case it writes an error to the log and the custom languages stay in English.

## Installation

1. Install MelonLoader into the game folder and launch the game once.
2. Download `MultiLanguage.dll` from the Releases page.
3. Put it into the `Mods` folder of the game.
4. Start the game and click the flag in the bottom left corner of the main menu.

## Adding or fixing a language

Everything lives in two folders next to the project file:

```
Flags/<code>.png   flag, any size or proportions
Lang/<code>.txt    translation, UTF-8
```

---

## Русский

**MultiLanguage** — мод для **Slendytubbies 3 Multiplayer** (MelonLoader 0.7.x, IL2CPP). Добавляет 16 новых языков и заменяет маленький список языков на панель с круглыми флагами.

### Что делает

- Добавляет в игру 16 языков (таблица выше).
- Клик по флагу в левом нижнем углу открывает панель: круглые флаги, под ними название языка на нём самом.
- Переводы и флаги зашиты в DLL, в `UserData` мод ничего не создаёт.
- Кириллица, греческий, латиница с диакритикой, японский и корейский отображаются: в шрифте игры нужных букв нет, поэтому мод подставляет другой шрифт, который игра уже загрузила.

### Установка

1. Поставьте MelonLoader в папку игры и один раз запустите игру.
2. Скачайте `MultiLanguage.dll` на странице Releases.
3. Положите его в папку `Mods`.
4. Запустите игру и нажмите на флаг в левом нижнем углу главного меню.

Нужна версия игры **2.62+**.

### Как добавить или поправить язык

Всё лежит в двух папках рядом с проектом:

```
Flags/<код>.png   флаг, любой размер и пропорции
Lang/<код>.txt    перевод, UTF-8
```
