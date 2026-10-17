---
name: Vydrop
description: A direct, bilingual Windows media downloader with a red download mark and calm working surfaces.
colors:
  brand: "#E63D4D"
  page-light: "#F8F7F8"
  page-dark: "#181B20"
  surface-light: "#F1EDF0"
  surface-dark: "#20242A"
  field-light: "#FFFFFF"
  field-dark: "#282B32"
  ink-light: "#272329"
  ink-dark: "#F6F2F4"
  muted-light: "#655963"
  muted-dark: "#BEB2BB"
  accent-light: "#A91F33"
  accent-dark: "#FF8393"
  action-light: "#BA263A"
  action-dark: "#CB2D42"
  on-accent-light: "#FFFFFF"
  on-accent-dark: "#FFFFFF"
  border-light: "#8E7D87"
  border-dark: "#94828D"
  hover-light: "#F3E9ED"
  hover-dark: "#353039"
  selected-light: "#F8E3E9"
  selected-dark: "#3E2933"
  error-light: "#A12232"
  error-dark: "#FFB0B7"
  info-accent-light: "#355F8D"
  info-accent-dark: "#AACAF1"
  divider-light: "#DFD7DC"
  divider-dark: "#48414A"
  success-light: "#24643E"
  success-dark: "#85D1A2"
  web-page-light: "#faf7f8"
  web-page-dark: "#181b20"
  web-ink-light: "#272329"
  web-ink-dark: "#f6f2f4"
  web-muted-light: "#645963"
  web-muted-dark: "#c5b5c0"
  web-accent-light: "#a42237"
  web-accent-dark: "#ff8393"
  web-line-light: "#d7cdd3"
  web-line-dark: "#655660"
  web-surface-light: "#eee6eb"
  web-surface-dark: "#282b32"
typography:
  headline:
    fontFamily: "Inter"
    fontSize: "25px"
    fontWeight: 600
    lineHeight: "36px"
  title:
    fontFamily: "Inter"
    fontSize: "23px"
    fontWeight: 600
    lineHeight: "32px"
  title-compact:
    fontFamily: "Inter"
    fontSize: "16px"
    fontWeight: 600
    lineHeight: "22px"
  body:
    fontFamily: "Inter"
    fontSize: "14px"
    lineHeight: "22px"
  body-arabic:
    fontFamily: "Noto Sans Arabic"
    fontSize: "14px"
    lineHeight: "22px"
  label:
    fontFamily: "Inter"
    fontSize: "14px"
    fontWeight: 600
    lineHeight: "22px"
  download-label:
    fontFamily: "Inter"
    fontSize: "17px"
    fontWeight: 600
  queue-heading:
    fontFamily: "Inter"
    fontSize: "21px"
    fontWeight: 600
    lineHeight: "28px"
  queue-body:
    fontFamily: "Inter"
    fontSize: "13px"
    lineHeight: "20px"
  web-body:
    fontFamily: 'system-ui, -apple-system, "Segoe UI", sans-serif'
    fontSize: "1.0625rem"
    lineHeight: 1.75
  web-headline:
    fontFamily: 'system-ui, -apple-system, "Segoe UI", sans-serif'
    fontSize: "clamp(2rem, 4vw, 2.9rem)"
    lineHeight: 1.25
    letterSpacing: "-.025em"
  web-headline-narrow:
    fontFamily: 'system-ui, -apple-system, "Segoe UI", sans-serif'
    fontSize: "2rem"
    lineHeight: 1.25
  web-title:
    fontSize: "1.65rem"
    lineHeight: 1.25
  web-heading-small:
    fontSize: "1.18rem"
    lineHeight: 1.25
  web-navigation-table:
    fontSize: ".94rem"
    lineHeight: 1.75
  web-caption:
    fontSize: ".88rem"
    lineHeight: 1.75
  web-footer:
    fontSize: ".9rem"
    lineHeight: 1.75
  web-download:
    fontSize: "1.0625rem"
    fontWeight: 650
    lineHeight: 1.75
rounded:
  control: "8px"
  control-inset: "7px"
  duration-badge: "5px"
  count-badge: "10px"
  web-focus: "2px"
  web-code: "3px"
  web-download: "7px"
