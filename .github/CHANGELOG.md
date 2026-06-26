# v1.0.0 - クロスハッチ陰影 for YMM4

YukkuriMovieMaker4 向けのクロスハッチ陰影エフェクトプラグインの初回リリースです。
Direct2D カスタムピクセルシェーダーで入力映像の輝度と局所的な明暗勾配からクロスハッチ線を手続き的に生成し、ペン画・版画・漫画・設計図風の陰影へ変換する映像エフェクトプラグインです。
参照画像や外部モデルを使わず、暗い部分ほど線の層を段階的に増やし、流れの値に応じて線方向を明暗の勾配へ沿わせます。
輪郭と粒状感は線とは別の層として合成し、シードによって粒状感と線の位相を固定します。
8 言語対応 UI を備えます。

---

## 新機能

### 1. パラメーター正規化（ParameterNormalizer）

`internal static class ParameterNormalizer` は数値パラメーターのクランプと非有限値のフォールバックを提供します。

| メソッド | 説明 |
|---|---|
| `Finite(double, float, float, float)` | 値が有限なら [minimum, maximum] にクランプして float へ変換。非有限値（NaN・±∞）は fallback を返す |
| `Percent(double, float, float, float)` | `value / 100` を求め、`Finite` で [minimum, maximum] にクランプ |

`CrossHatchShadingEffectProcessor` は `using static` により両メソッドを参照します。

---

### 2. ピクセルシェーダー（CrossHatchShading.hlsl）

ピクセルシェーダー `main`（`ps_5_0`）はフレームごとに、中心画素とその周囲 8 画素の合計 9 サンプルから輝度・アルファの勾配を求め、スタイルプリセットと組み合わせてクロスハッチ線・輪郭・粒状感を合成します。ハッチ線そのものは手続き的に生成するため、追加のテクスチャを必要としません。`amount ≤ 0` または `source.a ≤ 0` のときはソースをそのまま返します。

#### 輝度と陰影量

各サンプルはプリマルチプライドの入力をストレート化（`rgb / max(a, 1e-5)`）し、Rec.709 係数 `(0.2126, 0.7152, 0.0722)` で輝度を求めます。陰影量は `shade = saturate((1 − luma) × shadowDepth)` です。周辺サンプルは入力矩形 `inputBounds` 内へクランプしてから参照します。

勾配は Scharr 係数（`kA = 3/16`・`kB = 10/16`）で輝度とアルファについて同時に計算します。

| 定数 | 値 | 用途 |
|---|---|---|
| `LumaWeight` | (0.2126, 0.7152, 0.0722) | Rec.709 輝度の重み |
| `GradientLow` | 0.015 | フロー適用を開始する勾配の下限 |
| `GradientHigh` | 0.150 | フロー適用が最大になる勾配の上限 |
| `EdgeThreshold` | 0.080 | 輪郭が現れ始めるしきい値 |
| `EdgeSoftness` | 0.140 | 輪郭境界の柔らかさ |
| `AlphaEdgeWeight` | 1.4 | 輪郭判定におけるアルファ勾配の重み |
| `PaperNoiseDepth` | 0.06 | 紙ノイズの振幅 |

#### スタイルプリセット

`GetStyle` は `style` の値に応じて、線の方向（4 層）・暗部しきい値（4 層）・粒状感倍率・輪郭倍率・交差濃度ブーストを切り替えます。

| スタイル | `style` | 線の方向 | 暗部しきい値 | 粒状感倍率 | 輪郭倍率 | 交差ブースト |
|---|---|---|---|---|---|---|
| ペン画 | 0 | -45° / 45° / 0° / 90° | 0.25 / 0.45 / 0.65 / 0.82 | 0.60 | 1.00 | 0.00 |
| 版画 | 1 | 20° / -20° / 70° / -70° | 0.18 / 0.36 / 0.56 / 0.76 | 1.00 | 0.60 | 0.35 |
| 漫画 | 2 | -45° / 45° / 0° / 90° | 0.30 / 0.50 / 0.70 / 0.86 | 0.50 | 1.40 | 0.00 |
| 設計図 | 3 | 0° / 90° / 45° / -45° | 0.22 / 0.44 / 0.66 / 0.84 | 0.35 | 1.40 | 0.00 |

#### 明暗勾配と線方向

輝度勾配の大きさ `lumaGradient` から接線方向 `flowTangent = normalize(float2(−gradY, gradX))` を求めます（勾配が `1e-5` 以下のときは固定方向 `(1, 0)`）。フローの重みは `flowWeight = flow × smoothstep(GradientLow, GradientHigh, lumaGradient)` です。

線層ごとに `0°`・`90°`・`45°`・`-45°` のオフセットを接線方向へ加え、固定方向との間を `normalize(lerp(fixedDir, flowDir, flowWeight))` で補間して各層の方向を決定します。これにより平坦部では固定方向、勾配のある部分では曲面に沿った交差ハッチが得られます。

