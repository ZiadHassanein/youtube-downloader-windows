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
rounded:
  control: "8px"
  control-inset: "7px"
  duration-badge: "5px"
  count-badge: "10px"
spacing:
  menu-inset: "4px"
  row: "8px"
  control-gap: "10px"
  compact: "12px"
  group: "16px"
  section: "20px"
  page-inset: "24px"
  form-inset: "30px"
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
---

# Design System: Vydrop

## Overview

**Creative North Star: "The Red Download Mark"**

Vydrop makes the next download decision clear. Its approved identity pairs a rounded red arrow and receiving tray with a compact vector wordmark: a larger bespoke red V with an angled right terminal, followed by slim Inter Regular outlines in theme Ink. Light rose neutrals or charcoal working surfaces keep links, real media details and controls readable. The red family connects identity, primary actions and selection without replacing the separate success, information and error roles.

The established interface is restrained, compact and bilingual. Preserve its native Windows controls, embedded lettering, visible keyboard focus and quiet separators. This document records the user-approved Vydrop rebrand of 16 October 2026; it supersedes historical ViFree lettering, play marks and teal/amber/blue brand directions. It describes implemented source, not a certification of every workflow or machine.

**Key Characteristics:**

- One flat red symbol and one fixed vector wordmark across desktop and companion.
- Shared neutral ground for form and media context, with tonal control faces.
- English and Arabic task content with stable physical header and pane anchors.
- Short, silent feedback tied to intent or real work; immediate settled states when Windows disables animation.

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

## Typography

**Body and interface font:** embedded Inter for Latin text and embedded Noto Sans Arabic for Arabic. The active interface font changes with language; URLs and technical file values retain literal LTR presentation and the Latin font where implemented. The companion popup uses the platform system sans-serif for body copy.

The hierarchy is compact, with semibold task headings and ordinary body copy. Body lines use explicit shared metrics so switching language does not change form rhythm. The frontmatter's `px` values represent WPF device-independent units at 96 DPI; native rendering scales with system DPI.

- **Headline:** New download.
- **Title / title-compact:** inspected media title, bounded to three wide lines or two compact lines with a full-title tooltip.
- **Body / body-arabic:** main task copy, field values and notices.
- **Label:** section labels and selected control emphasis.
- **Download-label:** the main action's distinct, slightly larger label.
- **Queue-heading / queue-body:** the queue's local compact density; do not shrink other screens to match it.

**The Lettering Rule.** The Vydrop wordmark uses `BrandInitialGeometry` for its larger bespoke red V with an angled right terminal and `BrandLettersGeometry` for the licensed Inter Regular “ydrop” outlines in theme Ink. The V is 28 source units tall, deliberately larger than the remaining letters' approximately 21.8-unit ascender height, while their baseline stays aligned. It is not a live text label and must not be recreated by choosing another font or weight. Keep the Inter license with distributed assets.

## Layout

Retain the existing surface composition in the [main-window surface brief](.impeccable/surfaces/src-downloader-desktop-views-mainwindow-xaml.md). MainWindow starts at 740 × 800 DIP with a 660 × 500 minimum, bounded to the working area. Header/footer stay fixed; task bodies scroll. The centered form caps at 620 DIP and uses symmetric 30-DIP horizontal and 24-DIP vertical insets. Shared small gaps and larger section gaps come from the frontmatter's observed spacing vocabulary, not a newly imposed scale.

Physical layout stays LTR while task content follows the active language. The brand and Settings remain physically left; utility header actions and Contact remain right. Recognized media can expand a normal narrow window by up to 420 DIP while retaining the original form width. Split view requires room for the minimum form width and a 320-DIP context pane; MainWindow computes this from the client width and actual window chrome. The 1000-DIP constant is an automatic-expansion guard, not a universal CSS breakpoint. When panes cannot fit, media becomes a compact strip below the link and activity follows Download. Respect explicit resizing and the implemented restoration behavior.