spacing:
  menu-inset: "4px"
  row: "8px"
  control-gap: "10px"
  compact: "12px"
  group: "16px"
  section: "20px"
  page-inset: "24px"
  form-inset: "30px"
  web-inset: "2rem"
  web-inset-narrow: "1.2rem"
  web-section: "3rem"
  web-block: "2rem"
  web-group: "1.5rem"
  web-heading-gap: "1rem"
  web-caption-gap: ".5rem"
components:
  button-primary:
    backgroundColor: "{colors.action-light}"
    textColor: "{colors.on-accent-light}"
    typography: "{typography.download-label}"
    rounded: "{rounded.control}"
    padding: "8px 16px"
    height: "52px"
    width: "280px"
  button-primary-dark:
    backgroundColor: "{colors.action-dark}"
    textColor: "{colors.on-accent-dark}"
    typography: "{typography.download-label}"
    rounded: "{rounded.control}"
    padding: "8px 16px"
    height: "52px"
    width: "280px"
  button-secondary:
    backgroundColor: "{colors.field-light}"
    textColor: "{colors.ink-light}"
    rounded: "{rounded.control}"
    padding: "8px 16px"
    height: "44px"
  button-quiet:
    backgroundColor: "transparent"
    textColor: "{colors.accent-light}"
    rounded: "{rounded.control}"
    padding: "8px 16px"
    height: "44px"
  input:
    backgroundColor: "{colors.field-light}"
    textColor: "{colors.ink-light}"
    typography: "{typography.body}"
    rounded: "{rounded.control}"
    padding: "5px 12px"
    height: "44px"
  format-selected:
    backgroundColor: "{colors.selected-light}"
    textColor: "{colors.accent-light}"
    typography: "{typography.label}"
    rounded: "{rounded.control}"
    padding: "8px 12px"
    height: "46px"
  language-menu:
    backgroundColor: "{colors.surface-light}"
    textColor: "{colors.ink-light}"
    rounded: "{rounded.control}"
    padding: "4px"
    width: "160px"
  media-surface:
    backgroundColor: "{colors.page-light}"
    textColor: "{colors.ink-light}"
    padding: "24px 30px"
  web-download:
    backgroundColor: "{colors.web-accent-light}"
    textColor: "{colors.web-page-light}"
    typography: "{typography.web-download}"
    rounded: "{rounded.web-download}"
    padding: ".65rem 1.15rem"
  web-download-dark:
    backgroundColor: "{colors.web-accent-dark}"
    textColor: "{colors.web-page-dark}"
    typography: "{typography.web-download}"
    rounded: "{rounded.web-download}"
    padding: ".65rem 1.15rem"
  web-navigation:
    textColor: "{colors.web-accent-light}"
    typography: "{typography.web-navigation-table}"
  web-table-heading:
    backgroundColor: "{colors.web-surface-light}"
    textColor: "{colors.web-ink-light}"
    typography: "{typography.web-navigation-table}"
    padding: ".8rem"
  web-inline-code:
    backgroundColor: "{colors.web-surface-light}"
    textColor: "{colors.web-ink-light}"
    rounded: "{rounded.web-code}"
    padding: ".12rem .3rem"
---

# Design System: Vydrop

## Overview

**Creative North Star: "The Red Download Mark"**

Vydrop makes the next download decision clear. Its selected identity is a single integrated angular wordmark: a red V with a download-arrow cutout, followed by geometric ydrop. The V alone identifies the app on the taskbar and browser toolbar. Light rose or charcoal working surfaces preserve the existing bilingual task layout and semantic colors.

The established interface is restrained, compact and bilingual. Preserve its native Windows controls, embedded lettering, visible keyboard focus and quiet separators. This document records the user-approved Vydrop rebrand of 16 October 2026; it supersedes historical ViFree lettering, play marks and teal/amber/blue brand directions. It describes implemented source, not a certification of every workflow or machine.

**Key Characteristics:**

- One flat red symbol and one fixed vector wordmark across desktop and companion.
- Shared neutral ground for form and media context, with tonal control faces.
- English and Arabic task content with stable physical header and pane anchors.
- Short, silent feedback tied to intent or real work; immediate settled states when Windows disables animation.

The documentation extends this identity with a web reading scale. Tokens prefixed `web-` belong to `docs/assets/site.css`; unprefixed tokens and the native-app contracts remain intact. The approved red V, solid white arrow and angular wordmark continue unchanged. The documentation's reading purpose and composition are recorded in the [docs surface brief](.impeccable/surfaces/docs.md).