#### 線層の生成

各層の線は、シードで決まるオフセット `seedOffset` を加えたパターン座標 `patternPosition = scene + seedOffset` を方向ベクトルへ射影して生成します。

| 項目 | 式 |
|---|---|
| 射影 | `projection = dot(patternPosition, direction) + Hash01(seed × 4 + i) × density` |
| 線間距離 | `distancePx = abs(frac(projection / density) − 0.5) × density` |
| アンチエイリアス | `aa = max(fwidth(projection), 1.0) × 0.5` |
| 線マスク | `lineMask = 1 − smoothstep(thickness/2 − aa, thickness/2 + aa, distancePx)` |
| 層の重み | `active ? smoothstep(threshold_i, threshold_i + toneSoftness, shade) : 0` |

`active` は `lineLayers == 0`（自動）または `i < lineLayers` のとき真です。各層の寄与 `lineMask × layerWeight` を `max` で合成し、版画のみ交差濃度ブーストを加えます（`hatch = saturate(hatchMax + crossBoost × max(hatchSum − hatchMax, 0))`）。

#### 輪郭と粒状感

粒状感は `seed` を含む `Hash32` ベースの `CellNoise` で決定論的に生成します。インクのかすれは `inkBreak = lerp(1, smoothstep(0.15, 0.95, noise), grain × grainScale)`、紙ノイズは `(noise − 0.5) × grain × grainScale × PaperNoiseDepth` です。

輪郭の強さ `edgeMagnitude = sqrt(lumaGradient² + alphaGradient² × AlphaEdgeWeight)` を求め、`edge = smoothstep(EdgeThreshold, EdgeThreshold + EdgeSoftness, edgeMagnitude) × saturate(outline × outlineScale)` で輪郭層を生成します。

#### 合成

最終インクマスクは `inkMask = saturate(max(hatch × inkBreak, edge))` です。色モードに応じて出力色を作り、`amount` で合成します。

| 色モード | `colorMode` | 出力色 |
|---|---|---|
| インクと紙 | 0 | `lerp(saturate(paperColor + paperNoise), inkColor, inkMask)` |
| 元の色を残す | 1 | `lerp(sourceRgb, inkColor, inkMask)` |

`outRgb = lerp(sourceRgb, styledRgb, amount)` を求め、入力アルファを保持して `float4(saturate(outRgb) × source.a, source.a)` をプリマルチプライドで出力します。

---

### 3. カスタムシェーダーエフェクト（CrossHatchShadingCustomEffect）

`internal sealed class CrossHatchShadingCustomEffect(IGraphicsDevicesAndContext) : D2D1CustomShaderEffectBase` は `[CustomEffect(1)]` の 1 入力エフェクトとして宣言されます（入力 0: ソース画像）。

公開プロパティは `GetFloatValue`・`GetIntValue`・`GetVector4Value`・`SetValue` を介して `EffectImpl` へ転送します。

| プロパティ | 型 | 範囲 |
|---|---|---|
| `Amount` | `float` | 0〜1 |
| `Density` | `float` | 3〜80 |
| `Thickness` | `float` | 0.2〜12 |
| `ShadowDepth` | `float` | 0〜2 |
| `Flow` | `float` | 0〜1 |
| `Outline` | `float` | 0〜1 |
| `ToneSoftness` | `float` | 0〜0.5 |
| `Grain` | `float` | 0〜1 |
| `Seed` | `int` | 0〜9999 |
| `Style` | `int` | 0〜3 |
| `ColorMode` | `int` | 0〜1 |
| `LineLayers` | `int` | 0〜4 |
| `InkColor` | `Vector4` | 各成分 0〜1 |
| `PaperColor` | `Vector4` | 各成分 0〜1 |

#### EffectImpl（内部 sealed クラス）

`float` プロパティの setter は `Clamp(value, minimum, maximum, fallback)`（非有限値は fallback）で値を制限し、`UpdateConstants` で定数バッファーを更新します。`int` プロパティは `Math.Clamp` で範囲内に制限し、`Vector4` プロパティは `Vector4.Clamp` で各成分を [0, 1] に制限します。

`ConstantBuffer` 構造体（`LayoutKind.Sequential`）のレイアウトは以下のとおりです。`int` 4 個と 3 個の `Vector4` を 16 バイト境界へ整合させています。

