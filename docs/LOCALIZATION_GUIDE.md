# Koralytics Localization Guide

This document is the official guide for managing translations and localization within the Koralytics Angular Frontend. Every developer must follow this guide when adding or modifying translations to ensure consistency, high performance, and proper RTL support.

---

## 1. Overview

Koralytics is a multi-tenant global platform, which means supporting multiple languages is a strict requirement. 
- **Supported Languages**: English (`en`) and Arabic (`ar`).
- **Default Language**: English (`en`) is the default fallback language.
- **Language Switching**: The language state is preserved in `localStorage` (`koralytics_lang`) and managed globally via Angular Signals (`currentLang` signal).
- **RTL/LTR Behavior**: The document direction is automatically toggled by the `LocalizationService`. When Arabic is selected, `document.documentElement.dir` is set to `rtl` and `lang` to `ar`. CSS uses logical properties (or RTL-specific overrides) to flip layouts.

---

## 2. Architecture

Our localization architecture relies on `@ngx-translate/core` powered by a custom multi-file HTTP loader, allowing us to split translations by feature rather than loading a massive monolithic JSON file.

```text
User Action (Clicks "Ar")
       ↓
[LanguageSwitcherComponent] -> Triggers toggleLanguage()
       ↓
[LocalizationService]
   ├── Updates 'currentLang' Signal
   ├── Updates 'localStorage'
   ├── Toggles 'dir' (rtl/ltr) on <html>
   └── Calls TranslateService.use('ar')
       ↓
[TranslateService]
       ↓
[CustomTranslateLoader]
   ├── Fetches ar/common.json
   ├── Fetches ar/auth.json
   ├── Fetches ar/system-admin.json
   └── Merges JSON objects via forkJoin
       ↓
Angular Components (Pipes / Signals / Getters)
       ↓
UI Updates Instantly (Hot-Swapped)
```

**Layers**:
- **LocalizationService**: The brain. Manages state, signals, and DOM direction.
- **CustomTranslateLoader**: Optimization layer. Fetches only the required JSON modules instead of one giant file.
- **JSON Translation Files**: The source of truth for text. Stored in `public/i18n/`.

---

## 3. Folder Structure

Translations are stored in the `public` folder so they can be served as static assets. The structural logic splits code (services) from content (JSON files).

```text
Koralytics.Angular/
├── public/
│   └── i18n/
│       ├── ar/
│       │   ├── auth.json             # Auth & Registration translations
│       │   ├── common.json           # Reusable global strings (Pagination, Buttons)
│       │   └── system-admin.json     # System Admin dashboard translations
│       └── en/
│           ├── auth.json
│           ├── common.json
│           └── system-admin.json
├── src/
│   ├── app/
│   │   └── app.config.ts             # TranslateService Provider Setup
│   ├── core/
│   │   ├── i18n/
│   │   │   └── custom-translate-loader.ts # Multi-file fetching logic
│   │   └── services/
│   │       └── localization.service.ts    # State & RTL management
│   └── shared/
│       ├── components/
│       │   └── language-switcher/         # UI Dropdown to switch languages
│       └── pipes/
│           └── localized-date.pipe.ts     # Translates date formats automatically
```

---

## 4. How Translation Works (Under the Hood)

When a user visits the app or clicks "Arabic":
1. **Trigger**: `LocalizationService.setLanguage('ar')` is called.
2. **DOM Update**: The service immediately sets `<html lang="ar" dir="rtl">`. Global CSS kicks in to flip the layout.
3. **Fetching**: `TranslateService.use('ar')` is invoked. It delegates to `CustomTranslateLoader`.
4. **Network Calls**: The loader fires parallel HTTP GET requests (via `forkJoin`) to fetch `/i18n/ar/common.json`, `/i18n/ar/auth.json`, etc.
5. **Merging**: The JSON responses are merged into a single dictionary object in memory.
6. **Rendering**: Angular Change Detection runs. Any template using the `| translate` pipe or `this.translate.instant()` re-evaluates and displays the Arabic text.

---

## 5. How to Translate a New Feature

When building a new feature (e.g., `ScoutingDashboard`), follow these exact steps:

**Step 1: Create the Translation Keys in JSON**
Open both `en/scouting.json` and `ar/scouting.json` (create them if they are new, and register the module in `CustomTranslateLoader`).
```json
// en/scouting.json
{
  "SCOUTING": {
    "TITLE": "Scouting Dashboard",
    "BTN_SEARCH": "Find Players"
  }
}
```

**Step 2: Update the Template (HTML)**
Replace hardcoded strings with the `translate` pipe.
```html
<!-- BAD: Hardcoded -->
<h1>Scouting Dashboard</h1>
<button>Find Players</button>

<!-- GOOD: Localized -->
<h1>{{ 'SCOUTING.TITLE' | translate }}</h1>
<button>{{ 'SCOUTING.BTN_SEARCH' | translate }}</button>
```
*Note: Make sure your standalone component imports `TranslatePipe`.*