## Colors

The palette combines crimson action faces, rose selection tints and warm neutral text with light or charcoal surfaces. The frontmatter records both theme values; the source names below map directly to `ThemeResources.Palette`.

The sidecar's eight-step OKLCH tonal ramps are synthesized display metadata: each holds the source token's hue and chroma while stepping lightness from 15% to 95%. They are not application theme tokens and do not alter the canonical hexadecimal palette above.

### Primary

- **Download Red — BrandFill:** the fixed identity mark. Keep its flat fill when generating canonical assets.
- **Action Crimson — Action:** solid Download and other primary action faces; pair with OnAccent.
- **Readable Red — Accent:** links, focus borders, selected labels and line icons. Its light and dark values differ from Action to suit these roles.
- **Rose Selection — Selected:** checked faces and selection feedback; combine with a border, checkmark or semibold label.

### Neutral

- **Page:** the shared form/media ground and main application background.
- **Surface:** restrained grouping, menu frames, notices and thumbnail placeholders.
- **Field:** editable inputs and ordinary button faces.
- **Ink / Muted:** primary content and supporting text respectively. The wordmark's “ydrop” outlines follow Ink; its custom V follows BrandFill.
- **Border / Divider:** interactive boundaries and quieter structural separators respectively.
- **Hover:** a tonal overlay on existing faces. Do not substitute a new saturated accent for this role.
- **OnAccent:** lettering over the filled primary action.

### Semantic roles

Success remains green; InfoAccent remains blue; Error remains an independent red role. Preserve their accompanying text and symbols. Windows high contrast replaces semantic brushes with system text, window and highlight brushes through `ThemeResources.Apply`; do not bypass those overrides for ordinary interface controls.

**The Role Rule.** BrandFill is the identity paint; Action is a filled control; Accent is a readable detail. Reuse the correct role rather than applying the logo red to every element.

### Documentation web roles

The web palette keeps a warm light page and a charcoal dark page, selected by `prefers-color-scheme`. The `web-page`, `web-ink`, `web-muted`, `web-accent`, `web-line` and `web-surface` pairs map directly to the stylesheet's six custom properties. Page and Ink carry long-form reading; Muted supports captions and the footer; Line separates the header, footer and table rows; Surface grounds table headings and code. Web Accent serves links, keyboard outlines and the app-download link, whose lettering uses Web Page. These CSS roles deliberately have their own values where the native palette differs; they do not replace native Action, Accent or Border.

## Typography

**Body and interface font:** embedded Inter for Latin text and embedded Noto Sans Arabic for Arabic. The active interface font changes with language; URLs and technical file values retain literal LTR presentation and the Latin font where implemented. The companion popup uses the platform system sans-serif for body copy.

The hierarchy is compact, with semibold task headings and ordinary body copy. Body lines use explicit shared metrics so switching language does not change form rhythm. The unprefixed frontmatter's `px` values represent WPF device-independent units at 96 DPI; native rendering scales with system DPI.

- **Headline:** New download.
- **Title / title-compact:** inspected media title, bounded to three wide lines or two compact lines with a full-title tooltip.
- **Body / body-arabic:** main task copy, field values and notices.
- **Label:** section labels and selected control emphasis.
- **Download-label:** the main action's distinct, slightly larger label.
- **Queue-heading / queue-body:** the queue's local compact density; do not shrink other screens to match it.

**The Lettering Rule.** Use the approved integrated angular wordmark: the symmetrical red V/down-arrow emblem is the first letter, followed by geometric ydrop in theme Ink. There is no separate symbol before the word. The V has equal mirrored arms, a centered download arrow and a sharp lower point. The download arrow inside the V is filled solid white, with no outline. Canonical contours are in assets/brand/wordmark.geometry.json, with the selected reference in assets/brand/approved-reference.png; scripts/draw-brand-lettering.ps1 applies them to Symbols.xaml. The 966 × 284-unit source fits a 128 × 38-DIP Viewbox inside the existing 42-DIP fixed-height button. Preserve physical LTR order in Arabic and the overlay focus ring. Light mode uses charcoal ydrop; dark mode uses light ydrop; BrandFill stays red. Taskbar, window, standalone app and extension icons contain only the V, from BrandMarkGeometry on its 64-unit canvas. Retain Inter for interface text and its existing license.