| フィールド | 型 | 説明 |
|---|---|---|
| `Amount` | `float` | 合成量 |
| `Density` | `float` | 密度 |
| `Thickness` | `float` | 太さ |
| `ShadowDepth` | `float` | 陰影 |
| `Flow` | `float` | 流れ |
| `Outline` | `float` | 輪郭 |
| `ToneSoftness` | `float` | 階調の柔らかさ |
| `Grain` | `float` | 粒状感 |
| `Seed` | `int` | シード |
| `Style` | `int` | スタイル |
| `ColorMode` | `int` | 色モード |
| `LineLayers` | `int` | 線の層 |
| `InkColor` | `Vector4` | インク色（RGBA） |
| `PaperColor` | `Vector4` | 紙色（RGBA） |
| `InputBounds` | `Vector4` | 入力矩形（Left, Top, Right, Bottom） |

`MapInputRectsToOutputRect` は入力 0 の矩形を `ClampInputRect` でクランプして出力矩形に設定し（拡張なし）、`InputBounds` を定数バッファーに書き込みます。`MapOutputRectToInputRects` は勾配サンプリングのために入力矩形を 2px だけ拡張します。

シェーダーリソース: `pack://application:,,,/CrossHatching;component/Shaders/CrossHatchShading.cso`（ps_5_0、`ShaderResourceUri.Get` が生成）

---

### 4. エフェクト定義（CrossHatchShadingEffect）

`public sealed class CrossHatchShadingEffect : VideoEffectBase` を継承します。

`[VideoEffect]` 属性は以下のパラメーターで宣言されます。

- 表示名：`Texts.EffectName`（ローカライズキー）
- カテゴリー：`VideoEffectCategories.Filtering`
- 検索タグ：`TagCrossHatch`・`TagHatching`・`TagPen`・`TagEngraving`・`TagBlueprint`・`TagManga`（「クロスハッチ」・「ハッチング」・「ペン画」・「版画」・「設計図」・「漫画」）
- `IsAviUtlSupported = false` により AviUtl 向け EXO 出力は非対応
- `ResourceType = typeof(Texts)` でローカライズリソースを指定

`Label` プロパティは `Texts.EffectName` を返します。

列挙型は以下のとおりで、整数値はそのままシェーダーへ渡されます。

| 列挙型 | 値 |
|---|---|
| `CrossHatchStyle` | `Pen = 0`・`Engraving = 1`・`Manga = 2`・`Blueprint = 3` |
| `CrossHatchColorMode` | `InkAndPaper = 0`・`PreserveSource = 1` |
| `CrossHatchLineLayers` | `Auto = 0`・`One = 1`・`Two = 2`・`Three = 3`・`Four = 4` |

公開プロパティは以下のとおりです（内部範囲は `Animation` の最小値・最大値）。

**基本グループ**

| プロパティ | 型 | デフォルト | 内部範囲 |
|---|---|---|---|
| `Style` | `CrossHatchStyle` | `Pen` | — |
| `Amount` | `Animation` | 100 | 0〜100 |
| `Density` | `Animation` | 12 | 3〜80 |
| `Thickness` | `Animation` | 1.2 | 0.2〜12 |
| `ShadowDepth` | `Animation` | 100 | 0〜200 |
| `Flow` | `Animation` | 40 | 0〜100 |
| `Outline` | `Animation` | 30 | 0〜100 |

**色グループ**

| プロパティ | 型 | デフォルト |
|---|---|---|
| `InkColor` | `Color` | `#FF111111` |
| `PaperColor` | `Color` | `#FFF6F1E7` |
| `ColorMode` | `CrossHatchColorMode` | `InkAndPaper` |

**詳細グループ**

| プロパティ | 型 | デフォルト | 内部範囲 |
|---|---|---|---|
| `LineLayers` | `CrossHatchLineLayers` | `Auto` | — |
| `ToneSoftness` | `Animation` | 12 | 0〜50 |
| `Grain` | `Animation` | 12 | 0〜100 |
| `Seed` | `int` | 0 | 0〜9999 |

`GetAnimatables` は `Amount`・`Density`・`Thickness`・`ShadowDepth`・`Flow`・`Outline`・`ToneSoftness`・`Grain` を返します（`Style`・`InkColor`・`PaperColor`・`ColorMode`・`LineLayers`・`Seed` はアニメーション対象外）。

`CreateExoVideoFilters` は空のシーケンスを返します（EXO 非対応）。`CreateVideoEffect` は `CrossHatchShadingEffectProcessor` を生成します。

---

### 5. エフェクトプロセッサー（CrossHatchShadingEffectProcessor）

`internal sealed class CrossHatchShadingEffectProcessor : VideoEffectProcessorBase` を継承します。

#### Update メソッド

`IsPassThroughEffect || effect is null` の場合は `effectDescription.DrawDescription` をそのまま返します。

各フレームで `ParameterNormalizer` を用いて以下の値を計算します。

