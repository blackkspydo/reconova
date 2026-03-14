# Reconova Design Language

**Brand Essence:** Threat intelligence command center — dark, intense, glassy. Red signals danger, authority, and protection.

**Keywords:** `Vigilant · Precise · Shield · Red Alert · Night Operations`

---

## Color Palette

### Core Brand

| Role | Hex | HSL | Usage |
|------|-----|-----|-------|
| **Primary (Red Hot)** | `#E53E3E` | `0 74% 57%` | Brand red — CTAs, active states, logo accent |
| **Primary Light** | `#FC5C5C` | `0 97% 67%` | Hover states, glows, highlights |
| **Primary Dark** | `#C53030` | `0 61% 48%` | Pressed states, active borders |
| **Primary Muted** | `rgba(229,62,62,0.15)` | — | Ambient glow, tinted backgrounds |

### Backgrounds & Surfaces

| Role | Hex | Usage |
|------|-----|-------|
| **Background** | `#0C0C10` | Page background — deep dark with warm undertone |
| **Surface 1** | `#161620` | Cards, sidebar, elevated panels |
| **Surface 2** | `#1E1E2A` | Nested cards, table rows, dropdowns |
| **Surface 3** | `#262635` | Hover states on surfaces |

### Glass System

| Role | Value | Usage |
|------|-------|-------|
| **Glass fill** | `rgba(255,255,255,0.03)` | Default glass panel background |
| **Glass fill hover** | `rgba(255,255,255,0.05)` | Hovered glass panel |
| **Glass border** | `rgba(255,255,255,0.06)` | Subtle edge on glass panels |
| **Glass border hover** | `rgba(255,255,255,0.10)` | Hovered edge |
| **Backdrop blur** | `blur(20px)` | Standard glass blur |
| **Red glow** | `0 0 80px rgba(229,62,62,0.08)` | Ambient red glow on auth cards |

### Text

| Role | Hex | Usage |
|------|-----|-------|
| **Text primary** | `#F5F5F7` | Headings, body text |
| **Text secondary** | `#8B8B9E` | Labels, descriptions, hints |
| **Text muted** | `#5A5A6E` | Disabled text, placeholders |
| **Text on primary** | `#FFFFFF` | Text on red buttons |

### Semantic Colors

| Role | Hex | Usage |
|------|-----|-------|
| **Success** | `#38A169` | Active, healthy, enabled, pass |
| **Warning** | `#D69E2E` | Locked, caution, expiring, partial |
| **Danger** | `#E53E3E` | Critical, failed, destructive (same as primary) |
| **Info** | `#4299E1` | Informational, pending, in-progress |

### Severity Scale (CVE / Vulnerabilities)

| Severity | Hex | Badge |
|----------|-----|-------|
| CRITICAL | `#E53E3E` | Red — matches brand |
| HIGH | `#ED8936` | Orange |
| MEDIUM | `#D69E2E` | Amber |
| LOW | `#4299E1` | Blue |
| NONE | `#5A5A6E` | Muted |

---

## Typography

### Font Stack

| Role | Font | Fallback |
|------|------|----------|
| **Primary** | Inter | system-ui, sans-serif |
| **Monospace** | JetBrains Mono | ui-monospace, monospace |

Monospace is used for: IP addresses, CVE IDs, hashes, API keys, timestamps, OTP codes.

### Type Scale

| Token | Size | Weight | Line Height | Usage |
|-------|------|--------|-------------|-------|
| `heading-1` | 32px | 600 | 1.2 | Page titles |
| `heading-2` | 24px | 600 | 1.3 | Section titles |
| `heading-3` | 20px | 600 | 1.4 | Card titles |
| `heading-4` | 16px | 600 | 1.5 | Subsections |
| `body-lg` | 16px | 400 | 1.6 | Intro text, descriptions |
| `body` | 14px | 400 | 1.5 | Default body text |
| `body-sm` | 13px | 400 | 1.5 | Helper text, captions |
| `caption` | 12px | 400 | 1.4 | Table cells, badges, timestamps |
| `mono` | 13px | 400 | 1.5 | Code, IPs, IDs |