### Documentation web hierarchy

Documentation uses the local system sans-serif stack in `web-body`, with a generous unitless line height. Its `rem` and viewport units are browser CSS measurements. The fluid page heading uses `web-headline`; at the narrow breakpoint it uses `web-headline-narrow`. Section and question headings use `web-title` and `web-heading-small`; navigation and tables share `web-navigation-table`; captions and footer use their smaller dedicated roles. Headings keep the browser's bold weight and balanced wrapping. Arabic page headings reset letter spacing to normal. No external font is loaded, and this web scale does not change embedded Inter or Noto Sans Arabic in the native app.

**The Web Scale Rule.** Use the `web-` tokens for documentation CSS and the unprefixed tokens for native controls. A browser reading size or radius must not redefine a WPF DIP token.

## Layout

Retain the existing surface composition in the [main-window surface brief](.impeccable/surfaces/src-downloader-desktop-views-mainwindow-xaml.md). MainWindow starts at 740 × 800 DIP with a 660 × 500 minimum, bounded to the working area. Header/footer stay fixed; task bodies scroll. The centered form caps at 620 DIP and uses symmetric 30-DIP horizontal and 24-DIP vertical insets. Shared small gaps and larger section gaps come from the frontmatter's observed spacing vocabulary, not a newly imposed scale.

Physical layout stays LTR while task content follows the active language. The brand and Settings remain physically left; utility header actions and Contact remain right. Recognized media can expand a normal narrow window by up to 420 DIP while retaining the original form width. Split view requires room for the minimum form width and a 320-DIP context pane; MainWindow computes this from the client width and actual window chrome. The 1000-DIP constant is an automatic-expansion guard, not a universal CSS breakpoint. When panes cannot fit, media becomes a compact strip below the link and activity follows Download. Respect explicit resizing and the implemented restoration behavior.

The Download action is centered. QueueWindow retains its local 700 × 460 default, 580 × 360 minimum, fixed header/footer and scrolling body. Contextual menus center on their physical trigger, keep an eight-DIP client inset and six-DIP trigger gap, and choose the available above/below position. Dropdown lists reserve a constant 20-DIP outer gutter for the 16-DIP scrollbar and four-DIP clearance; mirror the gutter in Arabic without changing option width.

### Documentation web layout

Docs use one centered reading column capped at 78ch, with `web-inset` horizontal padding and top/bottom margins of three/five rem. The header and footer have a separate 1100px maximum, two-rem horizontal insets and one-and-a-half-rem vertical insets. Header navigation wraps. At 650px and below, the header stacks, page/header/footer insets become `web-inset-narrow`, and the article's top margin becomes two rem. The brand remains a 144 × 44px contained image and keeps physical LTR direction in either language.

English and Arabic pages derive both `lang` and page direction from the page's language in `docs/_layouts/default.html`. Text alignment follows the reading direction; inline code isolates literal LTR values. Comparison tables stay semantic tables inside an `overflow-x:auto` wrapper, containing narrow-screen overflow within the table rather than widening the page. Images stay within the reading column and preserve their aspect ratio.

## Elevation & Depth

The interface uses tonal layering and thin borders rather than application-drawn shadows. Page remains continuous across form and media context; Surface and Field identify local groups and controls. Owned native windows and popup placement provide structural depth. No shared shadow token is defined by the inspected theme files. Keep the native window boundary and standard Windows behavior.

Feedback changes opacity, transforms or a clipped theme snapshot rather than layout dimensions. Hover fades in over 130 ms and out over 100 ms; a recognized media view reveals once over 180 ms. Theme switching uses a 260-ms opaque clipped snapshot so text remains legible during the transition. Logo scale and highlight settle after their finite 360/600-ms sequence. Progress interpolation, the download arrow, completion check and queue feedback remain finite and tied to actual state. Cubic EaseOut is the common easing. All effects are silent; reduced-motion and high-contrast paths settle immediately.

Documentation stays flat: thin Web Line separators, Web Surface table headings and code, and no shadows or animation. Link hover increases underline thickness; the filled download link gains an underline. A two-pixel Web Accent outline with five-pixel offset marks keyboard focus. Forced colors adds a system ButtonText border to the download link.

## Shapes

