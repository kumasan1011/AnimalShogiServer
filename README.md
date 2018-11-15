# どうぶつしょうぎ対局サーバー概要

## 通信

TCP/IPを用い対局の開始、指し手のやりとり、対局の終了検知を仲介する。

## サーバー

shogi.keio.app
80番ポート

## ログインとログアウト

クライアント側は対象アドレスの対象ポートにTCPで繋げばログインされる(Telnetなどでも可)

## 対局条件と対局の開始

サーバーは以下の形でクライアントに対局条件の通知を行う

BEGIN Game_Summary

Game_ID:20180910-001

Your_Turn:+ (後手番なら-)

END Game_Summary

## 盤面の表示

初期局面からのスタートとする

## 対局の合意

対局条件を確認したクライアントはサーバに対して以下のように合意のメッセージを送信する

AGREE

その後、サーバーから以下の通り対局開始のメッセージにより対局が始まる。

START

## 指し手の送信

駒の表記

h : ひよこ

z : ぞう

k : きりん

棋譜表記

```
  1   2   3
+---+---+---+
|   |   |   | a
+---+---+---+
|   |   |   | b
+---+---+---+
|   |   |   | c
+---+---+---+
|   |   |   | d
+---+---+---+
```

手番側のクライアントは自身の指し手を以下のようにサーバーに送る

`+2c2b (後手番なら -2b2c など)`

サーバーはこの手が合法なら両クライアントに

`+2c2b,OK`と送る。
駒打ちの場合は、`h*2b`など  
成りの場合は`2b2a+`のように最後に`+`を付ける。`2b2a+`
投了など対局が終了、中断した場合は`'#'`で始まる文字列を2行続けて送信し、何らかの事象の発生を通知する。
事象発生後、双方のクライアントはログアウトする。

## 対局終了

Ex. 引き分けで対局終了の場合

＃GAME_OVER

＃DRAW

### Ex. 勝った時

＃GAME_OVER

＃WIN

### Ex. 負けた時

＃GAME_OVER

＃LOSE

### Ex. 反則手で負けた時

＃ILLEGAL

＃LOSE
