# 新入生名簿作成システム (WPF版)

名古屋大学の「地獄の細道」で新入生の連絡先を入力してもらうためのツールです。
こちらはフルスクリーン・高DPIモニタに対応しています。

## バイナリダウンロード

[Releases](https://github.com/tats-u/freshman-list-wpf/releases) からダウンロード可能です。

## ビルド方法

### Visual Studio

`名簿作成システム(WPF).sln`を開き、**NuGetパッケージの復元を行ってから**ビルドしてください。

### コマンドライン

x64 Native Tools Command Promptなど、`MSBuild`コマンドが利用できる端末から以下のコマンドを実行します。

```powershell
MSBuild -restore -p:Configuration=Release -p:Platform="Any CPU" -m
```

## ライセンスについて

政治色が強いサークルの使用は禁止しています。通常のサークルはMITライセンスです。