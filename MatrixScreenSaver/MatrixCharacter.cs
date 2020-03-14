﻿using System;

namespace MatrixScreenSaver
{
    public class MatrixCharacter
    {
        //public static readonly Brush FontBrushDefault = new SolidColorBrush(Color.FromArgb(0xff, 0x1e, 0x91, 0x1c));
        //public static readonly Brush FontBrushNew = new SolidColorBrush(Color.FromArgb(0xff, 0xff, 0xff, 0xff));
        //public static readonly Brush FontBrushOld = new SolidColorBrush(Color.FromArgb(0xff, 0x00, 0x00, 0x00));

        public static readonly char[] PoolOfCharacters =
        {
            // Latin
            'A', 'B', 'C', 'D', 'E', 'F', 'G', 'H', 'I', 'J', 'K', 'L', 'M', 'N', 'O', 'P', 'Q', 'R', 'S', 'T', 'U', 'V', 'W', 'X', 'Y', 'Z',
            'a', 'b', 'c', 'd', 'e', 'f', 'g', 'h', 'i', 'j', 'k', 'l', 'm', 'n', 'o', 'p', 'q', 'r', 's', 't', 'u', 'v', 'w', 'x', 'y', 'z',
            // Cyrillic (?)
            // Greek (?)
            // Hiragana (?)
            // Katakana
            'ア', 'イ', 'ウ', 'エ', 'オ', 'カ', 'ガ', 'キ', 'ギ', 'ク', 'グ', 'ケ', 'ゲ', 'コ', 'ゴ', 'サ', 'ザ', 'シ', 'ジ', 'ス', 'ズ',
            'セ', 'ゼ', 'ソ', 'ゾ', 'タ', 'ダ', 'チ', 'ヂ', 'ツ', 'ヅ', 'テ', 'デ', 'ト', 'ド', 'ナ', 'ニ', 'ヌ', 'ネ', 'ノ', 'ハ', 'バ', 'パ',
            'ヒ', 'ビ', 'ピ', 'フ', 'ブ', 'プ', 'ヘ', 'ベ', 'ペ', 'ホ', 'ボ', 'ポ', 'マ', 'ミ', 'ム', 'メ', 'モ', 'ヤ', 'ユ', 'ヨ', 'ラ',
            'リ', 'ル', 'レ', 'ロ', 'ワ', 'ヰ', 'ヱ', 'ヲ', 'ン', 'ヴ',
            // Numbers
            '0', '1', '2', '3', '4', '5', '6', '7', '8', '9',
            // Signs
            '!', '?', '.', ',', ';', ':', '(', ')', '[', ']', '{', '}',
            '+', '-', '*', '/', '=',
            '_', '#', '$', '%', '&', '~', '^'
        };

        //public Brush Brush { get; set; } = FontBrushOld;
        public int Brush { get; set; } = 0;

        public char Character { get; set; } = ' ';
        public DateTime CreationDate { get; set; } = DateTime.Now;
        public string Name { get; set; }
        //public bool HasChanged { get; set; }

        //public static Brush CalculateBrush(MatrixCharacter character)
        //{
        //    return FontBrushDefault;
        //}
    }
}