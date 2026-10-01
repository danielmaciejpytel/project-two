# Gameplay HUD graphics

Every graphic of the approved gameplay mockup (artifact version 13) in its own file: **66 elements**, each as an SVG and as a high resolution PNG.

- `Svg/<Category>/*.svg`: vector sources (sizes are units of the 1920x1080 layout).
- `Png/<Category>/*.png`: export at **2x**, which is one pixel per pixel on a 4K monitor (3840x2160); on Full HD it is an exact 2:1 downscale.
- `Overview.png`: every element on one sheet.
- `Scripts/`: generator (`BuildSvgs.py`), PNG renderer (`RenderPngs.py`, headless Edge) and this document's generator (`MakeDocs.py`).

Rules: **there is no text in the graphics** (all text is TextMeshPro, font JustinFont11Bold), no element is cut or sliced (no 9-slice), and elements are layered when needed (track and fill of the time bar, card frame and the card art from the game). Flat rectangles (badges, key, icon frame) stretch to the text without distortion because they have no slanted corners.

Not drawn here: unit cards, the board and the characters (game art).

Colors: red `#ff1b47` (dark `#641020`), blue `#14c8d8` (dark `#0a5861`), accent `#d72e66`, warning `#ff4d4d`, healing `#c9f2d4`, panel `#121212` at 86% opacity.

## Regeneration

```bash
python Scripts/BuildSvgs.py     # SVG + Manifest.json
python Scripts/RenderPngs.py    # PNG (headless Edge, about 2 s)
python Scripts/MakeDocs.py      # Overview.png and this README
```

The PNG scale is the `SCALE` constant in `BuildSvgs.py`. The tag icons are traced from the 42x42 originals of the game (`Assets/Sprites/ChoiceScreen/Tag*.png`).

## Files

### Bars

| File | SVG (layout px) | PNG (px) | Use |
|---|---|---|---|
| `BarBodyRed` | 398x96 | 796x192 | Body of the commander bar of the red team (398x96), at the left edge of the bar. Pieces of the bar: the game hides stripes one by one. |
| `BarBodyBlue` | 398x96 | 796x192 | Body of the commander bar of the blue team (398x96), at the right edge of the bar (x222). |
| `BarStripeRed` | 57x96 | 114x192 | One stripe of the red bar (57x96): one living member of the team. Five of them at x372, 403, 432, 463, 494; the farthest from the name goes first. |
| `BarStripeBlue` | 57x96 | 114x192 | One stripe of the blue bar (57x96), mirrored: x191, 160, 131, 100, 69. |
| `BarCommanderRed` | 620x96 | 1240x192 | The whole commander bar of the red team in one piece (reference; the game builds it from body and stripes). Top left corner, x24 y24. |
| `BarCommanderBlue` | 620x96 | 1240x192 | The whole commander bar of the blue team in one piece (reference). Top right corner, x1276 y24, mirrored. |
| `PipCommanderOn` | 14x14 | 28x28 | Commander health point, full (white). 14 px, gap 6 px, the first at x+20 y+68. |
| `PipCommanderOff` | 14x14 | 28x28 | Commander health point, lost. |
| `TimerBarTrack` | 488x8 | 976x16 | Track of the time bar in the turn banner (x+26 y+92 inside the banner). |
| `TimerBarFill` | 488x8 | 976x16 | Fill of the time bar (Image Filled, horizontal). Tinted with the team color, below 10 s with #ff4d4d. |

### Panels

| File | SVG (layout px) | PNG (px) | Use |
|---|---|---|---|
| `PanelBanner` | 540x112 | 1080x224 | Turn banner (x690 y24): turn number, player name, seconds, time bar. |
| `PanelHint` | 800x72 | 1600x144 | Hint panel (x560 y148). |
| `PanelLog` | 384x216 | 768x432 | Battle log in the command column (y212), full variant. |
| `PanelLogShort` | 384x140 | 768x280 | Battle log when the ability button sits above it (y288). |
| `PanelLogCollapsed` | 384x44 | 768x88 | Battle log collapsed to its title (the Hide/Show button). |
| `PanelSettings` | 840x500 | 1680x1000 | Settings panel of the pause menu and the end screen (840x500, middle of the screen). |
| `PanelConfirm` | 840x280 | 1680x560 | The question "Are you sure?" of the pause menu (840x280). |
| `PanelCallout` | 340x126 | 680x252 | Damage preview next to an enemy in range (x944 y404, 340x126). |
| `PanelWinner` | 760x112 | 1520x224 | Winner banner of the end screen (x580 y190). |
| `PanelSummary` | 760x240 | 1520x480 | Game summary of the end screen (x580 y318). |
| `PanelInfo` | 872x208 | 1744x416 | Unit and tile details (bottom left corner, x24 y848), the slant is parallel to the board edge. |
| `PanelTeam` | 872x208 | 1744x416 | Team panel with the cards (bottom right corner, x1024 y848), mirrored slant. |

### Buttons