### Weights

- **400 Regular** — body text, labels
- **500 Medium** — emphasis, navigation items
- **600 Semibold** — headings, buttons, badges

---

## Spacing & Layout

### Spacing Scale (based on 4px grid)

| Token | Value |
|-------|-------|
| `space-1` | 4px |
| `space-2` | 8px |
| `space-3` | 12px |
| `space-4` | 16px |
| `space-5` | 20px |
| `space-6` | 24px |
| `space-8` | 32px |
| `space-10` | 40px |
| `space-12` | 48px |
| `space-16` | 64px |

### Border Radius

| Token | Value | Usage |
|-------|-------|-------|
| `radius-sm` | 6px | Badges, small elements |
| `radius-md` | 8px | Inputs, buttons |
| `radius-lg` | 12px | Cards, panels |
| `radius-xl` | 16px | Auth cards, modals |
| `radius-full` | 9999px | Pills, avatars |

---

## Glass System

Glass is the core visual language of Reconova — representing **clarity, transparency, and seeing through layers** — a natural metaphor for a security/surveillance platform.

### Glass Card (Default)

```css
.glass-card {
  background: rgba(255, 255, 255, 0.03);
  backdrop-filter: blur(20px);
  -webkit-backdrop-filter: blur(20px);
  border: 1px solid rgba(255, 255, 255, 0.06);
  border-radius: 12px;
  box-shadow: 0 8px 32px rgba(0, 0, 0, 0.3);
}
```

### Glass Card — Auth Pages (with red glow)

```css
.glass-card-auth {
  background: rgba(255, 255, 255, 0.03);
  backdrop-filter: blur(24px);
  -webkit-backdrop-filter: blur(24px);
  border: 1px solid rgba(255, 255, 255, 0.08);
  border-radius: 16px;
  box-shadow:
    0 0 80px rgba(229, 62, 62, 0.08),
    0 0 160px rgba(229, 62, 62, 0.04),
    0 8px 32px rgba(0, 0, 0, 0.4);
}
```

### Glass Sidebar

```css
.glass-sidebar {
  background: rgba(255, 255, 255, 0.04);
  backdrop-filter: blur(16px);
  border-right: 1px solid rgba(255, 255, 255, 0.06);
}
```

### Auth Page Background — Rotating Glow

The red ambient glow **rotates clockwise** as users progress through the auth flow. This creates a subtle sense of forward movement — same card, same glass, just the light source shifts.

| Screen | Glow Position | CSS `radial-gradient` anchor |
|--------|--------------|------------------------------|
| Login | Upper-left | `at 20% 30%` |
| Register Step 1 | Bottom-left | `at 20% 80%` |
| Register Step 2 | Right | `at 80% 50%` |
| 2FA Setup | Upper-right | `at 80% 20%` |
| 2FA Verify | Top-center | `at 50% 20%` |
| Change Password | Bottom-right | `at 80% 80%` |
| Forgot Password | Left-center | `at 15% 50%` |

```css
/* Login — glow at upper-left (0° base) */
.auth-bg-login {
  background: #0C0C10;
  background-image:
    radial-gradient(ellipse at 20% 30%, rgba(229, 62, 62, 0.08) 0%, transparent 50%),
    radial-gradient(ellipse at 70% 70%, rgba(197, 48, 48, 0.04) 0%, transparent 50%);
}

/* Register Step 1 — glow rotated ~120° to bottom-left */
.auth-bg-register-1 {
  background: #0C0C10;
  background-image:
    radial-gradient(ellipse at 20% 80%, rgba(229, 62, 62, 0.08) 0%, transparent 50%),
    radial-gradient(ellipse at 70% 20%, rgba(197, 48, 48, 0.04) 0%, transparent 50%);
}

/* Register Step 2 — glow rotated ~240° to right */
.auth-bg-register-2 {
  background: #0C0C10;
  background-image:
    radial-gradient(ellipse at 80% 50%, rgba(229, 62, 62, 0.08) 0%, transparent 50%),
    radial-gradient(ellipse at 20% 50%, rgba(197, 48, 48, 0.04) 0%, transparent 50%);
}
```

