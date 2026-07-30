# Koralytics Angular — Design System Analysis

## Project Structure Overview

```
src/
├── app/
│   ├── features/         # Feature modules (player, academy-admin, coach, etc.)
│   ├── layouts/          # dashboard-layout, auth-layout
│   └── app.routes.ts     # Routing
├── shared/
│   ├── components/       # 30 reusable components
│   └── directives/       # scrollReveal, etc.
├── styles/
│   └── index.css         # Imports: tokens (colors, radius, shadows, spacing, transitions, typography) + base (reset, globals, animations)
└── styles.css            # Root: scrollReveal animations, box-sizing, bg-color #06080a
```

---

## Design System: Colors

| Token / Class        | Hex / Value                      | Meaning                        |
|----------------------|----------------------------------|--------------------------------|
| `--accent`           | `#c8ff4d`                        | Primary lime/neon — base tier  |
| `--tier-neon` (base) | `#c8ff4d`                        | Player tier: Standard          |
| `--tier-neon` (gold) | `#ffd700`                        | Player tier: Gold              |
| `--tier-neon` (elite)| `#ff6a00`                        | Player tier: Elite/Orange      |
| `--color-green`      | `#a3e635`                        | Win / positive / drill         |
| `--color-yellow`     | `#facc15`                        | Draw / warning / match         |
| `--color-red`        | `#f87171`                        | Loss / error / danger          |
| `--color-blue`       | `#38bdf8`                        | Transfer canvas / info         |
| `--electric-lime`    | `#ccff00`                        | Academy comparison highlight   |
| `--electric-cyan`    | `#00e5ff`                        | Academy avg benchmark          |
| **Background root**  | `#06080a`                        | Global page background         |
| Card background      | `#11141a` / `#12161f` / `#0d1117`| Section cards                  |
| Card hover bg        | `#161b27`                        | Hover state on cards           |
| Border               | `rgba(255,255,255,0.06)`         | Default subtle borders         |
| Border hover         | `rgba(255,255,255,0.18)`         | Hover border                   |
| `--text-main`        | `#f3f4f6`                        | Primary text                   |
| `--text-dim`         | `#8b909a`                        | Dim/secondary text             |
| `--text-muted`       | `#6b7280`                        | Muted/label text               |
| Muted label          | `#808897`                        | Bio labels, panel headers      |

---

## Design System: Typography

| Role               | Font Family                              | Weight / Size     |
|--------------------|------------------------------------------|-------------------|
| Display / Headers  | `'Bebas Neue'`, `'Oswald'`, sans-serif   | 700 / large       |
| Body               | `'Inter'`, sans-serif                    | 400–700           |
| Stats / KPI Values | `'Bebas Neue'` or `'Oswald'`             | 700 / 42–96px     |
| Section Subtitles  | `'Inter'` uppercase                      | 700 / 11–12px     |
| Meta / Labels      | `'Inter'` uppercase, letter-spacing 1px  | 700 / 10–11px     |

---

## Design System: Spacing & Layout

- **Page wrapper**: `max-width: 1180px` (profile) / `850px` (timelines) / `1240px` (comparison), `margin: 0 auto`, `padding-top: 100–120px`
- **Gap between sections**: `20–28px`
- **Card padding**: `18–28px`
- **Border radius**: `14–22px` (panels), `20px` (hero), `16px` (cards), `12–14px` (inner panels)
- **Grid layouts**: 
  - Main profile grid: `grid-template-columns: 244px 1fr 300px`
  - KPI grid: `repeat(4, 1fr)`
  - Breakdown: `repeat(3, 1fr)`
  - Dashboard comparison: `420px 1fr`

---

## Design System: Animations & Transitions

| Name                  | Definition                                                                 |
|-----------------------|----------------------------------------------------------------------------|
| `fadeInUp`            | `opacity 0→1, translateY(20px→0)`                                         |
| `avatarPulse`         | `box-shadow + border-color pulse` with tier-neon over 3s                  |
| `scrollReveal`        | Directive — adds `.revealed` class → opacity 0→1, transform none          |
| `tableRowFadeIn`      | `opacity 0→1, translateY(4px→0)` staggered by `--animation-order`         |
| `slideDown`           | `opacity 0→1, translateY(-10px→0)` for expanded rows                      |
| `pulseDot`            | Tactical dot pulse (box-shadow scale 1.5)                                  |
| `pulseGlow`           | MOTM ribbon glow loop                                                      |
| `radarPing`           | Transfer canvas radar node ring ping                                       |
| `sweepShimmer`        | Button shimmer sweep animation                                             |
| **Transition standard**| `0.3–0.4s cubic-bezier(0.16, 1, 0.3, 1)` for cards/buttons/panels        |

---

## Reusable Shared Components (`src/shared/components/`)