The Download action is centered. QueueWindow retains its local 700 × 460 default, 580 × 360 minimum, fixed header/footer and scrolling body. Contextual menus center on their physical trigger, keep an eight-DIP client inset and six-DIP trigger gap, and choose the available above/below position. Dropdown lists reserve a constant 20-DIP outer gutter for the 16-DIP scrollbar and four-DIP clearance; mirror the gutter in Arabic without changing option width.

## Elevation & Depth

The interface uses tonal layering and thin borders rather than application-drawn shadows. Page remains continuous across form and media context; Surface and Field identify local groups and controls. Owned native windows and popup placement provide structural depth. No shared shadow token is defined by the inspected theme files. Keep the native window boundary and standard Windows behavior.

Feedback changes opacity, transforms or a clipped theme snapshot rather than layout dimensions. Hover fades in over 130 ms and out over 100 ms; a recognized media view reveals once over 180 ms. Theme switching uses a 260-ms opaque clipped snapshot so text remains legible during the transition. Logo scale and highlight settle after their finite 360/600-ms sequence. Progress interpolation, the download arrow, completion check and queue feedback remain finite and tied to actual state. Cubic EaseOut is the common easing. All effects are silent; reduced-motion and high-contrast paths settle immediately.

## Shapes

Controls, menus, notices and thumbnail clipping share gently rounded eight-DIP corners. Inner hover layers use seven-DIP corners to sit inside the one-DIP face border; duration and history badges retain their smaller local shapes. Line icons use rounded caps and joins, generally 18-DIP boxes with a 1.5-DIP stroke. Header icons keep their existing larger measurements.

The canonical download mark is the filled arrow/tray geometry in `Themes/Symbols.xaml`, with a slim 5.5-unit shaft and tray on its 64-unit canvas. This slimmer weight follows the user’s refinement; retain its rounded ends and the same silhouette. The desktop lockup uses a 30 × 34-DIP symbol box, an eight-DIP gap and a 90 × 30-DIP wordmark Viewbox containing a 104.066 × 34.104-unit canvas. The visual boxes still span 128 DIP together, inside the fixed-height brand button. Both scale uniformly. The companion repeats the two canonical wordmark paths beside its mark. Keep the entire lockup physically LTR in Arabic. Light/dark brand-logo PNG exports are 512 × 168 pixels, with SVG exports generated from the same compact geometry.

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

**The Stable Focus Rule.** The dedicated BrandButtonStyle has a fixed 42-DIP height and four-DIP horizontal padding. Its two-DIP Accent focus border is an overlay inside those fixed bounds, never part of measurement. Clicking, pressing or focusing the brand must not alter header height or move the content below it. Preserve the tonal hover, Selected press, disabled opacity and finite symbol feedback within that stable frame.

## Do's and Don'ts

### Do

- **Do** use semantic light/dark pairs and preserve Windows high-contrast substitution.
- **Do** keep real text labels, focus, checks and selection semantics alongside color.
- **Do** preserve the approved layout, type, control geometry and existing finite motion when extending the red identity.
- **Do** keep the Vydrop vector lockup LTR and scale its parts uniformly.
- **Do** preserve literal URLs and file paths while localizing interface copy.
- **Do** label synthetic fixture screenshots and retain asset provenance outside public product copy.

### Don't

- **Don't** restore historical ViFree lettering, the notched play mark or the superseded teal/amber/blue brand palette on current Vydrop surfaces.
- **Don't** replace the custom vector wordmark with ordinary text or use Action as a substitute for readable Accent details.
- **Don't** fabricate media thumbnails, metadata or success states.
- **Don't** add sound, looping decoration or motion that overrides Windows preferences.
- **Don't** change persisted compatibility identifiers merely to make internal names match the visible rebrand.

Source authority: `src/Downloader.Desktop/Themes/ThemeResources.cs`, `Controls.xaml`, `Workspace.xaml`, `Symbols.xaml`, the native component XAML/code-behind, and `browser/common/popup.css`. The companion sidecar contains metadata and illustrative HTML/CSS translations of these WPF components; native source remains authoritative for behavior and rendering.