---

## Components

### Buttons

| Variant | Style |
|---------|-------|
| **Primary** | `background: linear-gradient(135deg, #E53E3E, #C53030)` — white text, red glow on hover |
| **Secondary** | Glass background, `#F5F5F7` text, glass border |
| **Ghost** | Transparent, `#8B8B9E` text, no border |
| **Destructive** | Solid `#E53E3E` background (same as primary for this brand) |
| **Outline** | Transparent, `1px solid rgba(255,255,255,0.12)`, light text |

All buttons: `border-radius: 8px`, `font-weight: 600`, `padding: 10px 20px`

### Inputs

```css
.input {
  background: #161620;
  border: 1px solid rgba(255, 255, 255, 0.08);
  border-radius: 8px;
  color: #F5F5F7;
  padding: 10px 14px;
  font-size: 14px;
}

.input:focus {
  border-color: #E53E3E;
  box-shadow: 0 0 0 3px rgba(229, 62, 62, 0.15);
}

.input::placeholder {
  color: #5A5A6E;
}
```

### Auth Page Inputs (glass variant)

```css
.input-glass {
  background: rgba(255, 255, 255, 0.04);
  border: 1px solid rgba(255, 255, 255, 0.08);
  border-radius: 8px;
  color: #F5F5F7;
  backdrop-filter: blur(8px);
}

.input-glass:focus {
  border-color: rgba(229, 62, 62, 0.5);
  box-shadow: 0 0 0 3px rgba(229, 62, 62, 0.1);
}
```

### Status Badges

Color is never the only indicator — always include a dot + text label.

```css
.badge {
  display: inline-flex;
  align-items: center;
  gap: 6px;
  padding: 4px 10px;
  border-radius: 9999px;
  font-size: 12px;
  font-weight: 500;
}

/* Variants */
.badge-active    { background: rgba(56,161,105,0.12); color: #38A169; }
.badge-locked    { background: rgba(214,158,46,0.12); color: #D69E2E; }
.badge-expired   { background: rgba(214,158,46,0.12); color: #D69E2E; }
.badge-pending   { background: rgba(66,153,225,0.12); color: #4299E1; }
.badge-deactivated { background: rgba(229,62,62,0.12); color: #E53E3E; }
```

### Alert Banners

```css
.alert {
  padding: 12px 16px;
  border-radius: 8px;
  font-size: 14px;
  display: flex;
  align-items: center;
  gap: 10px;
}

.alert-info    { background: rgba(66,153,225,0.1); border: 1px solid rgba(66,153,225,0.2); color: #4299E1; }
.alert-warning { background: rgba(214,158,46,0.1); border: 1px solid rgba(214,158,46,0.2); color: #D69E2E; }
.alert-error   { background: rgba(229,62,62,0.1); border: 1px solid rgba(229,62,62,0.2); color: #E53E3E; }
.alert-success { background: rgba(56,161,105,0.1); border: 1px solid rgba(56,161,105,0.2); color: #38A169; }
```

### Sidebar Navigation

```css
.sidebar-item {
  padding: 10px 16px;
  border-radius: 8px;
  color: #8B8B9E;
  font-size: 14px;
  font-weight: 500;
  transition: all 0.15s;
}

.sidebar-item:hover {
  background: rgba(255, 255, 255, 0.04);
  color: #F5F5F7;
}

.sidebar-item.active {
  background: rgba(229, 62, 62, 0.08);
  color: #F5F5F7;
  border-left: 2px solid #E53E3E;
}
```

### Data Tables