Controls, menus, notices and thumbnail clipping share gently rounded eight-DIP corners. Inner hover layers use seven-DIP corners to sit inside the one-DIP face border; duration and history badges retain their smaller local shapes. Line icons use rounded caps and joins, generally 18-DIP boxes with a 1.5-DIP stroke. Header icons keep their existing larger measurements.

Use the approved integrated angular wordmark: the symmetrical red V/down-arrow emblem is the first letter, followed by geometric ydrop in theme Ink. There is no separate symbol before the word. The V has equal mirrored arms, a centered download arrow and a sharp lower point. The download arrow inside the V is filled solid white, with no outline. Canonical contours are in assets/brand/wordmark.geometry.json, with the selected reference in assets/brand/approved-reference.png; scripts/draw-brand-lettering.ps1 applies them to Symbols.xaml. The 966 × 284-unit source fits a 128 × 38-DIP Viewbox inside the existing 42-DIP fixed-height button. Preserve physical LTR order in Arabic and the overlay focus ring. Light mode uses charcoal ydrop; dark mode uses light ydrop; BrandFill stays red. Taskbar, window, standalone app and extension icons contain only the V, from BrandMarkGeometry on its 64-unit canvas. Retain Inter for interface text and its existing license. Transparent light/dark PNG and SVG exports, solid white/dark previews and seven native icon sizes are generated by scripts/render-brand.ps1. The companion popup uses the same wordmark contours; its video tab uses the V icon.

Documentation uses its own restrained CSS radii: `web-download` for the filled app-download link, `web-code` for inline code and `web-focus` for outlined focus targets. Tables and the main reading surface remain square. These CSS pixels do not redefine the native eight-DIP control shape.

## Components

### Buttons

Primary actions are clear, filled and semibold. Download uses the PrimaryButton face and its established centered 280 × 52-DIP action. Hover adds a subtle OnAccent overlay (opacity 0.08); focus and press use Ink on the boundary, with a two-DIP focus border. Disabled primary actions use opacity 0.5.

Ordinary buttons use Field, Ink and Border with the shared corner and inset values. Hover adds Hover; press uses Selected; keyboard focus increases the Accent boundary to two DIP. Quiet and toolbar actions preserve transparent resting faces. Most secondary controls have a 44-DIP minimum; toolbar and queue variants retain their local density. Disabled ordinary controls use opacity 0.55.

### Inputs / Fields

Text fields use Field, Ink, Border, Accent caret and Selected text selection. Their resting boundary is one DIP; focus changes its color to Accent without enlarging the field. Keep URL direction literal, field height fixed and accessibility names intact. The queue deliberately uses shorter local link editors.

### Selection controls and menus

Video and Audio share the same FormatSegment style: equal prominence, 46-DIP minimum, rounded outlined faces and distinct labeled icons. Checked state combines Selected, Accent and semibold text. Keyboard focus uses a two-DIP Accent boundary; hover preserves selection and adds a restrained tonal overlay.

The Arabic label above the audio-quality picker is **جودة الصوت**. The English label remains **Audio bitrate**; this localized copy refinement does not rename the English control or change its inspected-source and bitrate-selection behavior.

Language, output-format and quality choices use separate rounded button faces with a persistent selected boundary. Keep native MenuItem, ComboBox and radio semantics, keyboard navigation and dismissal. Checkmarks remain physically left in both languages while labels follow their own direction. The language menu is the quieter 160-DIP Surface/Divider frame with four-DIP padding, two equal 40-DIP rows and one four-DIP gap; Audio retains its own density. Preserve the dropdown scrollbar gutter.

### Cards / Containers

Media context is a flat Page surface with symmetric insets; it is not a raised contrasting card. Thumbnails preserve the actual image aspect and rounded clipping. Notices use restrained Surface/Border treatment with supporting text and relevant actions. Queue and history rows use separators, bounded text and full-value tooltips rather than decorative card stacks.

### Navigation and brand lockup

The header's fixed-left logo is an actual new-download button with an accessible action name. Recent downloads, language and theme are quiet utilities separated by thin Divider lines. Keep history counts legible as text in their Selected badge. The fixed footer keeps Settings left and Contact right. Do not reintroduce the removed visible author credit.

Use the same symbol and custom vector wordmark as a unified lockup in desktop and companion. BrandFill paints the mark and custom V; theme Ink paints the remaining letters. Asset regeneration must read the canonical geometry; do not hand-edit PNG/ICO exports into a separate identity.