| File | SVG (layout px) | PNG (px) | Use |
|---|---|---|---|
| `ButtonPrimaryRed` | 244x64 | 488x128 | Main button (Surrender) in the red team turn, 244x64. |
| `ButtonPrimaryBlue` | 244x64 | 488x128 | Main button (Surrender) in the blue team turn. |
| `ButtonCall` | 128x64 | 256x128 | Call button (128x64). |
| `ButtonCallActive` | 128x64 | 256x128 | Call button in the call mode (Cancel), pink edge. |
| `ButtonAbility` | 384x64 | 768x128 | Unit ability button (Q), 384x64, under the row of the main buttons. |

### Pause menu and end screen

| File | SVG (layout px) | PNG (px) | Use |
|---|---|---|---|
| `ButtonMenu` | 372x72 | 744x144 | Button of the pause menu and the end screen (372x72). |
| `ButtonMenuSelected` | 372x72 | 744x144 | Selected or first button, pink edge. |
| `ButtonMenuWide` | 760x72 | 1520x144 | Wide menu button of the settings rows: resolution and difficulty (760x72). |
| `SliderTrack` | 460x8 | 920x16 | Track of a settings slider (460x8). |
| `SliderFill` | 460x8 | 920x16 | Fill of a settings slider, white, tinted with the accent color. |
| `SliderHandle` | 16x32 | 32x64 | Handle of a settings slider (16x32). |
| `ScrollbarTrack` | 8x480 | 16x960 | Track of the vertical scrollbar (8x480), flat, stretches to the length of the view. |
| `ScrollbarHandle` | 16x64 | 32x128 | Handle of the vertical scrollbar (16x64), white, stretches to its length. |
| `OverlayDim` | 1920x1080 | 3840x2160 | Full screen dimming under the pause menu and the end screen. |

### Team cards (frames and health points)

| File | SVG (layout px) | PNG (px) | Use |
|---|---|---|---|
| `SlotFrame` | 90x114 | 180x228 | Frame of a team card (90x114), normal and already moved. |
| `SlotFrameSelectedRed` | 90x114 | 180x228 | Frame of the selected card or the card to call, red team. |
| `SlotFrameSelectedBlue` | 90x114 | 180x228 | Frame of the selected card or the card to call, blue team. |
| `SlotFrameReserve` | 90x114 | 180x228 | Frame of the reserve (dashed, empty slot). |
| `SlotCrossOut` | 90x114 | 180x228 | Strike over the card of a killed unit: one dashed line from the bottom left to the top right (90x114), in the color of the dashed frame of the reserve (white, 24%); the card under it is greyed out like the reserve. |
| `PipSmallRed` | 5x5 | 10x10 | Health point under a card, red (5 px, gap 2 px). |
| `PipSmallBlue` | 5x5 | 10x10 | Health point under a card, blue. |
| `PipSmallOff` | 5x5 | 10x10 | Health point under a card, lost. |

### Small elements

| File | SVG (layout px) | PNG (px) | Use |
|---|---|---|---|
| `BadgeAccent` | 64x28 | 128x56 | Flat badge background: Hint, NEW (stretched to the text). |
| `BadgeMuted` | 64x28 | 128x56 | Flat badge background: MOVED. |
| `Keycap` | 64x28 | 128x56 | Key (Space, C, Esc), text #111111, stretched to the text. |
| `IconFrame` | 36x36 | 72x72 | Background of a tag icon in the info panel (36x36) and in the damage preview (22x22). |
| `LogTickRed` | 4x18 | 8x36 | Battle log entry marker, red. |
| `LogTickBlue` | 4x18 | 8x36 | Battle log entry marker, blue. |
| `DividerLine` | 360x1 | 720x2 | Turn header line in the battle log (stretched). |

### Tag icons

| File | SVG (layout px) | PNG (px) | Use |
|---|---|---|---|
| `IconTagBackstab` | 22x22 | 44x44 | Tag icon: Backstab (22 px in the layout). |
| `IconTagBinary` | 22x22 | 44x44 | Tag icon: Binary (22 px in the layout). |
| `IconTagCaller` | 22x22 | 44x44 | Tag icon: Caller (22 px in the layout). |
| `IconTagConfuser` | 22x22 | 44x44 | Tag icon: Confuser (22 px in the layout). |
| `IconTagGunman` | 22x22 | 44x44 | Tag icon: Gunman (22 px in the layout). |
| `IconTagImpair` | 22x22 | 44x44 | Tag icon: Impair (22 px in the layout). |
| `IconTagImpale` | 22x22 | 44x44 | Tag icon: Impale (22 px in the layout). |
| `IconTagPiercing` | 22x22 | 44x44 | Tag icon: Piercing (22 px in the layout). |
| `IconTagProvoke` | 22x22 | 44x44 | Tag icon: Provoke (22 px in the layout). |
| `IconTagRecovery` | 22x22 | 44x44 | Tag icon: Recovery (22 px in the layout). |
| `IconTagSpy` | 22x22 | 44x44 | Tag icon: Spy (22 px in the layout). |
| `IconTagSwift` | 22x22 | 44x44 | Tag icon: Swift (22 px in the layout). |
| `IconTagTough` | 22x22 | 44x44 | Tag icon: Tough (22 px in the layout). |
| `IconTagTeleporter` | 22x22 | 44x44 | Tag icon: Teleporter (22 px in the layout). |
| `IconTagZeal` | 22x22 | 44x44 | Tag icon: Zeal (22 px in the layout). |
