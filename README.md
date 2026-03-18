# Unify 2 Min

https://github.com/cat-yummi/DSP-Unify2Min

Unify belt and inserter rate displays to "/ min" (per minute) to match the unit used in assemblers, miners, and other facilities.

## Features

Converts the following displays from "per second" to "/ min":

1. **Belts**: 
   - Item tooltip hover: "x/s" → "x / min"
   - Belt window (when clicked): "x items/s" → "x / min"

2. **Inserters**:
   - Item tooltip hover:
     - Regular inserters (Grade 1-3): "x round-trips/s/grid" → "x / min/grid"
     - Container inserter (Grade 4, maxed): "120 items/s" → "7200 / min"
     - Container inserter (Grade 4, not maxed): keeps original display
   - Inserter window (when clicked): "x round-trips/s" → "x / min"
   - Build preview (when placing): "x round-trips/s" → "x / min"
     - Container inserter (maxed): always shows "7200 / min"
     - Container inserter (not maxed): keeps original display

All values are calculated as: original_value × 60

## Recommended Companion Mod

**[Assembler UI]** - This mod displays material consumption and product output rates, also using "/ min" as the unit. Using both mods together provides a consistent experience across all rate displays in the game.

## Installation

Place `Unify2Min.dll` in `BepInEx/plugins`.

## Requirements

- BepInEx 5.4.x

---

将游戏中传送带与分拣器的速率显示统一为「/ min」（每分钟），与制造台、矿机等设施保持一致。

## 功能

将以下几处原本以「每秒」显示的数值转换为「/ min」：

1. **传送带**：
   - 悬停物品提示：「x/s」→「x / min」
   - 点开详情窗口：「x 货物每秒」→「x / min」

2. **分拣器**：
   - 悬停物品提示：
     - 普通分拣器（1-3级）：「x 往返/秒/格」→「x / min/格」
     - 集装分拣器（4级，满级）：「120 每秒运送货物」→「7200 / min」
     - 集装分拣器（4级，未满级）：保持原版显示
   - 点开详情窗口：「x 往返/s」→「x / min」
   - 建造预览（放置时）：「x 往返/s」→「x / min」
     - 集装分拣器（满级）：固定显示「7200 / min」
     - 集装分拣器（未满级）：保持原版显示

所有数值计算方式：原值 × 60

## 推荐搭配 Mod

**[制造台界面优化 (Assembler UI)]** - 该 mod 可显示制造台的原料消耗和产品产出速率，单位同样为「/ min」。两个 mod 一起使用可以在游戏中获得统一的速率显示体验。

## 安装

将 `Unify2Min.dll` 放入 `BepInEx/plugins`。

## 依赖

- BepInEx 5.4.x