**The Stable Focus Rule.** The dedicated BrandButtonStyle has a fixed 42-DIP height and four-DIP horizontal padding. Its two-DIP Accent focus border is an overlay inside those fixed bounds, never part of measurement. Clicking, pressing or focusing the brand must not alter header height or move the content below it. Keep its background transparent in every pointer state, preserve disabled opacity and use the finite whole-wordmark shine for hover feedback.

### Documentation navigation and download

The docs header pairs the approved theme-aware logo with links to supported sites, browser setup, GitHub and the alternate language. A keyboard-visible skip link reaches the semantic main/article. The home page's app-download CTA is an ordinary link to the latest GitHub release, using the web download tokens; it does not adopt the native button's fixed size. The localized footer carries authorship, MIT licensing and security/privacy access. The page works without client scripts, external fonts or tracking.

### Documentation tables and figures

Comparison tables retain header and cell semantics, start-aligned text, Web Line row separators, Web Surface headings and the contained overflow wrapper. Markdown tables must compile as tables inside that wrapper. Native app figures use cursor-free, 100% scale client-area renders of the 4.2.0 source UI, explicitly captioned as source renders. They are local-fixture empty-state evidence, not screenshots of the published binary or live media activity. Setup diagrams remain labelled illustrations. Preserve capture resolution and the provenance in `docs/images/capture-notes.json`.

## Do's and Don'ts

### Do

- **Do** use semantic light/dark pairs and preserve Windows high-contrast substitution.
- **Do** keep real text labels, focus, checks and selection semantics alongside color.
- **Do** preserve the approved layout, type, control geometry and existing finite motion when extending the red identity.
- **Do** keep the Vydrop vector lockup LTR and scale its parts uniformly.
- **Do** preserve literal URLs and file paths while localizing interface copy.
- **Do** label synthetic fixture screenshots and retain asset provenance outside public product copy.
- **Do** keep the documentation's `web-` CSS scale separate from native DIP measurements, derive reading direction from language, and contain table overflow.
- **Do** retain explicit source-render and illustration labels on documentation figures.

### Don't

- **Don't** restore historical ViFree lettering, the notched play mark or the superseded teal/amber/blue brand palette on current Vydrop surfaces.
- **Don't** replace the custom vector wordmark with ordinary text or use Action as a substitute for readable Accent details.
- **Don't** fabricate media thumbnails, metadata or success states.
- **Don't** add sound, looping decoration or motion that overrides Windows preferences.
- **Don't** change persisted compatibility identifiers merely to make internal names match the visible rebrand.
- **Don't** add external fonts, client scripts or tracking to the documentation reading surface.

Source authority: `src/Downloader.Desktop/Themes/ThemeResources.cs`, `Controls.xaml`, `Workspace.xaml`, `Symbols.xaml`, the native component XAML/code-behind, and `browser/common/popup.css`. The companion sidecar contains metadata and illustrative HTML/CSS translations of these WPF components; native source remains authoritative for behavior and rendering.

Documentation source authority: `docs/assets/site.css`, `docs/_layouts/default.html`, `docs/_includes/head.html` and their English/Arabic Markdown pages. Web component previews in the sidecar demonstrate this CSS independently of the native translations. The docs surface brief records the bounded review evidence and detector limitations.


Contact menu refinement authority: The footer contact popup uses a 208-DIP Surface/Divider frame with four-DIP padding and three 40-DIP Field rows separated by four DIP. Each row has a seven-DIP radius, a leading red line icon and a clear text label. GitHub uses a repository-branch icon, LinkedIn a profile monogram and email an envelope. Icons retain physical LTR orientation in Arabic; row order and label alignment follow the active language. Hover uses Hover, keyboard focus is a painted two-DIP Accent ring, and the Contact trigger uses Selected while its menu is open. Preserve the three existing commands, above-footer placement and native dismissal/focus behavior.

Header interaction refinement authority: Brand hover keeps the header transparent and plays one 600-ms shine across the full V, arrow and lettering. The effect is clipped to the glyphs, cancels on pointer exit or motion-preference changes and does not loop. Keep the native click action and keyboard focus overlay. ThemeButtonStyle is a fixed 42-by-42-DIP target with its 22-DIP icon centered; its two-DIP focus ring is paint over the fixed face, never measured border thickness. Focusing or switching themes must not move the header or content below it.