**Step 3: Update the Component (TypeScript)**
If you need translated text for variables, dialogs, or dropdowns, inject `TranslateService` or bind raw keys to translated child components.
```typescript
// Component
private translate = inject(TranslateService);

// For Dropdowns using custom-select, pass the raw key (the component pipes it)
statusOptions = [
  { label: 'SCOUTING.STATUS_ACTIVE', value: 'Active' } 
];

// For imperative dialogs
showDialog() {
  const msg = this.translate.instant('SCOUTING.CONFIRM_MSG', { name: user.name });
}
```

**Step 4: Dates and Formatting**
Never use the standard `date` pipe. Always use `localizedDate`.
```html
<span>{{ user.createdAt | localizedDate:'mediumDate' }}</span>
```

**Step 5: Verify RTL**
Run the app, switch to Arabic, and ensure:
- Text aligns right (`text-align: start` is your friend).
- Margins and paddings flip (use `margin-inline-start` instead of `margin-left`).
- Icons point the correct way.

---

## 6. Translation Rules

You **MUST** adhere to these rules. No exceptions.

- ❌ **Never hardcode visible strings.** Every user-facing text must be a translation key.
- ✅ **Use Namespaces.** Group keys logically.
  - Good: `AUTH.LOGIN.TITLE`
  - Bad: `LOGIN_TITLE`
- ✅ **Use `common.json` for shared strings.** Do not duplicate keys like "Save", "Cancel", or Pagination text across different feature JSONs.
- ✅ **Keep English as the source language.** Develop in English first, verify layout, then add Arabic.
- ✅ **Arabic must be natural.** Avoid literal machine translations. Ensure grammar (e.g., pluralization, gender) makes sense in context.

---

## 7. Common Mistakes & Fixes

| Mistake | Symptom | Fix |
| :--- | :--- | :--- |
| **Missing `TranslatePipe` import** | `NG8004: No pipe found with name 'translate'` | Add `TranslatePipe` to the `imports: []` array of your standalone component. |
| **Missing Translation Key** | UI displays the raw key (e.g., `AUTH.LOGIN.TITLE`) | Add the exact key path to both English and Arabic JSON files. Ensure the JSON syntax is valid. |
| **Invalid JSON Commas** | Entire translation file fails to load (silent failure or console error) | Check for trailing commas in your JSON file. Use a linter. |
| **RTL Alignment Broken** | Text is stuck on the left in Arabic | Replace `text-align: left` with `text-align: start`, or `margin-left` with `margin-inline-start`. |
| **`translate.instant()` not updating** | Text stays English after switching language | `instant()` only evaluates once. Use the `| translate` pipe in templates, or manually listen to `onLangChange` events if you must set variables in TS. |

---

## 8. Debugging Guide

- **Text not translated / Key displayed instead**: 
  1. Open Network tab. Did the JSON file load? (e.g., `/i18n/en/system-admin.json`).
  2. If it loaded, verify the JSON structure matches your key exactly. `{"AUTH": {"TITLE": ""}}` matches `AUTH.TITLE`.
- **JSON not loading (404)**: 
  1. Ensure the JSON file exists in `public/i18n/`.
  2. Ensure the module is added to the `modules` array in `custom-translate-loader.ts`.
- **RTL not changing**: 
  1. Inspect the `<html>` tag. Does it say `dir="rtl"`? If not, check `LocalizationService`.
- **Dates not translating**:
  1. Ensure you imported and used `LocalizedDatePipe` instead of Angular's default `date` pipe.

---

## 9. Best Practices

- **Feature-Based Translation**: Keep JSON files small. If you build a new `Scouting` module, create a `scouting.json` file. Do not dump everything into `common.json`.
- **Dynamic Variable Injection**: Use `{{name}}` interpolation for dynamic data to allow translators to change the word order. 
  - *Good*: `"CONFIRM": "Delete account for {{name}}?"` -> `{{ 'CONFIRM' | translate:{name: user.name} }}`
  - *Bad*: `"CONFIRM": "Delete account for "` -> `{{ 'CONFIRM' | translate }} + user.name`
- **Enum Translations**: Map backend Enums directly to keys (e.g., `ROLE_` + `role.toUpperCase()`).

---

## 10. Pull Request Checklist

Every developer must check these off before opening a PR:

- [ ] I have verified there is absolutely NO hardcoded user-facing text in my HTML or TS.
- [ ] I have added all new translation keys to the English JSON files.
- [ ] I have added natural, accurate translations to the Arabic JSON files.
- [ ] I have switched the app to Arabic and verified that the RTL layout (margins, alignment, icons) looks perfect.
- [ ] My JSON files are formatted correctly with no syntax errors.
- [ ] I reused existing `common.json` keys (e.g., Save, Cancel) instead of duplicating them.

---

## 11. Future Improvements

