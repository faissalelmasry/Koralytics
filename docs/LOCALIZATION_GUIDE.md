# Koralytics Localization Guide

This guide outlines how localization (i18n) works in the Koralytics Angular Frontend, how to add translations for new features, and the common pitfalls to avoid based on our development experience.

---

## 1. How Localization Works

Koralytics supports English (`en`) and Arabic (`ar`), using `@ngx-translate/core` with a custom multi-file loader.

- **State Management**: The `LocalizationService` acts as the brain. It manages the current language signal, persists user preference in `localStorage` (`koralytics_lang`), and instantly toggles the `dir` attribute on the `<html>` tag to flip the layout for RTL languages.
- **RTL Support**: When Arabic is selected, the document direction is set to `rtl` (`dir="rtl"`) and language to `ar`. Our global CSS relies on CSS Logical Properties (like `margin-inline-start` instead of `margin-left`, or `text-align: start` instead of `text-align: left`) to automatically mirror layouts.
- **JSON Dictionaries**: Translations are stored as JSON files split by feature inside `public/i18n/ar/` and `public/i18n/en/`. We split them (e.g., `drills.json`, `auth.json`, `common.json`) to keep the files organized and small.
- **Dynamic Loading**: Our `CustomTranslateLoader` uses `forkJoin` to fire parallel HTTP requests, fetching only the necessary JSON files for the active modules and merging them into a single dictionary in memory. This keeps initial bundle sizes small and the app highly performant.

---

## 2. Implementing New Feature Localization

When building or updating a feature, follow these steps to localize it:

### Step 1: Add Keys to JSON Files
Always add your translation keys to both the English and Arabic JSON files for that feature. Use clear, namespaced keys to avoid collisions. Do not duplicate global words (like "Save" or "Cancel"); reuse keys from `common.json` instead.
```json
// public/i18n/en/feature.json
{
  "FEATURE": {
    "TITLE": "Feature Title",
    "GREETING": "Hello {{name}}!"
  }
}
```

### Step 2: Use the Translate Pipe in HTML
Never hardcode user-facing strings in HTML. Use the `translate` pipe.
```html
<!-- Static text -->
<h1>{{ 'FEATURE.TITLE' | translate }}</h1>

<!-- Dynamic text with parameters -->
<p>{{ 'FEATURE.GREETING' | translate:{ name: userName } }}</p>
```
*Note: Make sure your standalone component includes `TranslatePipe` in its `imports: []` array.*

### Step 3: Localizing Dates
Angular's default `date` pipe doesn't natively handle runtime dynamic locale switching well. Always use our custom `LocalizedDatePipe`.
```html
<!-- BAD -->
<span>{{ session.date | date:'mediumDate' }}</span>

<!-- GOOD -->
<span>{{ session.date | localizedDate:'mediumDate' }}</span>
```

### Step 4: Use Getters for TypeScript Variables
If you need to supply translated dropdown options or component inputs from TypeScript, **always use a getter** so the translations update dynamically when the user toggles the language.
```typescript
// BAD: Evaluates once in the constructor/ngOnInit. Will get stuck in English if the user switches to Arabic!
statusOptions = [
  { value: 'Active', label: this.translate.instant('STATUS.ACTIVE') }
];

// GOOD: Re-evaluates automatically when Angular's change detection runs
get statusOptions() {
  return [
    { value: 'Active', label: this.translate.instant('STATUS.ACTIVE') }
  ];
}
```

---

## 3. Common Issues During Development

Here are the most frequent localization issues we've encountered and how to fix them:

### 1. `translate.instant()` Sticking to One Language (The "Reload Bug")
- **Issue**: Developers often use `this.translate.instant('KEY')` in `ngOnInit()` or when defining class properties. This translates the text exactly once upon load. If the user clicks "Arabic", the text remains in English until they refresh the page.
- **Fix**: Use the `| translate` pipe directly in the HTML whenever possible. If you must translate in TypeScript (like for Dropdown arrays), wrap the array in a `get propertyName()` block so it re-evaluates dynamically as shown in Step 4.

### 2. Hardcoded Dynamic Database Values (Enums & Names)
- **Issue**: Backend data like category names (`"Passing"`) or Drill Modes (`"Manual"`) render in English because they are displayed directly (e.g., `{{ drill.categoryName }}`).
- **Fix**: Map the dynamic value to a strict key namespace and pipe it through translate. If the translation isn't found, it safely falls back to the original text if you set up fallback logic, but you should aim to cover all dynamic keys in the JSON files.
  ```html
  <span>{{ ('DRILLS.DYNAMIC.CAT_' + drill.categoryName.toUpperCase()) | translate }}</span>
  ```

### 3. Hardcoded Modal & Dialog Messages
- **Issue**: Setting a confirmation modal message via template strings in TypeScript causes untranslated English to appear (e.g., `` message: `Are you sure you want to remove ${drillName}` ``).
- **Fix**: Use a parameterized translation key, preserving the dynamic data.
  ```typescript
  // In Component:
  this.confirmModal = {
    message: 'DRILLS.REMOVE_CONFIRM_MSG',
    messageParams: { name: drillName }
  };
  ```
  ```html
  <!-- In HTML: -->
  <p>{{ confirmModal.message | translate:confirmModal.messageParams }}</p>
  ```

### 4. Placeholder Attributes Not Translating
- **Issue**: Passing hardcoded strings to component inputs like `placeholderText="Search..."` will not translate.
- **Fix**: Use property binding `[placeholderText]` with the translate pipe.
  ```html
  <app-search-bar [placeholderText]="'COMMON.SEARCH' | translate"></app-search-bar>
  ```

### 5. HTML Entities and Uppercase CSS
- **Issue**: If you use HTML entities in your JSON translations (like `&bull;`) and the HTML element uses `text-transform: uppercase`, the browser converts it to `&BULL;` which breaks the rendering (it shows the raw text instead of a dot).
- **Fix**: Avoid HTML entities in JSON. Use raw Unicode characters directly (e.g., `•` instead of `&bull;`).

### 6. Custom Validator Error Messages
- **Issue**: Angular Custom Validators often return error objects with hardcoded English messages (e.g., `{ invalidPhone: { message: 'Phone number is invalid' } }`). 
- **Fix**: Do not render the raw `.message` property in the UI. Make the component check for the error key (`invalidPhone`) and map it to a translation key (e.g., `ERRORS.INVALID_PHONE`) in the HTML template.
