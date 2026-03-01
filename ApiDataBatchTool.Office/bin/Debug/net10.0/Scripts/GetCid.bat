@echo off
REM ============================================
REM CID取得用バッチファイル（ダミー）
REM
REM 引数: %1 = 種別（BUSINESSCARD / OFFICE）
REM 出力: 標準出力にCID文字列のみを出力
REM ============================================

REM 引数を取得
set TYPE=%1

REM 引数に応じてCIDを出力（本番では実際の取得コマンドに置き換え）
if "%TYPE%"=="BUSINESSCARD" (
    echo CID_BUSINESSCARD_12345
) else if "%TYPE%"=="OFFICE" (
    echo CID_OFFICE_67890
) else (
    echo CID_UNKNOWN
)