As the platform scales, the localization architecture can be enhanced with:
- **Automated Missing-Key Detection**: Add a CI/CD pipeline step (using tools like `ngx-translate-extract`) to fail builds if keys are missing in `ar` but exist in `en`.
- **Translation Linting**: Ensure JSON files are alphabetized and adhere to strict schemas.
- **Dynamic Locale Loading for Dates**: Dynamically import Angular locale data chunks at runtime instead of statically bundling `locales/ar-EG` in `app.config.ts`, reducing initial bundle size.


## Frequently Encountered Issues & Pitfalls

1. **`translate.instant()` in TypeScript (The "Reload Required" Bug)**:
   - **Issue**: Using `this.translate.instant('KEY')` when mapping data in a component (e.g., inside a `.subscribe` or `.map`) sets the translation statically on load. It will NOT update dynamically when the user toggles the language switch, forcing the user to reload the page to see changes.
   - **Solution**: Instead of translating in TypeScript, pass the raw translation *key* directly into your view model (e.g., `role: 'ACADEMY_ADMIN.ADMINS_SECTION.OWNER'`). Then, use the `| translate` pipe in the HTML template to dynamically resolve it. For dynamic table cells, rely on the `translate: true` property in the `TableColumn` config.

2. **Breaking CSS Badges and Dynamic Classes**:
   - **Issue**: Often, a raw value from the backend (like 'owner', 'active', 'pending') is used both as the text to display AND as the CSS class (e.g., `<span class="badge {{ role }}">`). If you translate this value before applying it to the HTML, the CSS breaks because the browser tries to apply Arabic words as classes (e.g., `<span class="badge المالك">`).
   - **Solution**: Always separate the raw value from the display value in your view models. Provide two properties: one for the raw CSS logic (`adminRoleRaw: 'owner'`) and one for the translation key (`adminRole: 'ACADEMY_ADMIN.OWNER'`).

3. **Reusable Dialogs and Shared Components Missing Translations**:
   - **Issue**: Hardcoding plain English text as inputs to shared components (e.g., `<app-confirm-dialog title="Remove Member">`).
   - **Solution**: Upgrade the shared component templates to natively pipe their inputs through `| translate` (e.g., `<h2>{{ title | translate }}</h2>`). The parent component should then exclusively pass the translation *key* (e.g., `[title]="'ACADEMY_ADMIN.REMOVE_TITLE'"`).

4. **Table Header (th) RTL Alignment Fluidity**:
   - **Issue**: In HTML/CSS, table headers often default to `text-align: center` or `left`. Writing `[dir="rtl"] th { text-align: right; }` forces right alignment in Arabic, but can cause English headers to misalign or center improperly depending on the browser.
   - **Solution**: Use `th { text-align: start !important; }`. The `start` logical property is completely fluid—it aligns left natively in LTR (English) and naturally mirrors to the right in RTL (Arabic) without needing explicit `[dir="rtl"]` overrides.

5. **Translating Dynamic Statuses / Enums**:
   - **Issue**: When a column renders a status (e.g., "Active", "Pending"), these are often derived dynamically. 
   - **Solution**: Construct the translation key dynamically `squadStatus: 'ACADEMY_ADMIN.MEMBERS.STATUS_' + status.toUpperCase()`, rather than hardcoding English strings or using `.instant()`.

6. **Nested Interpolation Errors**:
   - **Issue**: Using `{{ 'KEY' | translate }}` inside another `{{ ... }}` block or ternary operator will cause an Angular Parser Error. 
   - **Solution**: Within a data-binding expression, use the pipe directly: `{{ condition ? ( 'KEY1' | translate ) : ( 'KEY2' | translate ) }}` without nested curly braces.

7. **Returning Hardcoded English Strings from Getters/Properties**:
   - **Issue**: Sometimes developers create getters (e.g., `get footLabel() { return 'Right'; }`) or define arrays (e.g., `options = [{ label: 'Right' }]`) that output raw English strings, expecting them to magically translate later.
   - **Solution**: Return the translation key from the getter or property (`return 'PROFILE.FOOT_RIGHT';`), and ensure the HTML template or child component runs that property through the `| translate` pipe or native `TranslateService` logic.

8. **Translating Instructional Text with Dynamic UI References**:
   - **Issue**: When an instructional string references a UI element (e.g., "Click 'Save Changes' below"), splitting the string into multiple pieces around the dynamic UI text is messy and hard to maintain.
   - **Solution**: Create a single translation key that takes a parameter: `"INSTRUCTION": "Click {{btn}} below"`, and use interpolation in the template to pass the translated button name: `{{ 'INSTRUCTION' | translate:{ btn: ('COMMON.SAVE' | translate) } }}`.

9. **Handling Custom Validator Error Messages**:
   - **Issue**: Angular Custom Validators often return error objects with hardcoded English messages (e.g., `{ invalidPhone: { message: 'Phone number is invalid' } }`). Parent components mistakenly display this raw `message` string instead of translating it.
   - **Solution**: Avoid rendering validator `.message` strings directly. Instead, have the custom component internally manage its translated `errorMessage` (using `TranslateService.instant()` or `TranslatePipe`), or have the parent component detect the error key (`invalidPhone`) and construct the translation string using localized parameters dynamically.