| Component             | Selector              | Purpose                                  |
|-----------------------|-----------------------|------------------------------------------|
| `CardComponent`       | `app-card`            | Panel wrapper (elevated/outlined/flat)   |
| `NavbarComponent`     | `app-navbar`          | Fixed top nav + floating sidebar trigger |
| `FooterComponent`     | `app-footer`          | Page footer                              |
| `CustomButtonComponent` | `app-button`        | Variants: accent/coral/cyan/slate/amber/gold; sizes: sm/md/lg; shimmer/loading |
| `LoadingSpinnerComponent` | `app-loading-spinner` | size: lg/md/sm                       |
| `EmptyStateComponent` | `app-empty-state`     | No data state                            |
| `DataTableComponent`  | `app-data-table`      | Cyber-table with badges, row actions, expand |
| `PaginationComponent` | `app-pagination`      | Page navigation                          |
| `ConfirmDialogComponent` | `app-confirm-dialog` | Modal confirmation                      |
| `ModalContainerComponent` | `app-modal-container` | Generic modal                          |
| `SearchBarComponent`  | `app-search-bar`      | Unified search                           |
| `FilterPanelComponent`| `app-filter-panel`    | Sticky filter card                       |
| `CustomSelectComponent` | `app-select`        | Dropdown select                          |
| `CustomInputComponent` | `app-text-input`     | Text input field                         |
| `CustomDatePickerComponent` | `app-date-picker` | Date picker                          |
| `CustomToggleComponent` | `app-toggle`        | Toggle/checkbox                          |
| `PhoneInputComponent` | `app-phone-input`     | Phone number input                       |
| `ToastComponent`      | `app-toast`           | Notification toast                       |
| `StatusChipComponent` | `app-status-chip`     | Status badge                             |
| `RatingDisplayComponent` | `app-rating-display` | Star/numeric rating                    |
| `ChartComponent`      | `app-chart`           | Chart wrapper                            |
| `ImageUploadComponent` | `app-image-upload`   | Image upload with preview                |
| `FileUploadComponent` | `app-file-upload`     | Generic file upload                      |
| `FootballPitchComponent` | `app-football-pitch` | SVG tactical pitch                      |
| `LogoComponent`       | `app-logo`            | Koralytics logo                          |
| `StepperComponent`    | `app-stepper`         | Multi-step progress indicator            |

---

## Player Feature Components (UI Reference)

| Component                      | Key Design Patterns                                     |
|--------------------------------|---------------------------------------------------------|
| `player-profile`               | Hero banner, main grid (3-col), KPI grid, breakdown section, pitch panel, radar panel, pack overlay |
| `player-card`                  | 3D flip card, tier colors (base/gold/elite), stat bars  |
| `player-match-timeline`        | `page-wrapper` 850px, `koralytics-card`, filter-panel-card, `tactical-tag`, group-label, pagination |
| `player-drill-timeline`        | Same pattern as match-timeline                          |
| `player-team-events`           | Same `koralytics-card` pattern + left-border color      |
| `player-academy-comparison`    | `comparison-page` 1240px, radar card, `metric-card`, progress tracks |
| `player-scouter-views`         | Similar card/filter layout                              |
| `transfer-canvas`              | `matrix-card`, canvas quadrant heatmap, radar-pulse-node |

---

## CSS Class Patterns (Do Reuse)

### Card / Panel
```css
/* Standard dark panel */
background: #11141a;
border: 1px solid #1e232d;
border-radius: 18px;
padding: 16–28px;
transition: border-color 0.3s ease, box-shadow 0.3s ease, transform 0.3s cubic-bezier(0.16, 1, 0.3, 1);

/* Hover */
:hover { border-color: var(--tier-border-glow); transform: translateY(-4px); box-shadow: 0 12px 30px var(--tier-glow); }
```

### Hero Banner
```css
background: linear-gradient(135deg, #11141a 0%, #0c0e12 100%);
border: 1px solid #1e232d;
border-radius: 20px;
padding: 20px 28px;
/* Top accent line: */
::before { height: 2px; background: linear-gradient(90deg, var(--tier-neon), transparent 70%); }
```

### KPI Card
```css
.kpi-card { background: #11141a; border-radius: 16px; border: 1px solid #1e232d; padding: 18px; }
.kpi-value { font-family: 'Bebas Neue'; font-size: 42px; }
.kpi-header { font-size: 11px; font-weight: 700; color: #808897; text-transform: uppercase; }
```

### Timeline / List Card
```css
.koralytics-card { background: #12161f; border-radius: 14px; padding: 18px 24px;
  transition: transform 0.35s cubic-bezier(0.16, 1, 0.3, 1), border-color 0.35s ease; }
.koralytics-card:hover { transform: translateY(-4px); box-shadow: 0 12px 30px -8px rgba(0,0,0,0.6); }
```

