﻿============================================================
=
=  ログファイル監視＆通知ツール LogWatcher
=
============================================================

------------------------------------------------------------
■このツールは
------------------------------------------------------------
指定したログファイルの更新を監視し、更新があったとき中身を開いて新しい
ログをチェックします。そこに特定のキーワード（たとえば"[ERROR]"など）が
あったとき、Growl for Windowsを利用して画面上にポップアップ表示する
ツールです。このツールはコンソールアプリケーションです。

------------------------------------------------------------
■事前準備
------------------------------------------------------------
Growl for Windowsが必要になります。

Growl for Windowsとは、画面上にポップアップを表示するだけの汎用的なアプリ
ケーションです。Macで有名なGrowlをWindowsに移植したものです。

ここからダウンロードできます。

Growl for Windows
http://www.growlforwindows.com/gfw/

ダウンロードしたら実行してインストールしてください。Windowsファイア
ウォールの警告が出たら許可してください。

------------------------------------------------------------
■インストール
------------------------------------------------------------
以下の2つのファイルを好きなところに置くだけです。

  LogWatcher.exe
  LogWatcher.exe.config

------------------------------------------------------------
■アンインストール
------------------------------------------------------------
ファイルを削除するだけです。

------------------------------------------------------------
■起動と終了
------------------------------------------------------------
LogWatcher.exeを叩くと起動します。'q'キーを押すと終了します。

------------------------------------------------------------
■最低限の設定チェック
------------------------------------------------------------
このツールはログの更新部分をすべて通知してくれるのではなく、更新部分
のうち、特定のキーワードを含んでいる行を通知してくれるツールです。
つまり、ログでエラーが出ている箇所をうまいこと勝手に探して完璧に
通知してくれるツールではないということです。

キーワードの設定をきちんと行わない限り、あなたの望む通知は行って
くれません。デフォルトの設定では足りない（または過剰である）可能性
がありますので、「カスタマイズ」の項を参考にして、設定を確認して
ください。

------------------------------------------------------------
■注意事項
------------------------------------------------------------
- LogWatcherが起動した時点で存在しないファイルは監視対象になりません。
- 一度に大量の通知がされるのを防ぐために、1回の更新チェックで、1つの
　ファイルにつき25行以上のログが追加されている場合、通知を一部スキップ
　します。このとき、その旨がLogWatcherから通知されます

------------------------------------------------------------
■カスタマイズ
------------------------------------------------------------
LogWatcher.exe.configの中身を書き換えることで、設定を変更することが
できます。

起動すると、コンソール画面に現在の設定が一部表示されるので参考にして
ください。

なお、LogWatcher.exe.configの変更後は、LogWatcher.exeを再起動しないと
反映されません。

ErrorWordsIgnoreCaseとNormalWordsIgnoreCaseの違いですが、実際のところ
GrowlにStickyとして通知するのがError、Sticky指定しないのがNormalという
違いしかありません。Sticky指定すると、ポップアップはクリックするまで
表示され続けます。Sticky指定なしだと、一定時間後に自動で消えます。消え
るまでの時間は、Growlの設定を変更することで変えられます。


TargetDirs

　監視対象のログファイルをディレクトリ単位で指定します。そのディレクト
　リ配下の、ファイル名が".log"で終わるファイルを監視対象とします。サブ
　ディレクトリは対象になりません。複数指定したい場合はカンマで区切って
　ください。
　例：
　　C:\Temp\log,C:\App\log

TargetFiles

　監視対象のログファイルをファイル単位で指定します。この場合拡張子が
　".log"でなくても対象となります。複数指定したい場合はカンマで区切って
　ください。
　例：
　　C:\Temp\log\rotation.log,C:\App\log\system.log

ErrorWordsIgnoreCase

　エラーとしてGrowlに通知したいキーワードを指定します。正規表現で記述
　してください。大文字小文字は区別されません。
　例：
　　\[ERROR\]|\[Fatal\]|&lt;ERROR&gt;|ErrorCode

ErrorWords

　ErrorWordsIgnoreCaseと同じですが、大文字小文字は区別されます。

NormalWordsIgnoreCase

　通常ログとしてGrowlに通知したいキーワードを指定します。正規表現で記述
　してください。大文字小文字は区別されません。
　例：
　　\[WARN\]

NormalWords

　NormalWordsIgnoreCaseと同じですが、、大文字小文字は区別されます。

WatchingInterval

　ファイルの更新をチェックする周期を指定します。単位はmsecです。

GrowlPassword

　Growlにパスワードが設定されている場合に指定します。設定されていない
　場合は空にします。

GrowlHostName

　リモートにあるGrowlを利用する場合にホスト名を指定します。必要なければ
　空にします。

GrowlTcpPort

　リモートにあるGrowlを利用する場合にポート番号を指定します。必要なけれ
　ば空にします。

------------------------------------------------------------
■Growl for Windows側のカスタマイズ
------------------------------------------------------------
ポップアップのデザイン、表示位置、自動消去の時間など、Growl側で設定を
変更することも可能です。

タスクバーにあるアイコンを右クリックして「Open Growl」を選択すると
Growlの設定画面が表示されます。

一度でもLogWatcherを起動しているならば、「Applications」タブに“LogWatcher”
というApplication Nameが存在するはずです。その設定をいじると表示などを
変えられます。

------------------------------------------------------------
■更新履歴
------------------------------------------------------------
Ver.1.0.0.0
  - 新規作成
