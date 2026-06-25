----------------------------
---------- TODO ------------
----------------------------

- markdown preview mode: markdown preview mode will render the formatting but the content will still be editable
- / commands in md and txt versions. / commands and utilities are different for md and txt versions. for md, the options will be to insert a table, ol list, ul list, info box, warning box, check list, quotation etc. for txt the options would include inserting predefined templates of text for common text decorations. insert emojis options will be available to both file types
- color theme and other settings should be customizable via a json file

- app icon and branding
- toolbar for markdown or text file formatting
- rich text format support with toolbar for colors and other stuff which rich text has
- ctrl + and ctrl - to zoom in and out the whole app
- README
- ctrl + S shortcut for saving.
- auto saving as an optional feature in settings
- double click folder on sidebar to toggle expand them

## Navigation & search (biggest gap right now)
- Global search across all notes — search content in every file in the workspace, not just the open one. With match preview snippets.
- Find & replace in the current document (regex optional). Table-stakes for an editor and not on your list.
- Quick-open / command palette — Ctrl+P to jump to any file by fuzzy name; Ctrl+Shift+P for commands. Pairs naturally with your / commands.
- Document outline panel — auto-generated from markdown headings, click to jump. Great for long notes.
- Wiki-style [[links]] between notes + backlinks panel. This is what turns a note app into a knowledge base (Obsidian's core idea) and your folder of SampleNotes suggests that use case.

## File & workspace management
- **NOT FAVOR** Tabs or split view for multiple open files side by side.
- Pinned / favorite notes and a recent-files list.
- Quick note / scratchpad — a global hotkey to capture a thought without picking a location.
- Move/rename/drag in the sidebar with link-reference updating.
- **NOT FAVOR** Trash / soft-delete instead of permanent removal.

## Editing power
- Auto-save + local version history — periodic snapshots you can diff and restore. Cheap insurance, high trust.
- Word/character/reading-time count in a status bar.
- **MAYBE LATER** Spell check (locally available dictionaries, matching your "local fonts" philosophy).
- Markdown table editor — visual editing instead of raw pipes (extends your / table insert).
- Paste-as-markdown — paste an image and have it saved + linked; paste rich HTML and convert to markdown.
- Code block syntax highlighting in both edit and preview.

## Export & sharing
- Export to PDF / HTML / DOCX from markdown. Very common ask for a markdown app.
- Copy as rich text so it pastes formatted into email/Word.
- Print support with the rendered theme.

## Productivity / knowledge
- Tags and a tag browser (frontmatter or inline #tags).
- Aggregated task view — collect all - [ ] checkboxes across notes into one to-do list.
- Daily notes / journal templates (your Journal.md sample hints at this).
- Pin-to-top metadata via YAML frontmatter (title, date, tags).

## Polish & platform
- Distraction-free / focus mode (hide sidebar & chrome, center text).
- Session restore — reopen the files and cursor positions from last run.
- Portable mode — keep settings next to the app so it runs from a USB stick.
- Auto-update mechanism.


----------------------------
---------- DONE ------------
----------------------------

- un supported file types should still be shown (if set in settings) but with a custom svg icon like a gray question mark.
- settings UI panel for 
    - color theme
    - formatting
    - font selection (from locally available fonts)
    - etc
- the side bar icons should be customizable by replacing svgs in a folder