| パラメーター | 変換 |
|---|---|
| `Amount` | `value / 100` を [0, 1] にクランプ（非有限値は 1） |
| `Density` | 有限値を [3, 80] にクランプ（非有限値は 12） |
| `Thickness` | 有限値を [0.2, 12] にクランプ（非有限値は 1.2） |
| `ShadowDepth` | `value / 100` を [0, 2] にクランプ（非有限値は 1） |
| `Flow` | `value / 100` を [0, 1] にクランプ（非有限値は 0.4） |
| `Outline` | `value / 100` を [0, 1] にクランプ（非有限値は 0.3） |
| `ToneSoftness` | `value / 100` を [0, 0.5] にクランプ（非有限値は 0.12） |
| `Grain` | `value / 100` を [0, 1] にクランプ（非有限値は 0.12） |
| `Seed` | `Math.Clamp(item.Seed, 0, 9999)` |
| `Style` | `(int)item.Style` |
| `ColorMode` | `(int)item.ColorMode` |
| `LineLayers` | `(int)item.LineLayers` |
| `InkColor` | `Color` を `Vector4(R/255, G/255, B/255, A/255)` へ変換 |
| `PaperColor` | `Color` を `Vector4(R/255, G/255, B/255, A/255)` へ変換 |

計算した値は `Parameters`（readonly record struct）にまとめ、`isFirst` または前フレームと値が異なる場合のみ各プロパティを `effect` へ転送します。

#### CreateEffect / setInput / ClearEffectChain

`CreateEffect` は `CrossHatchShadingCustomEffect` を生成し、`IsEnabled` が false の場合は破棄して `null` を返します。

`setInput` は `effect?.SetInput(0, input, true)` を呼び出します。

`ClearEffectChain` は `effect?.SetInput(0, null, true)` を呼び出し、`isFirst = true` にリセットします。

---

### 6. ローカライズ（Texts）

`Texts` クラスは `[AutoGenLocalizer]` 属性を持つ `partial` クラスとして宣言されます。
`YukkuriMovieMaker.Generator` のソースジェネレーターが `Texts.csv` を処理し、各ロケールのリソースファイルを自動生成します。

対応言語：日本語（`ja-jp`）・英語（`en-us`）・中国語簡体字（`zh-cn`）・中国語繁体字（`zh-tw`）・韓国語（`ko-kr`）・スペイン語（`es-es`）・アラビア語（`ar-sa`）・インドネシア語（`id-id`）

ローカライズキーの一覧は以下のとおりです。

| キー | ja-jp |
|---|---|
| `EffectName` | クロスハッチ陰影 |
| `BasicGroup` | 基本 |
| `ColorGroup` | 色 |
| `DetailGroup` | 詳細 |
| `StyleName` | スタイル |
| `StyleDesc` | 線の方向と階調と質感のプリセットを選びます。 |
| `AmountName` | 適用量 |
| `AmountDesc` | 元映像とハッチ結果の合成量です。 |
| `DensityName` | 密度 |
| `DensityDesc` | 線の間隔です。小さいほど線が密になります。 |
| `ThicknessName` | 太さ |
| `ThicknessDesc` | 各ハッチ線の太さです。 |
| `ShadowDepthName` | 陰影 |
| `ShadowDepthDesc` | 暗部で線が増える強さです。 |
| `FlowName` | 流れ |
| `FlowDesc` | 明暗の勾配へ線の向きを寄せる強さです。 |
| `OutlineName` | 輪郭 |
| `OutlineDesc` | ハッチに重ねる輪郭線の強さです。 |
| `InkColorName` | インク色 |
| `InkColorDesc` | ハッチ線と輪郭線の色です。 |
| `PaperColorName` | 紙色 |
| `PaperColorDesc` | 紙または背景の色です。 |
| `ColorModeName` | 色 |
| `ColorModeDesc` | 出力色の作り方です。 |
| `LineLayersName` | 線の層 |
| `LineLayersDesc` | 暗部に重ねる線方向の最大数です。 |
| `ToneSoftnessName` | 階調の柔らかさ |
| `ToneSoftnessDesc` | 線層が現れる境界の柔らかさです。 |
| `GrainName` | 粒状感 |
| `GrainDesc` | 紙とインクの手続き的な粒状感です。 |
| `SeedName` | シード |
| `SeedDesc` | 粒状感と線位相を固定する値です。 |
| `StylePen` | ペン画 |
| `StyleEngraving` | 版画 |
| `StyleManga` | 漫画 |
| `StyleBlueprint` | 設計図 |
| `ColorModeInkAndPaper` | インクと紙 |
| `ColorModePreserveSource` | 元の色を残す |
| `LineLayersAuto` | 自動 |
| `TagCrossHatch` | クロスハッチ |
| `TagHatching` | ハッチング |
| `TagPen` | ペン画 |
| `TagEngraving` | 版画 |
| `TagBlueprint` | 設計図 |
| `TagManga` | 漫画 |