### Pill / Badge
```css
.pill { background: #181c24; border: 1px solid #272d3a; padding: 3px 10px; border-radius: 20px; font-size: 11px; }
.badge { font-size: 0.6rem; font-weight: 800; padding: 2px 8px; border-radius: 4px; letter-spacing: 0.5px; }
```

### Filter Panel
```css
.filter-panel-card { position: sticky; top: 0; z-index: 10;
  background: rgba(18, 22, 31, 0.94); border-radius: 14px;
  backdrop-filter: blur(16px); box-shadow: 0 8px 32px rgba(0,0,0,0.5); }
```

### Section Label / Tactical Tag
```css
.tactical-tag { color: var(--color-green); font-size: 0.7rem; font-weight: 700; letter-spacing: 1.5px; text-transform: uppercase; }
.tactical-tag::before { 6×6px dot, background: var(--color-green); animation: pulseDot 2s; }
.panel-header { font-size: 11px; font-weight: 800; color: #808897; text-transform: uppercase; letter-spacing: 1px; }
```

### Stats Block
```css
.stats-block { background: rgba(0,0,0,0.35); border-radius: 10px; padding: 10px 18px; }
.stat-pill { display: flex; flex-direction: column; align-items: center; }
.stat-pill .lbl { font-size: 0.58rem; font-weight: 700; color: #6b7280; letter-spacing: 1px; }
.stat-pill .val { font-family: 'Oswald'; font-size: 1.1rem; font-weight: 700; }
```

### Bio Quick List (Hero section stats)
```css
.bio-quick-list { display: flex; gap: 20px; background: rgba(0,0,0,0.3); padding: 10px 18px; border-radius: 12px; }
.bio-lbl { font-size: 10px; font-weight: 700; color: #808897; text-transform: uppercase; }
.bio-val { font-size: 13px; font-weight: 700; color: #fff; }
```

---

## Data Table Patterns (`app-data-table`)

Cyber badge colors:
- `.cyber-badge.active` → `#c8ff4d` (lime)
- `.cyber-badge.injured` → `#ff6a5c` (red)
- `.cyber-badge.pending` → `#ffb84d` (amber)
- `.cyber-badge.admin` → `#4da6ff` (blue)

Action buttons: `.btn-view` (blue), `.btn-delete` (red on hover), `.btn-expand` (lime)

---

## Button Variants (`app-button`)
- `variant="accent"` → Lime `#c8ff4d` / dark text
- `variant="coral"` → Red `#ff6a5c`
- `variant="slate"` → Dark `#14171c` / white text
- `variant="amber"` → Orange gradient
- `variant="gold"` → Gold gradient
- `variant="cyan"` → Cyan `#00f0ff`
- `[shimmer]="true"` → Shimmer sweep effect
- `[loading]="true"` → Loading spinner overlay

---

## Page Layout Pattern

All full-page feature components follow this exact structure:
```html
<app-navbar></app-navbar>

<div class="page-wrapper">        <!-- or .profile-page / .comparison-page -->
  <!-- tactical-tag (breadcrumb) -->
  <!-- header-row with section-title and badge-count -->
  <!-- filter-panel-card (sticky) -->
  <!-- Loading / Error / Empty states -->
  <!-- Main content grid/list -->
  <!-- Pagination -->
</div>

<app-footer scrollReveal direction="bottom" [delay]="200"></app-footer>
```

---

## scrollReveal Directive
Used on virtually every section/card:
```html
<div scrollReveal direction="bottom" [delay]="0">...</div>
<div scrollReveal direction="bottom" [delay]="100">...</div>
```
Delays stagger by 50–80ms increments.

---

## Responsive Breakpoints
- `max-width: 768px` — Collapse grid to 1fr, flex-direction column
- `max-width: 900px` — Comparison grid collapses

---

## Key Rules for New Pages
1. **Always use** `<app-navbar>` + `<app-footer>` 
2. **Page wrapper** padded `padding-top: 100–120px` to clear fixed navbar
3. **Dark backgrounds only**: `#06080a` → `#0c0e12` → `#11141a` → `#12161f`
4. **Border colors**: `rgba(255,255,255,0.06)` default, `rgba(255,255,255,0.18)` hover
5. **Accent via `--tier-neon` / `--color-green` (#a3e635)** for feature highlights
6. **Transitions**: `cubic-bezier(0.16, 1, 0.3, 1)` or `cubic-bezier(0.2, 0.8, 0.2, 1)`
7. **Hover lift**: `transform: translateY(-4px)` on cards
8. **Loading state**: `<app-loading-spinner size="lg">` + `<p>Loading...</p>`
9. **Empty state**: `<app-empty-state title="..." description="...">`
10. **Tables**: `<app-data-table>` with cyber badges
11. **Reuse `.koralytics-card` / `.kpi-card` / `.hero-banner` CSS patterns**
12. **Never introduce new color schemes** — extend tier system only
