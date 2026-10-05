using System;
using System.Collections.Generic;
using System.Linq;

namespace MatrixScreenSaver
{
    [Flags]
    public enum CharacterSets
    {
        None = 0,
        Latin = 1,
        Katakana = 2,
        Digits = 4,
        Symbols = 8,
        Greek = 16,
        Cyrillic = 32,
        Hiragana = 64,
    }

    public class MatrixCharacter
    {
        private static readonly Dictionary<CharacterSets, char[]> CharactersBySet = new Dictionary<CharacterSets, char[]>
        {
            [CharacterSets.Latin] = new[]
            {
                'A', 'B', 'C', 'D', 'E', 'F', 'G', 'H', 'I', 'J', 'K', 'L', 'M', 'N', 'O', 'P', 'Q', 'R', 'S', 'T', 'U', 'V', 'W', 'X', 'Y', 'Z',
                'a', 'b', 'c', 'd', 'e', 'f', 'g', 'h', 'i', 'j', 'k', 'l', 'm', 'n', 'o', 'p', 'q', 'r', 's', 't', 'u', 'v', 'w', 'x', 'y', 'z'
            },
            [CharacterSets.Katakana] = new[]
            {
                'ア', 'イ', 'ウ', 'エ', 'オ', 'カ', 'ガ', 'キ', 'ギ', 'ク', 'グ', 'ケ', 'ゲ', 'コ', 'ゴ', 'サ', 'ザ', 'シ', 'ジ', 'ス', 'ズ',
                'セ', 'ゼ', 'ソ', 'ゾ', 'タ', 'ダ', 'チ', 'ヂ', 'ツ', 'ヅ', 'テ', 'デ', 'ト', 'ド', 'ナ', 'ニ', 'ヌ', 'ネ', 'ノ', 'ハ', 'バ', 'パ',
                'ヒ', 'ビ', 'ピ', 'フ', 'ブ', 'プ', 'ヘ', 'ベ', 'ペ', 'ホ', 'ボ', 'ポ', 'マ', 'ミ', 'ム', 'メ', 'モ', 'ヤ', 'ユ', 'ヨ', 'ラ',
                'リ', 'ル', 'レ', 'ロ', 'ワ', 'ヰ', 'ヱ', 'ヲ', 'ン', 'ヴ'
            },
            [CharacterSets.Digits] = new[]
            {
                '0', '1', '2', '3', '4', '5', '6', '7', '8', '9'
            },
            [CharacterSets.Symbols] = new[]
            {
                '!', '?', '.', ',', ';', ':', '(', ')', '[', ']', '{', '}',
                '+', '-', '*', '/', '=',
                '_', '#', '$', '%', '&', '~', '^'
            },
            [CharacterSets.Greek] = LettersBetween('Α', 'Ω').Concat(LettersBetween('α', 'ω')).ToArray(),
            [CharacterSets.Cyrillic] = LettersBetween('А', 'я').ToArray(),
            [CharacterSets.Hiragana] = LettersBetween('ぁ', 'ゖ').Except("ぁぃぅぇぉっゃゅょゎゕゖ").ToArray(),
        };

        public int Brush { get; set; } = 0;

        // Level on the screen; falls behind Brush when a frame skips the redraw and leaves the character standing.
        public int DisplayedBrush { get; set; }

        public char Character { get; set; } = ' ';
        public int Speed { get; set; } = 0;

        // A flash drop runs down the whole screen within a frame or two.
        public bool IsFlash { get; set; }

        // Index of the palette the drop is drawn with.
        public int Palette { get; set; }

        public static char[] CreatePool(CharacterSets sets)
        {
            return CharactersBySet.Where(pair => sets.HasFlag(pair.Key)).SelectMany(pair => pair.Value).ToArray();
        }

        // The Unicode blocks contain unassigned code points, e.g. between the Greek capitals.
        private static IEnumerable<char> LettersBetween(char first, char last)
        {
            return Enumerable.Range(first, last - first + 1).Select(code => (char)code).Where(char.IsLetter);
        }
    }
}