```css
.table-row {
  border-bottom: 1px solid rgba(255, 255, 255, 0.04);
}

.table-row:hover {
  background: rgba(255, 255, 255, 0.02);
}

.table-header {
  color: #5A5A6E;
  font-size: 12px;
  font-weight: 600;
  text-transform: uppercase;
  letter-spacing: 0.05em;
}
```

### OTP Input (2FA)

```css
.otp-box {
  width: 48px;
  height: 56px;
  text-align: center;
  font-family: 'JetBrains Mono', monospace;
  font-size: 24px;
  font-weight: 600;
  color: #F5F5F7;
  background: rgba(255, 255, 255, 0.04);
  border: 1px solid rgba(255, 255, 255, 0.08);
  border-radius: 8px;
}

.otp-box:focus {
  border-color: #E53E3E;
  box-shadow: 0 0 0 3px rgba(229, 62, 62, 0.15);
}
```

### Password Strength Bar

```css
.strength-bar { background: #1E1E2A; height: 4px; border-radius: 2px; }
.strength-weak { background: #E53E3E; width: 33%; }
.strength-good { background: #D69E2E; width: 66%; }
.strength-strong { background: #38A169; width: 100%; }
```

---

## Logo

**RECONOVA** in Inter Semibold (600), letter-spacing `0.08em`, uppercase.

Optional: small red shield icon or red accent bar to the left of the wordmark.

On dark backgrounds: white text with red accent.
On auth pages: subtle red glow behind the wordmark.

---

## Shadows

| Token | Value | Usage |
|-------|-------|-------|
| `shadow-sm` | `0 2px 8px rgba(0,0,0,0.2)` | Buttons, small elements |
| `shadow-md` | `0 8px 32px rgba(0,0,0,0.3)` | Cards, panels |
| `shadow-lg` | `0 16px 64px rgba(0,0,0,0.4)` | Modals, auth cards |
| `shadow-glow` | `0 0 80px rgba(229,62,62,0.08)` | Auth card red glow |
| `shadow-glow-strong` | `0 0 80px rgba(229,62,62,0.12), 0 0 160px rgba(229,62,62,0.06)` | Hero elements |

---

## Animation

- **Transitions:** 150ms ease for interactive states (hover, focus)
- **Glass shimmer:** Optional subtle gradient animation on auth page background
- **Glow pulse:** Optional slow pulse on auth card glow (2s ease-in-out infinite)
- **Prefer reduced motion:** Respect `prefers-reduced-motion` — disable all animations

---

## Accessibility

- All text meets **WCAG AA** contrast ratio (4.5:1 minimum)
- Color is **never the only indicator** — always include text labels with status badges
- Focus rings use red primary with sufficient contrast
- Inputs have visible labels (not just placeholders)
- Interactive elements have minimum 44x44px touch targets
- Dark theme is the default; light theme may be added later

---

## Design Tokens Summary (Tailwind Config)

```js
// tailwind.config.js — theme.extend
{
  colors: {
    bg: { DEFAULT: '#0C0C10', surface: '#161620', surface2: '#1E1E2A', surface3: '#262635' },
    brand: { DEFAULT: '#E53E3E', light: '#FC5C5C', dark: '#C53030', muted: 'rgba(229,62,62,0.15)' },
    text: { DEFAULT: '#F5F5F7', secondary: '#8B8B9E', muted: '#5A5A6E' },
    success: '#38A169',
    warning: '#D69E2E',
    danger: '#E53E3E',
    info: '#4299E1',
  },
  fontFamily: {
    sans: ['Inter', 'system-ui', 'sans-serif'],
    mono: ['JetBrains Mono', 'ui-monospace', 'monospace'],
  },
  borderRadius: {
    sm: '6px', md: '8px', lg: '12px', xl: '16px',
  },
}
```

---

**Document Version:** 1.0
**Last Updated:** March 2026
**Theme:** Dark-first, Red Hot, Glassmorphism
