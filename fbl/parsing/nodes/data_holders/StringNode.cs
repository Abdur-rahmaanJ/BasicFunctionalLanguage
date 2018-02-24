﻿using System.Text.RegularExpressions;

namespace FBL.Parsing.Nodes
{
    public class StringNode : ExpressionNode
    {
        private static Regex regexEscaper = new Regex(@"\\(.)", RegexOptions.Compiled);

        public string StringValue { get; set; }

        public StringNode(string value)
        {
            string EscapeCharacter(Match m)
            {
                string character = m.Groups[1].Value;
                switch (character[0])
                {
                    case 'n': return "\n";
                    case 't': return "\t";
                    case 's': return " ";
                    default: return character.ToString();
                }
            }

            StringValue = regexEscaper.Replace(value, EscapeCharacter);
            Value = this;
        }

        public override string ToString()
        {
            return $"{StringValue}";
        }

        public override ExpressionNode Clone()
        {
            return new StringNode(StringValue);
        }

        public override bool DeepEquals(ExpressionNode b, long visitId)
        {
            if (this == b) return true;

            if (b is StringNode bs)
                return StringValue == bs.StringValue;

            return false;
        }
    }
}
