using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace LogWatcher
{
    public class EncodingDetector
    {
        /// <summary>
        /// Kinds of Encoding
        /// </summary>
        protected enum CharCode
        {
            ASCII,
            BINARY,
            EUC,
            JIS,
            SJIS,
            UTF32,
            UTF32B,
            UTF16,
            UTF16B,
            UTF8N,
            UTF8,
            Unknown
        }

        /// <summary>
        /// Detect Encoding
        /// </summary>
        /// <param name="data">content of target file</param>
        /// <returns>instance of Encoding. If failed detecting, return null.</returns>
        public static Encoding DetectEncoding(byte[] data)
        {
            Encoding detectedEncoding = null;

            CharCode code = DetectCharCodeWithBomHeader(data, data.Length);
            if(code == CharCode.UTF32B)
            {
                // I'm not sure what to return
                return null;
            }

            if(code == CharCode.Unknown)
            {
                code = DetectCharCode(data, data.Length);
            }

            switch (code)
            {
                case CharCode.ASCII:
                    detectedEncoding = Encoding.ASCII;
                    break;
                case CharCode.EUC:
                    detectedEncoding = Encoding.GetEncoding("EUC-JP");
                    break;
                case CharCode.JIS:
                    detectedEncoding = Encoding.GetEncoding("iso-2022-jp");
                    break;
                case CharCode.SJIS:
                    detectedEncoding = Encoding.GetEncoding("shift_jis");
                    break;
                case CharCode.UTF8N:
                case CharCode.UTF8:
                    detectedEncoding = Encoding.UTF8;
                    break;
                case CharCode.UTF16:
                    detectedEncoding = Encoding.Unicode;
                    break;
                case CharCode.UTF16B:
                    detectedEncoding = Encoding.BigEndianUnicode;
                    break;
                case CharCode.UTF32:
                    detectedEncoding = Encoding.UTF32;
                    break;
                case CharCode.BINARY:
                default:
                    break;
            }

            return detectedEncoding;
        }

        /// <summary>
        /// 読み込んでいるbyte配列内容のエンコーディングを自前で判定する.
        /// Original source by http://d.hatena.ne.jp/hnx8/20120225/1330157903
        /// </summary>
        /// <param name="data">ファイルから読み込んだバイトデータ</param>
        /// <param name="datasize">バイトデータのサイズ</param>
        /// <returns>エンコーディングの種類</returns>
        private static CharCode DetectCharCode(byte[] data, int datasize)
        {
            //バイトデータ（読み取り結果）
            byte b1 = (datasize > 0) ? data[0] : (byte)0;
            byte b2 = (datasize > 1) ? data[1] : (byte)0;
            byte b3 = (datasize > 2) ? data[2] : (byte)0;
            byte b4 = (datasize > 3) ? data[3] : (byte)0;

            //UTF16Nの判定(ただし半角英数文字の場合のみ検出可能)
            if (b1 == 0x00 && (datasize % 2 == 0))
            {
                for (int i = 0; i < datasize; i = i + 2)
                {
                    if (data[i] != 0x00 || data[i + 1] < 0x06 || data[i + 1] >= 0x7f)
                    {   //半角OnlyのUTF16でもなさそうなのでバイナリ
                        return CharCode.BINARY;
                    }
                }
                return CharCode.UTF16B;
            }
            if (b2 == 0x00 && (datasize % 2 == 0))
            {
                for (int i = 0; i < datasize; i = i + 2)
                {
                    if (data[i] < 0x06 || data[i] >= 0x7f || data[i + 1] != 0x00)
                    {   //半角OnlyのUTF16でもなさそうなのでバイナリ
                        return CharCode.BINARY;
                    }
                }
                return CharCode.UTF16;
            }

            //全バイト内容を走査・まずAscii,JIS判定
            int pos = 0;
            int jisCount = 0;
            while (pos < datasize)
            {
                b1 = data[pos];
                if (b1 < 0x03 || b1 >= 0x7f)
                {   //非ascii(UTF,SJis等)発見：次のループへ
                    break;
                }
                else if (b1 == 0x1b)
                {   //ESC(JIS)判定
                    //2バイト目以降の値を把握
                    b2 = ((pos < datasize + 1) ? data[pos + 1] : (byte)0);
                    b3 = ((pos < datasize + 2) ? data[pos + 2] : (byte)0);
                    b4 = ((pos < datasize + 3) ? data[pos + 3] : (byte)0);
                    //B2の値をもとに判定
                    if (b2 == 0x24)
                    {   //ESC$
                        if (b3 == 0x40 || b3 == 0x42)
                        {   //ESC $@,$B : JISエスケープ
                            jisCount++;
                            pos = pos + 2;
                        }
                        else if (b3 == 0x28 && (b4 == 0x44 || b4 == 0x4F || b4 == 0x51 || b4 == 0x50))
                        {   //ESC$(D, ESC$(O, ESC$(Q, ESC$(P : JISエスケープ
                            jisCount++;
                            pos = pos + 3;
                        }
                    }
                    else if (b2 == 0x26)
                    {   //ESC& : JISエスケープ
                        if (b3 == 0x40)
                        {   //ESC &@ : JISエスケープ
                            jisCount++;
                            pos = pos + 2;
                        }
                    }
                    else if (b2 == 0x28)
                    {   //ESC((28)
                        if (b3 == 0x4A || b3 == 0x49 || b3 == 0x42)
                        {   //ESC(J, ESC(I, ESC(B : JISエスケープ
                            jisCount++;
                            pos = pos + 2;
                        }
                    }
                }
                pos++;
            }
            //Asciiのみならここで文字コード決定
            if (pos == datasize)
            {
                if (jisCount > 0)
                {   //JIS出現
                    return CharCode.JIS;
                }
                else
                {   //JIS未出現。Ascii
                    return CharCode.ASCII;
                }
            }

            bool prevIsKanji = false; //文字コード判定強化、同種文字のときにポイント加算-HNXgrep
            int notAsciiPos = pos;
            int utfCount = 0;
            //UTF妥当性チェック（バイナリ判定を行いながら実施）
            while (pos < datasize)
            {
                b1 = data[pos];
                pos++;

                if (b1 < 0x03 || b1 == 0x7f || b1 == 0xff)
                {   //バイナリ文字：直接脱出
                    return CharCode.BINARY;
                }
                if (b1 < 0x80 || utfCount < 0)
                {   //半角文字・非UTF確定時は、後続処理は行わない
                    continue; // 半角文字は特にチェックしない
                }

                //2バイト目を把握、コードチェック
                b2 = ((pos < datasize) ? data[pos] : (byte)0x00);
                if (b1 < 0xC2 || b1 >= 0xf5)
                {   //１バイト目がC0,C1,F5以降、または２バイト目にしか現れないはずのコードが出現、NG
                    utfCount = -1;
                }
                else if (b1 < 0xe0)
                {   //2バイト文字：コードチェック
                    if (b2 >= 0x80 && b2 <= 0xbf)
                    {   //２バイト目に現れるべきコードが出現、OK（半角文字として扱う）
                        if (prevIsKanji == false) { utfCount += 2; } else { utfCount += 1; prevIsKanji = false; }
                        pos++;
                    }
                    else
                    {   //２バイト目に現れるべきコードが未出現、NG
                        utfCount = -1;
                    }
                }
                else if (b1 < 0xf0)
                {   //3バイト文字：３バイト目を把握
                    b3 = ((pos + 1 < datasize) ? data[pos + 1] : (byte)0x00);
                    if (b2 >= 0x80 && b2 <= 0xbf && b3 >= 0x80 && b3 <= 0xbf)
                    {   //２/３バイト目に現れるべきコードが出現、OK（全角文字扱い）
                        if (prevIsKanji == true) { utfCount += 4; } else { utfCount += 3; prevIsKanji = true; }
                        pos += 2;
                    }
                    else
                    {   //２/３バイト目に現れるべきコードが未出現、NG
                        utfCount = -1;
                    }
                }
                else
                {   //４バイト文字：３，４バイト目を把握
                    b3 = ((pos + 1 < datasize) ? data[pos + 1] : (byte)0x00);
                    b4 = ((pos + 2 < datasize) ? data[pos + 2] : (byte)0x00);
                    if (b2 >= 0x80 && b2 <= 0xbf && b3 >= 0x80 && b3 <= 0xbf && b4 >= 0x80 && b4 <= 0xbf)
                    {   //２/３/４バイト目に現れるべきコードが出現、OK（全角文字扱い）
                        if (prevIsKanji == true) { utfCount += 6; } else { utfCount += 4; prevIsKanji = true; }
                        pos += 3;
                    }
                    else
                    {   //２/３/４バイト目に現れるべきコードが未出現、NG
                        utfCount = -1;
                    }
                }
            }

            //SJIS妥当性チェック
            pos = notAsciiPos;
            int sjisCount = 0;
            while (sjisCount >= 0 && pos < datasize)
            {
                b1 = data[pos];
                pos++;
                if (b1 < 0x80) { continue; }// 半角文字は特にチェックしない
                else if (b1 == 0x80 || b1 == 0xA0 || b1 >= 0xFD)
                {   //SJISコード外、可能性を破棄
                    sjisCount = -1;
                }
                else if ((b1 > 0x80 && b1 < 0xA0) || b1 > 0xDF)
                {   //全角文字チェックのため、2バイト目の値を把握
                    b2 = ((pos < datasize) ? data[pos] : (byte)0x00);
                    //全角文字範囲外じゃないかチェック
                    if (b2 < 0x40 || b2 == 0x7f || b2 > 0xFC)
                    {   //可能性を除外
                        sjisCount = -1;
                    }
                    else
                    {   //全角文字数を加算,ポジションを進めておく
                        if (prevIsKanji == true) { sjisCount += 2; } else { sjisCount += 1; prevIsKanji = true; }
                        pos++;
                    }
                }
                else if (prevIsKanji == false)
                {
                    //半角文字数の加算（半角カナの連続はボーナス点を高めに）
                    sjisCount += 1;
                }
                else
                {
                    prevIsKanji = false;
                }
            }
            //EUC妥当性チェック
            pos = notAsciiPos;
            int eucCount = 0;
            while (eucCount >= 0 && pos < datasize)
            {
                b1 = data[pos];
                pos++;
                if (b1 < 0x80) { continue; } // 半角文字は特にチェックしない
                //2バイト目を把握、コードチェック
                b2 = ((pos < datasize) ? data[pos] : (byte)0);
                if (b1 == 0x8e)
                {   //1バイト目＝かな文字指定。2バイトの半角カナ文字チェック
                    if (b2 < 0xA1 || b2 > 0xdf)
                    {   //可能性破棄
                        eucCount = -1;
                    }
                    else
                    {   //検出OK,EUC文字数を加算（半角文字）
                        if (prevIsKanji == false) { eucCount += 2; } else { eucCount += 1; prevIsKanji = false; }
                        pos++;
                    }
                }
                else if (b1 == 0x8f)
                {   //１バイト目の値＝３バイト文字を指定
                    if (b2 < 0xa1 || (pos + 1 < datasize && data[pos + 1] < 0xa1))
                    {   //２バイト目・３バイト目で可能性破棄
                        eucCount = -1;
                    }
                    else
                    {   //検出OK,EUC文字数を加算（全角文字）
                        if (prevIsKanji == true) { eucCount += 3; } else { eucCount += 1; prevIsKanji = true; }
                        pos += 2;
                    }
                }
                else if (b1 < 0xa1 || b2 < 0xa1)
                {   //２バイト文字のはずだったがどちらかのバイトがNG
                    eucCount = -1;
                }
                else
                {   //２バイト文字OK（全角）
                    if (prevIsKanji == true) { eucCount += 2; } else { eucCount += 1; prevIsKanji = true; }
                    pos++;
                }
            }

            //文字コード決定
            if (eucCount > sjisCount && eucCount > utfCount)
            {
                return CharCode.EUC;
            }
            else if (utfCount > sjisCount)
            {
                return CharCode.UTF8N;
            }
            else if (sjisCount > -1)
            {
                return CharCode.SJIS;
            }
            else
            {
                return CharCode.BINARY;
            }
        }

        /// <summary>
        /// Bom・ヘッダから決定できる文字コードを判定。
        /// </summary>
        /// <returns>エンコーディングの種類</returns>
        private static CharCode DetectCharCodeWithBomHeader(byte[] data, int datasize)
        {
            //バイトデータ（読み取り結果）
            byte b1 = (datasize > 0) ? data[0] : (byte)0;
            byte b2 = (datasize > 1) ? data[1] : (byte)0;
            byte b3 = (datasize > 2) ? data[2] : (byte)0;
            byte b4 = (datasize > 3) ? data[3] : (byte)1;

            //BOMから判別できる文字コード判定
            if (b1 == 0xFF && b2 == 0xFE && b3 == 0x00 && b4 == 0x00)
            {   //BOMよりUTF32(littleEndian)
                return CharCode.UTF32;
            }
            if (b1 == 0x00 && b2 == 0x00 && b3 == 0xFE && b4 == 0xFF)
            {   //BOMよりUTF32(bigEndian)
                return CharCode.UTF32B;
            }
            if (b1 == 0xff && b2 == 0xfe)
            {   //BOMよりUnicode(Windows標準のUTF-16のlittleEndian)
                return CharCode.UTF16;
            }
            if (b1 == 0xfe && b2 == 0xff)
            {   //BOMよりUnicode(UTF-16のBigEndien)
                return CharCode.UTF16B;
            }
            if (b1 == 0xef && b2 == 0xbb && b3 == 0xbf)
            {   //BOMよりUTF-8
                return CharCode.UTF8;
            }
            //BOMなし
            return CharCode.Unknown;
        }
    }
}
