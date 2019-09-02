using System;
using System.Collections.Generic;

namespace simplefilesystem
{
    public class ParseException : Exception

    {
        public ParseException()
        {
        }

        public ParseException(string input)
            : base("Could not parse input \"" + input + "\"")
        {
            Input = input;
            Reason = null;
        }

        public ParseException(string input, string reason)
            : base("Could not parse input \"" + input + "\": " + reason)
        {
            Input = input;
            Reason = reason;
        }

        public string Input { get; }

        public string Reason { get; }

        public override bool Equals(object obj)
        {
            return obj is ParseException exception &&
                   Input == exception.Input &&
                   Reason == exception.Reason;
        }

        public override int GetHashCode()
        {
            var hashCode = 244663692;
            hashCode = hashCode
                       * -1521134295
                       + EqualityComparer<string>
                           .Default.GetHashCode(Input);
            hashCode = hashCode
                       * -1521134295
                       + EqualityComparer<string>
                           .Default.GetHashCode(Reason);
            return hashCode;
        }
    }
}