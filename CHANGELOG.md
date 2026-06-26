# Changelog

All notable changes to Notty are documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.1.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [1.0.0] - 2026-06-26

First public release.

### Added

**Workspace & files**
- File-based workspace — any folder you pick; the choice is remembered between launches.
- Folder tree with expand/collapse, breadcrumbs, and a depth cap (deeper folders become "doors" that re-root the tree).
- Create, rename, duplicate, and delete (to the Recycle Bin) notes and categories; reveal in Explorer.
- Optional display of unsupported file types in the tree (with a neutral "?" icon).
- Recent-documents tracking (most recent 20).

**Editor**
- Code editor powered by AvalonEdit: word wrap, line numbers, configurable tab width, current-line highlight, font family/size.
- Smart Markdown list editing — `Enter` continues a list (and renumbers the next ordered item), `Enter` on an empty item outdents or exits the list, `Tab` / `Shift+Tab` indent/outdent.
- Slash commands — type `/` to insert elements; the menu adapts to Markdown vs. plain-text files, with interactive prompts for table size and image selection.
- `Down` on the last line jumps to end-of-line; `Up` on the first line jumps to start-of-line.

**Markdown preview (inline & editable)**
- Render GitHub-Flavored Markdown in place while keeping the text editable: headings, bold/italic/bold-italic, strikethrough, inline and fenced code, lists, task lists, tables, blockquotes, horizontal rules, links, images, and autolinks.
- GitHub alerts (`> [!NOTE]`, `[!TIP]`, `[!IMPORTANT]`, `[!WARNING]`, `[!CAUTION]`) rendered as colored callout boxes.
- Full-width backgrounds for fenced code blocks and alert blocks.

**Saving**
- Optional auto-save (on by default) ~500 ms after typing stops; manual save with `Ctrl+S`; changes flushed on file switch and on close.
- Live save-status indicator in the status bar (Unsaved changes → Saving… → Saved, with relative time).

**Theming & customization**
- Built-in Light and Dark themes; add your own by dropping a JSON palette into `%AppData%\Notty\Themes`.
- Settings UI for theme, fonts, formatting, saving, and file visibility.
- Swappable sidebar SVG icons.

**Branding**
- App icon, welcome-screen logo, and a branded About window.

[1.0.0]: https://github.com/Blankscreen-exe/Notty-Text-Editor/releases/tag/v1.0.0
